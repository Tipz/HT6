using System.Net;
using System.Net.Http.Json;
using Together.Contracts;

namespace Together.Client.Storage;

public sealed class AuthClient(ApiHttp api)
{
    public async Task<CurrentUserResponse?> CurrentAsync()
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{ApiRoutes.Auth}/me"));
        return response.StatusCode == HttpStatusCode.Unauthorized
            ? null
            : await ReadAsync<CurrentUserResponse>(response);
    }

    public async Task LoginAsync(string email, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiRoutes.Auth}/login?useCookies=true")
        {
            Content = JsonContent.Create(new { email, password })
        };
        using var response = await api.SendAsync(request);
        await EnsureSuccess(response, "Не удалось войти. Проверьте email и пароль.");
    }

    public async Task RegisterAsync(string email, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiRoutes.Auth}/register")
        {
            Content = JsonContent.Create(new { email, password })
        };
        using var response = await api.SendAsync(request);
        await EnsureSuccess(response, "Не удалось зарегистрироваться. Проверьте email и требования к паролю.");
        await LoginAsync(email, password);
    }

    public async Task LogoutAsync()
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ApiRoutes.Auth}/logout"));
        await EnsureSuccess(response, "Не удалось завершить сеанс.");
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccess(response, "Сервер вернул некорректный ответ.");
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new HttpRequestException("Пустой ответ API.");
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, string message)
    {
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(message, new InvalidOperationException(detail), response.StatusCode);
        }
    }
}
