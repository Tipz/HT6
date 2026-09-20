using System.Text.Json.Serialization;

namespace Together.Core;

public sealed class Variant
{
    [JsonRequired] public Guid Id { get; set; } = Guid.NewGuid();
    [JsonRequired]
    public Guid TripId
    {
        get; set;
    }
    [JsonRequired] public string Name { get; set; } = "";
    [JsonRequired] public string Destination { get; set; } = "";
    [JsonRequired] public string Accommodation { get; set; } = "";
    [JsonRequired] public string SourceUrl { get; set; } = "";
    [JsonRequired] public string RoadDescription { get; set; } = "";
    [JsonRequired] public string Notes { get; set; } = "";
    [JsonRequired]
    public int? TravelMinutes
    {
        get; set;
    }
    [JsonRequired]
    public int? Transfers
    {
        get; set;
    }
    [JsonRequired] public string Kitchen { get; set; } = "unknown";
    [JsonRequired] public string Crib { get; set; } = "unknown";
    [JsonRequired] public string Playground { get; set; } = "unknown";
    [JsonRequired]
    public int? DistanceMeters
    {
        get; set;
    }
    [JsonRequired] public string DistanceTarget { get; set; } = "";
    [JsonRequired] public long?[] Expenses { get; set; } = new long?[6];
    [JsonRequired]
    public bool NeedsBudgetReview
    {
        get; set;
    }
    [JsonRequired] public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    [JsonRequired] public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    [JsonRequired]
    public long Revision
    {
        get; set;
    }
    public Variant Copy()
    {
        var copy = (Variant)MemberwiseClone();
        copy.Expenses = [.. Expenses];
        return copy;
    }
}
