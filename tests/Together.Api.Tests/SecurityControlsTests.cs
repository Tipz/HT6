using System.Net;
using Together.Contracts;

namespace Together.Api.Tests;

public sealed class SecurityControlsTests
{
    [Fact]
    public async Task PasswordLogin_LocksAccountAfterFiveFailures()
    {
        using var factory = new TogetherApiFactory();
        using var client = factory.CreateClient();
        var email = $"lockout-{Guid.NewGuid():N}@example.test";
        const string password = "correct horse battery staple";
        using var register = await TripsApiTests.SendWithAntiforgery(
            client, HttpMethod.Post, $"{ApiRoutes.Auth}/register", new
            {
                email,
                password
            });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await TripsApiTests.SendWithAntiforgery(
                client,
                HttpMethod.Post,
                $"{ApiRoutes.Auth}/login?useCookies=true",
                new
                {
                    email,
                    password = "definitely wrong password"
                });
            Assert.False(failed.IsSuccessStatusCode);
        }

        using var locked = await TripsApiTests.SendWithAntiforgery(
            client, HttpMethod.Post, $"{ApiRoutes.Auth}/login?useCookies=true", new
            {
                email,
                password
            });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [Fact]
    public async Task AuthenticationEndpoints_AreRateLimited()
    {
        using var factory = new TogetherApiFactory();
        using var client = factory.CreateClient();
        HttpStatusCode lastStatus = 0;
        for (var attempt = 0; attempt < 31; attempt++)
        {
            using var response = await client.GetAsync($"{ApiRoutes.Auth}/me");
            lastStatus = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }
}
