using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Together.Api.Data;
using Together.Api.Endpoints;
using Together.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
var connectionString = builder.Configuration.GetConnectionString("Together")
    ?? "Host=localhost;Port=5432;Database=together;Username=together;Password=together_dev";
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("postgresql");
builder.Services.AddAuthorization();
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 5;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
var secureCookies = builder.Configuration.GetValue<bool?>("Authentication:SecureCookies")
    ?? !builder.Environment.IsDevelopment();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookies
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseBlazorFrameworkFiles();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet(ApiRoutes.Health, () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();
app.MapHealthChecks("/health/ready");

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
