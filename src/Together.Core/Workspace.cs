using System.Text.Json;
using System.Text.Json.Serialization;

namespace Together.Core;

public sealed class Workspace
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    [JsonRequired] public int SchemaVersion { get; set; } = 1;
    [JsonRequired]
    public long Revision
    {
        get; set;
    }
    [JsonRequired] public List<Trip> Trips { get; set; } = [];
    public Workspace Copy() => new() { SchemaVersion = SchemaVersion, Revision = Revision, Trips = Trips.Select(t => t.Copy()).ToList() };
    public static Workspace Parse(string json)
    {
        var data = JsonSerializer.Deserialize<Workspace>(json, JsonOptions) ?? throw new JsonException("Пустая запись");
        if (!DataValidation.IsValid(data))
            throw new JsonException("Повреждённые данные или несовместимая версия");
        return data;
    }
}
