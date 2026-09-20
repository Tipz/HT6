using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http.Json;
using Together.Contracts;

namespace Together.Client.Storage;

public sealed class ApiHttp(HttpClient client)
{
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head && request.Method != HttpMethod.Options)
        {
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Antiforgery);
            tokenRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            using var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();
            var token = await tokenResponse.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>(cancellationToken)
                ?? throw new HttpRequestException("Сервер не выдал antiforgery token.");
            request.Headers.Add("X-XSRF-TOKEN", token.Token);
        }
        return await client.SendAsync(request, cancellationToken);
    }

    public Uri ToAbsoluteUri(string relativeUri) => new(client.BaseAddress!, relativeUri);
}

public sealed class AuthenticationRequiredException : Exception;
