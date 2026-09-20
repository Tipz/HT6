namespace Together.Core;

public sealed class VariantDraft
{
    public string Name { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Accommodation { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string RoadDescription { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Hours { get; set; } = "";
    public string Minutes { get; set; } = "";
    public string Transfers { get; set; } = "";
    public string Kitchen { get; set; } = "unknown";
    public string Crib { get; set; } = "unknown";
    public string Playground { get; set; } = "unknown";
    public string DistanceMeters { get; set; } = "";
    public string DistanceTarget { get; set; } = "";
    public string[] Expenses { get; set; } = new string[6];
    public bool BudgetReviewed
    {
        get; set;
    }
    public static VariantDraft From(Variant v) => new()
    {
        Name = v.Name,
        Destination = v.Destination,
        Accommodation = v.Accommodation,
        SourceUrl = v.SourceUrl,
        RoadDescription = v.RoadDescription,
        Notes = v.Notes,
        Hours = (v.TravelMinutes / 60)?.ToString() ?? "",
        Minutes = (v.TravelMinutes % 60)?.ToString() ?? "",
        Transfers = v.Transfers?.ToString() ?? "",
        Kitchen = v.Kitchen,
        Crib = v.Crib,
        Playground = v.Playground,
        DistanceMeters = v.DistanceMeters?.ToString() ?? "",
        DistanceTarget = v.DistanceTarget,
        Expenses = v.Expenses.Select(Budget.Input).ToArray()
    };
    public long?[] ParsedExpenses() => Expenses.Select(x => Budget.TryParse(x, out var amount) ? amount : null).ToArray();
    private static bool OptionalInteger(string text, int max) => string.IsNullOrWhiteSpace(text) || (int.TryParse(text, out var number) && number >= 0 && number <= max);
    public Dictionary<string, string> Validate()
    {
        var errors = new Dictionary<string, string>();
        foreach (var (key, value) in new[] { ("name", Name), ("destination", Destination), ("accommodation", Accommodation) })
            if (!DataValidation.Text(value.Trim(), 150))
                errors[key] = "Обязательное поле, от 1 до 150 символов";
        if (!DataValidation.Url(SourceUrl.Trim()))
            errors["url"] = "Укажите ссылку http или https (до 2048 символов)";
        if (!OptionalInteger(Hours, 1666))
            errors["hours"] = "Укажите целое число часов от 0 до 1666";
        if (!OptionalInteger(Minutes, 59))
            errors["minutes"] = "Укажите целое число минут от 0 до 59";
        if (!errors.ContainsKey("hours") && !errors.ContainsKey("minutes") && (Parse(Hours) ?? 0) * 60 + (Parse(Minutes) ?? 0) > 100000)
            errors["hours"] = "Время не должно превышать 100 000 минут";
        if (!OptionalInteger(Transfers, 20))
            errors["transfers"] = "Укажите целое число от 0 до 20";
        if (!OptionalInteger(DistanceMeters, 1000000))
            errors["distance"] = "Укажите целое число от 0 до 1 000 000 м";
        if (string.IsNullOrWhiteSpace(DistanceMeters) != string.IsNullOrWhiteSpace(DistanceTarget) || DistanceTarget.Trim().Length > 100)
            errors["target"] = "Заполните расстояние и цель вместе; цель — до 100 символов";
        if (Notes.Length > 2000)
            errors["notes"] = "Не более 2000 символов";
        if (RoadDescription.Length > 2000)
            errors["road"] = "Не более 2000 символов";
        for (var i = 0; i < Expenses.Length; i++)
            if (!Budget.TryParse(Expenses[i], out _))
                errors[$"expense-{i}"] = "Сумма от 0 до 100 000 000 ₽, не более двух знаков после запятой";
        return errors;
    }
    private static int? Parse(string text) => string.IsNullOrWhiteSpace(text) ? null : int.Parse(text);
    public void Apply(Variant v)
    {
        v.Name = Name.Trim();
        v.Destination = Destination.Trim();
        v.Accommodation = Accommodation.Trim();
        v.SourceUrl = SourceUrl.Trim();
        v.RoadDescription = RoadDescription.Trim();
        v.Notes = Notes.Trim();
        v.Transfers = Parse(Transfers);
        v.TravelMinutes = string.IsNullOrWhiteSpace(Hours) && string.IsNullOrWhiteSpace(Minutes) ? null : (Parse(Hours) ?? 0) * 60 + (Parse(Minutes) ?? 0);
        v.Kitchen = Kitchen;
        v.Crib = Crib;
        v.Playground = Playground;
        v.DistanceMeters = Parse(DistanceMeters);
        v.DistanceTarget = DistanceTarget.Trim();
        v.Expenses = ParsedExpenses();
        if (BudgetReviewed)
            v.NeedsBudgetReview = false;
        v.Revision++;
        v.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
