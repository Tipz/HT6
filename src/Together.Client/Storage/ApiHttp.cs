using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Together.Client.Storage;

public sealed class ApiHttp(HttpClient client)
{
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return await client.SendAsync(request, cancellationToken);
    }
}

public sealed class AuthenticationRequiredException : Exception;
