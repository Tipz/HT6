using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Together.Api.Authentication;
using Together.Api.Data;
using Together.Api.Endpoints;
using Together.Api.Security;
using Together.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
});
builder.Services.Configure<LoggerFactoryOptions>(options =>
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);

var connectionString = builder.Configuration.GetConnectionString("Together");
if (string.IsNullOrWhiteSpace(connectionString))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("ConnectionStrings:Together must be configured outside Development.");
    connectionString = "Host=localhost;Port=5432;Database=together;Username=together;Password=together_dev";
}

var secureCookies = builder.Configuration.GetValue<bool?>("Authentication:SecureCookies")
    ?? !builder.Environment.IsDevelopment();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("postgresql", tags: ["ready"]);
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});

var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddYandexAuthentication(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
foreach (var value in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    if (IPAddress.TryParse(value, out var address))
        forwardedOptions.KnownProxies.Add(address);

var app = builder.Build();
var indexFile = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
string? indexHtml = null;
if (indexFile.Exists)
{
    using var reader = new StreamReader(indexFile.CreateReadStream());
    indexHtml = reader.ReadToEnd();
}
var contentSecurityPolicy = ContentSecurityPolicyFactory.Create(
    long.TryParse(app.Configuration["YandexMetrika:CounterId"], out _),
    indexHtml);

if (forwardedOptions.KnownProxies.Count > 0)
    app.UseForwardedHeaders(forwardedOptions);
if (!app.Environment.IsDevelopment())
    app.UseHsts();
if (builder.Configuration.GetValue<bool>("Security:UseHttpsRedirection"))
    app.UseHttpsRedirection();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    await next();
    context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Together.Request")
        .LogInformation(
            new EventId(1000, "RequestCompleted"),
            "HTTP {Method} {Path} responded {StatusCode}",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode);
});
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        context.Response.Headers.ContentSecurityPolicy = contentSecurityPolicy;
        return Task.CompletedTask;
    });
    await next();
});
app.UseBlazorFrameworkFiles();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    var unsafeMethod = HttpMethods.IsPost(context.Request.Method)
        || HttpMethods.IsPut(context.Request.Method)
        || HttpMethods.IsPatch(context.Request.Method)
        || HttpMethods.IsDelete(context.Request.Method);
    if (unsafeMethod && context.Request.Path.StartsWithSegments("/api"))
    {
        try
        {
            await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "Некорректный antiforgery token",
                status = StatusCodes.Status400BadRequest,
                traceId = context.TraceIdentifier
            });
            return;
        }
    }
    await next();
});

app.MapGet(ApiRoutes.Health, () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();
app.MapGet(ApiRoutes.Antiforgery, (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!));
}).AllowAnonymous();
app.MapGet(ApiRoutes.PublicSettings, (YandexProviderState yandex, IConfiguration configuration) =>
{
    var counterId = long.TryParse(configuration["YandexMetrika:CounterId"], out var parsed) && parsed > 0
        ? parsed
        : (long?)null;
    return Results.Ok(new PublicSettingsResponse(yandex.Enabled, counterId));
}).AllowAnonymous();

var auth = app.MapGroup(ApiRoutes.Auth).RequireRateLimiting("auth");
auth.MapIdentityApi<ApplicationUser>();
auth.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.NoContent();
}).RequireAuthorization();
auth.MapGet("/me", async (System.Security.Claims.ClaimsPrincipal principal, UserManager<ApplicationUser> users) =>
{
    var user = await users.GetUserAsync(principal);
    return user?.Email is null
        ? Results.Unauthorized()
        : Results.Ok(new CurrentUserResponse(user.Id, user.Email));
}).RequireAuthorization();

app.MapExternalAuth();
app.MapTripsApi();
app.MapFallbackToFile("index.html");

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    return;
}

app.Run();

public partial class Program;
