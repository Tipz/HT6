using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Together.Contracts;

namespace Together.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<TogetherApiFactory>
{
    private readonly HttpClient client;

    public HealthEndpointTests(TogetherApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task Readiness_ReturnsOkWhenDatabaseIsAvailable()
    {
        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MissingOptionalConfiguration_DisablesYandexServices()
    {
        using var unconfiguredFactory = new WebApplicationFactory<Program>();
        using var unconfiguredClient = unconfiguredFactory.CreateClient();
        var settings = await unconfiguredClient.GetFromJsonAsync<PublicSettingsResponse>(ApiRoutes.PublicSettings);

        Assert.NotNull(settings);
        Assert.False(settings.YandexLoginEnabled);
        Assert.Null(settings.YandexMetrikaCounterId);
    }
}
