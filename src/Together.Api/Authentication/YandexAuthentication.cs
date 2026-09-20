using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Identity;
using Together.Api.Data;

namespace Together.Api.Authentication;

public static class YandexAuthentication
{
    public const string Scheme = "Yandex";
    public const string CallbackPath = "/signin-yandex";

    public static YandexProviderState AddYandexAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var clientId = configuration["Authentication:Yandex:ClientId"];
        var clientSecret = configuration["Authentication:Yandex:ClientSecret"];
        var enabled = !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);

        if (enabled)
        {
            services.AddAuthentication().AddOAuth(Scheme, options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ClientId = clientId!;
                options.ClientSecret = clientSecret!;
                options.CallbackPath = CallbackPath;
                options.AuthorizationEndpoint = "https://oauth.yandex.com/authorize";
                options.TokenEndpoint = "https://oauth.yandex.com/token";
                options.UserInformationEndpoint = "https://login.yandex.ru/info?format=json";
                options.UsePkce = true;
                options.SaveTokens = false;
                options.Scope.Clear();
                options.Scope.Add("login:email");
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "default_email");
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", context.AccessToken);
                        using var response = await context.Backchannel.SendAsync(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();
                        using var profile = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                        context.RunClaimActions(profile.RootElement);
                    },
                    OnRemoteFailure = context =>
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/login?oauthError=cancelled");
                        return Task.CompletedTask;
                    }
                };
            });
        }

        var state = new YandexProviderState(enabled, Scheme);
        services.AddSingleton(state);
        return state;
    }
}

public sealed record YandexProviderState(bool Enabled, string Scheme);
