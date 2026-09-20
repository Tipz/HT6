namespace Together.Core;

public static class ExampleData
{
    public const string TripName = "Пример: летний отпуск у моря";

    public static Workspace Create()
    {
        var createdAt = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        var trip = new Trip
        {
            Name = TripName,
            StartDate = new DateOnly(2027, 7, 12),
            EndDate = new DateOnly(2027, 7, 19),
            Adults = 2,
            ChildAges = [4, 9],
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Revision = 1
        };

        trip.Variants =
        [
            CreateVariant(
                trip.Id,
                "Апартаменты у бухты",
                "Геленджик",
                "Апартаменты с двумя спальнями",
                "Самолёт до Геленджика, затем 35 минут на такси",
                "Тихий район, продуктовый магазин в соседнем доме.",
                330, 0, "yes", "yes", "no", 400,
                [42_000_00, 56_000_00, 24_000_00, 6_000_00, 10_000_00, 5_000_00],
                createdAt),
            CreateVariant(
                trip.Id,
                "Семейный отель «Сосны»",
                "Геленджик",
                "Семейный отель, завтрак включён",
                "Прямой поезд, трансфер от вокзала включён",
                "Есть детский клуб и бассейн с мелкой зоной.",
                1_120, 0, "no", "yes", "yes", 150,
                [38_000_00, 77_000_00, 14_000_00, 4_000_00, 8_000_00, 5_000_00],
                createdAt.AddMinutes(1)),
            CreateVariant(
                trip.Id,
                "Дом у Балтики",
                "Зеленоградск",
                "Небольшой дом с кухней и террасой",
                "Самолёт до Калининграда, автобус до Зеленоградска",
                "Можно готовить дома; рядом парк и велодорожка.",
                260, 1, "yes", "unknown", "yes", 600,
                [54_000_00, 49_000_00, 24_000_00, 8_000_00, 10_000_00, 5_000_00],
                createdAt.AddMinutes(2))
        ];
        trip.SelectedVariantIds = trip.Variants.Select(variant => variant.Id).ToList();

        return new Workspace { Revision = 1, Trips = [trip] };
    }

    private static Variant CreateVariant(
        Guid tripId,
        string name,
        string destination,
        string accommodation,
        string roadDescription,
        string notes,
        int travelMinutes,
        int transfers,
        string kitchen,
        string crib,
        string playground,
        int distanceMeters,
        long?[] expenses,
        DateTimeOffset createdAt) => new()
        {
            TripId = tripId,
            Name = name,
            Destination = destination,
            Accommodation = accommodation,
            RoadDescription = roadDescription,
            Notes = notes,
            TravelMinutes = travelMinutes,
            Transfers = transfers,
            Kitchen = kitchen,
            Crib = crib,
            Playground = playground,
            DistanceMeters = distanceMeters,
            DistanceTarget = "до пляжа",
            Expenses = expenses,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            Revision = 1
        };
}
