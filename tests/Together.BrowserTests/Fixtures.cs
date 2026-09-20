using Together.Core;
namespace Together.BrowserTests;

public static class Fixtures
{
    public static Workspace Create(int trips = 1, int variants = 3)
    {
        var data = new Workspace { Revision = 1 };
        string[] names = ["Апартаменты у бухты", "Семейный отель «Сосны»", "Дом у Балтики"];
        long?[][] budgets = [[4200000, 5600000, 2400000, 600000, 1000000, 500000], [4200000, 7700000, 1400000, 400000, 800000, 500000], [5400000, 4900000, 2400000, 800000, 1000000, 500000]];
        for (var n = 0; n < trips; n++)
        {
            var trip = new Trip { Name = n == 0 ? "Летний отпуск" : $"Поездка {n + 1}", StartDate = new(2027, 7, 12), EndDate = new(2027, 7, 19), ChildAges = [3, 8], Revision = 1 };
            for (var i = 0; i < variants; i++)
            {
                var v = new Variant
                {
                    TripId = trip.Id,
                    Name = i < 3 ? names[i] : $"Вариант {i + 1}",
                    Destination = i % 3 == 2 ? "Зеленоградск" : "Геленджик",
                    Accommodation = i % 3 == 1 ? "Семейный отель" : "Апартаменты",
                    TravelMinutes = new[] { 330, 310, 260 }[i % 3],
                    Transfers = i % 3 == 2 ? 1 : 0,
                    Kitchen = i % 3 == 1 ? "no" : "yes",
                    Crib = i % 3 == 2 ? "unknown" : "yes",
                    Playground = i % 3 == 0 ? "no" : "yes",
                    DistanceMeters = new[] { 400, 150, 600 }[i % 3],
                    DistanceTarget = "до пляжа",
                    Expenses = [.. budgets[i % 3]],
                    Revision = 1
                };
                trip.Variants.Add(v);
                if (i < 10)
                    trip.SelectedVariantIds.Add(v.Id);
            }
            data.Trips.Add(trip);
        }
        return data;
    }
}
