using System.Text.Json;
using Microsoft.JSInterop;
using Together.Core;
namespace Together.Client.Storage;

public sealed class BrowserStore(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? module;
    private async Task<IJSObjectReference> Module() => module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/storage.js");
    public async Task<Workspace> ReadAsync()
    {
        var json = await (await Module()).InvokeAsync<string?>("read");
        return json is null ? new Workspace() : Workspace.Parse(json);
    }
    public async Task<string> WriteAsync(Workspace next, long expectedRevision, Guid tripId, bool comparisonOnly = false)
    {
        var trip = next.Trips.Single(t => t.Id == tripId);
        if (!DataValidation.IsValid(new Workspace { Trips = [trip] }))
            return "invalid";
        // Comparison changes carry only their settings, not unchanged variant data.
        var json = comparisonOnly
            ? JsonSerializer.Serialize(new
            {
                trip.Id,
                trip.Revision,
                trip.UpdatedAt,
                trip.SelectedCriteria,
                trip.SelectedVariantIds
            }, Workspace.JsonOptions)
            : JsonSerializer.Serialize(trip, Workspace.JsonOptions);
        return await (await Module()).InvokeAsync<string>("write", expectedRevision, trip.Revision - 1, json, comparisonOnly);
    }
    public async ValueTask DisposeAsync()
    {
        if (module is not null)
            await module.DisposeAsync();
    }
}
