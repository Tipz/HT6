using System.Globalization;
using System.Text.RegularExpressions;

namespace Together.Core;

public static partial class Budget
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    public static bool TryParse(string text, out long? kopecks)
    {
        kopecks = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;
        var clean = text.Trim();
        if (!MoneyPattern().IsMatch(clean))
            return false;
        clean = clean.Replace(" ", "").Replace("\u00a0", "").Replace("\u202f", "").Replace(',', '.');
        if (!decimal.TryParse(clean, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rubles) || rubles > 100_000_000)
            return false;
        kopecks = (long)(rubles * 100);
        return true;
    }
    [GeneratedRegex(@"^(?:[0-9]+|[0-9]{1,3}(?:[ \u00a0\u202f][0-9]{3})+)(?:[.,][0-9]{1,2})?$")]
    private static partial Regex MoneyPattern();
    public static string Money(long amount) => (amount / 100m).ToString(amount % 100 == 0 ? "N0" : "N2", Russian) + " ₽";
    public static string Input(long? amount) => amount is null ? "" : (amount.Value / 100m).ToString("0.##", CultureInfo.InvariantCulture);
    public static long Total(long?[] expenses) => expenses.Sum(x => x ?? 0);
    public static bool Complete(long?[] expenses) => expenses.All(x => x.HasValue);
    public static string Summary(long?[] expenses) => expenses.All(x => x is null) ? "Бюджет не заполнен" : Money(Total(expenses));
}
