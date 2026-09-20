using System.Net;

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
    }
}
