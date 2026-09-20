using System.Net;
using System.Net.Http.Json;
using Together.Contracts;

namespace Together.Api.Tests;

public sealed class TripsApiTests(TogetherApiFactory factory) : IClassFixture<TogetherApiFactory>
{
    [Fact]
    public async Task Trips_RequireAuthentication()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(ApiRoutes.Trips);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_CanCreateReadAndDetectConflict_WhileOtherUserCannotRead()
    {
        using var owner = factory.CreateClient();
        await RegisterAndLogin(owner, $"owner-{Guid.NewGuid():N}@example.test");
        var request = new TripWriteRequest
        {
            Name = "Летняя поездка",
            StartDate = new DateOnly(2027, 7, 1),
            EndDate = new DateOnly(2027, 7, 8),
            Adults = 2,
            ChildAges = [4]
        };
        using var createdResponse = await owner.PostAsJsonAsync(ApiRoutes.Trips, request);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<TripResponse>();
        Assert.NotNull(created);
        Assert.Equal(1, created.Revision);

        var variantRequest = new VariantWriteRequest
        {
            ExpectedTripRevision = created.Revision,
            Name = "Отель у моря",
            Destination = "Сочи",
            Accommodation = "Семейный номер",
            SourceUrl = "https://example.test/hotel",
            TravelMinutes = 120,
            Transfers = 0,
            Kitchen = "yes",
            Crib = "unknown",
            Playground = "no",
            DistanceMeters = 500,
            DistanceTarget = "море",
            Expenses = [100_000, 0, null, 25_000, null, 5_000]
        };
        using var variantResponse = await owner.PostAsJsonAsync(
            $"{ApiRoutes.Trips}/{created.Id}/variants", variantRequest);
        Assert.Equal(HttpStatusCode.Created, variantResponse.StatusCode);
        var withVariant = await variantResponse.Content.ReadFromJsonAsync<TripResponse>();
        Assert.NotNull(withVariant);
        Assert.Equal(2, withVariant.Revision);
        Assert.Equal(variantRequest.Expenses, Assert.Single(withVariant.Variants).Expenses);

        using var conflictResponse = await owner.PutAsJsonAsync($"{ApiRoutes.Trips}/{created.Id}", request);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        using var other = factory.CreateClient();
        await RegisterAndLogin(other, $"other-{Guid.NewGuid():N}@example.test");
        using var hiddenResponse = await other.GetAsync($"{ApiRoutes.Trips}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenResponse.StatusCode);
    }

    [Fact]
    public async Task InvalidTrip_ReturnsValidationProblem()
    {
        using var client = factory.CreateClient();
        await RegisterAndLogin(client, $"validation-{Guid.NewGuid():N}@example.test");
        using var response = await client.PostAsJsonAsync(ApiRoutes.Trips, new TripWriteRequest());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task RegisterAndLogin(HttpClient client, string email)
    {
        const string password = "12345";
        using var register = await client.PostAsJsonAsync($"{ApiRoutes.Auth}/register", new { email, password });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        using var login = await client.PostAsJsonAsync($"{ApiRoutes.Auth}/login?useCookies=true", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
