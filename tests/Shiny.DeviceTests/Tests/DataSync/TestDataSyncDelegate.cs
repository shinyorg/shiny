using Shiny.Data.Sync;

namespace Shiny.DeviceTests.Tests;


public record SyncSent(SyncOperation Operation, string? ResponseBody);
public record SyncFailed(SyncOperation Operation, int StatusCode, Exception Exception);
public record SyncConflict(SyncOperation Operation, string RemotePayload);


// what OnConflict answers - each conflict test sets it before provoking the 409
public class DataSyncTestState
{
    public Func<SyncOperation, string, ConflictResolution> ConflictResolver { get; set; } = (_, _) => ConflictResolution.AcceptRemote;
}


public partial class TestDataSyncDelegate(EventHub events, DataSyncTestState state) : IDataSyncDelegate
{
    public const string SentTopic = "sync.sent";
    public const string ErrorTopic = "sync.error";
    public const string ReceivedTopic = "sync.received";
    public const string ConflictTopic = "sync.conflict";


    public Task OnSent(SyncOperation operation, string? responseBody)
    {
        events.Publish(SentTopic, new SyncSent(operation, responseBody));
        return Task.CompletedTask;
    }

    public Task OnError(SyncOperation operation, int statusCode, Exception ex)
    {
        events.Publish(ErrorTopic, new SyncFailed(operation, statusCode, ex));
        return Task.CompletedTask;
    }

    public Task OnReceived(SyncReceivedItem item)
    {
        events.Publish(ReceivedTopic, item);
        return Task.CompletedTask;
    }

    public Task<ConflictResolution> OnConflict(SyncOperation operation, string remotePayload)
    {
        events.Publish(ConflictTopic, new SyncConflict(operation, remotePayload));
        return Task.FromResult(state.ConflictResolver(operation, remotePayload));
    }
}


#if ANDROID
public partial class TestDataSyncDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        builder.SetContentTitle("Shiny Device Tests");
        builder.SetContentText("Running data sync tests");
    }
}
#endif
