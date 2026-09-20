using System.Text.Json;
using Together.Core;
namespace Together.Tests;

public sealed class ValidationTests
{
    private static Trip Trip() => new() { Name = "Лето", StartDate = new(2027, 7, 12), EndDate = new(2027, 7, 19), ChildAges = [3, 8] };
    [Fact]
    public void NightsUseCalendarDates()
    {
        Assert.Equal(7, Trip().Nights);
    }
    [Fact]
    public void InvalidDatesAndChildAgeRemainInDraft()
    {
        var draft = TripDraft.From(Trip());
        draft.EndDate = draft.StartDate;
        draft.ChildAges[0] = "";
        var errors = draft.Validate();
        Assert.Contains("end", errors.Keys);
        Assert.Contains("child-0", errors.Keys);
        Assert.Equal("Лето", draft.Name);
    }
    [Fact]
    public void FamilyChangeMarksBudgetWithoutChangingAmounts()
    {
        var trip = Trip();
        var variant = new Variant { TripId = trip.Id, Expenses = [1, 2, 3, 4, 5, 6] };
        trip.Variants.Add(variant);
        var draft = TripDraft.From(trip);
        draft.ChildAges[0] = "4";
        draft.Apply(trip);
        Assert.True(variant.NeedsBudgetReview);
        Assert.Equal(21, Budget.Total(variant.Expenses));
        var variantDraft = VariantDraft.From(variant);
        variantDraft.BudgetReviewed = true;
        variantDraft.Apply(variant);
        Assert.False(variant.NeedsBudgetReview);
    }
    [Fact]
    public void RenameDoesNotRequireReview()
    {
        var trip = Trip();
        trip.Variants.Add(new Variant { TripId = trip.Id });
        var draft = TripDraft.From(trip);
        draft.Name = "Новое название";
        draft.Apply(trip);
        Assert.False(trip.Variants[0].NeedsBudgetReview);
    }
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hello")]
    [InlineData("/relative")]
    [InlineData("https://")]
    public void UnsafeUrlsAreRejected(string url)
    {
        var draft = new VariantDraft { Name = "Вариант", Destination = "Море", Accommodation = "Отель", SourceUrl = url };
        Assert.Contains("url", draft.Validate().Keys);
    }
    [Fact]
    public void DistanceNeedsTargetAndTravelHasBound()
    {
        var draft = new VariantDraft { DistanceMeters = "100", Hours = "1666", Minutes = "41" };
        Assert.Contains("target", draft.Validate().Keys);
        Assert.Contains("hours", draft.Validate().Keys);
    }
    [Fact]
    public void MissingFieldsAndUnknownSchemaAreRejected()
    {
        Assert.Throws<JsonException>(() => Workspace.Parse("{}"));
        Assert.Throws<JsonException>(() => Workspace.Parse("""{"schemaVersion":2,"revision":0,"trips":[]}"""));
        Assert.Throws<JsonException>(() => Workspace.Parse("""{"schemaVersion":1,"revision":0,"trips":null}"""));
    }
    [Fact]
    public void SelectionCannotReferenceAnotherTrip()
    {
        var trip = Trip();
        trip.SelectedVariantIds.Add(Guid.NewGuid());
        Assert.False(DataValidation.IsValid(new Workspace { Trips = [trip] }));
    }
    [Fact]
    public void CopyIsIndependent()
    {
        var trip = Trip();
        var original = new Workspace { Trips = [trip] };
        var copy = original.Copy();
        copy.Trips[0].ChildAges[0] = 5;
        Assert.Equal(3, trip.ChildAges[0]);
    }
    [Fact]
    public void CopyDoesNotShareVariantExpensesOrComparisonSettings()
    {
        var trip = Trip();
        trip.Variants.Add(new Variant { TripId = trip.Id, Expenses = [1, 2, 3, 4, 5, 6] });
        trip.SelectedVariantIds.Add(trip.Variants[0].Id);
        var copy = new Workspace { Trips = [trip] }.Copy();
        copy.Trips[0].Variants[0].Expenses[0] = 99;
        copy.Trips[0].SelectedVariantIds.Clear();
        copy.Trips[0].SelectedCriteria.Clear();
        Assert.Equal(1, trip.Variants[0].Expenses[0]);
        Assert.Single(trip.SelectedVariantIds);
        Assert.Equal(7, trip.SelectedCriteria.Count);
    }
    [Fact]
    public void ExampleDataIsValidAndReadyForComparison()
    {
        var example = ExampleData.Create();

        Assert.True(DataValidation.IsValid(example));
        var trip = Assert.Single(example.Trips);
        Assert.Equal(3, trip.Variants.Count);
        Assert.Equal(3, trip.SelectedVariantIds.Count);
        Assert.All(trip.Variants, variant => Assert.True(Budget.Complete(variant.Expenses)));
    }
}
