using Shiny.Data.Sync;

namespace Sample.tvOS.Services;


public class SampleSyncDelegate(AppLog log) : IDataSyncDelegate
{
    public Task OnSent(SyncOperation operation, string? responseBody)
    {
        log.Write($"sync sent {operation.Verb} {operation.EntityIdentifier}");
        return Task.CompletedTask;
    }

    public Task OnError(SyncOperation operation, int statusCode, Exception ex)
    {
        log.Write($"sync failed ({statusCode}) {operation.EntityIdentifier}: {ex.Message}");
        return Task.CompletedTask;
    }

    public Task OnReceived(SyncReceivedItem item)
    {
        log.Write($"sync received {item.Verb} on {item.EndpointKey}");
        return Task.CompletedTask;
    }

    public Task<ConflictResolution> OnConflict(SyncOperation operation, string remotePayload)
    {
        log.Write($"sync conflict on {operation.EntityIdentifier} - keeping local");
        return Task.FromResult(ConflictResolution.KeepLocal);
    }
}
