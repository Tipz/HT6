using Together.Core;
namespace Together.Tests;

public sealed class BudgetTests
{
    [Fact]
    public void SixExpensesGiveReferenceTotal()
    {
        long?[] values = [4200000, 5600000, 2400000, 600000, 1000000, 500000];
        Assert.Equal(14300000, Budget.Total(values));
        Assert.True(Budget.Complete(values));
    }
    [Theory]
    [InlineData("42 000,12", 4200012L)]
    [InlineData("0", 0L)]
    [InlineData("0.01", 1L)]
    [InlineData("100000000", 10000000000L)]
    public void MoneyUsesExactKopecks(string input, long expected)
    {
        Assert.True(Budget.TryParse(input, out var value));
        Assert.Equal(expected, value);
    }
    [Theory]
    [InlineData("-1")]
    [InlineData("1.234")]
    [InlineData("Infinity")]
    [InlineData("12abc")]
    [InlineData("100000000.01")]
    [InlineData("1 2")]
    public void InvalidMoneyIsRejected(string input)
    {
        Assert.False(Budget.TryParse(input, out _));
    }
    [Fact]
    public void UnknownIsNotZero()
    {
        Assert.True(Budget.TryParse("", out var unknown));
        Assert.Null(unknown);
        Assert.False(Budget.Complete([0, 0, 0, 0, 0, unknown]));
        Assert.True(Budget.Complete([0, 0, 0, 0, 0, 0]));
    }
}
