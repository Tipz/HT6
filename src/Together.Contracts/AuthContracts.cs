namespace Together.Contracts;

public sealed record CurrentUserResponse(Guid Id, string Email);

public sealed record AntiforgeryTokenResponse(string Token);

public sealed record PublicSettingsResponse(bool YandexLoginEnabled, long? YandexMetrikaCounterId);
