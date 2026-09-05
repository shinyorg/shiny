using Sample.tvOS.Infrastructure;
using Sample.tvOS.Services;
using Shiny.Data.Sync;

namespace Sample.tvOS.Pages;


/// <summary>
/// Outbound-only sync of what was watched, over the same background NSUrlSession iOS uses, so a
/// queued operation survives the app being killed. The endpoint here is httpbin, which echoes
/// whatever it is sent - enough to watch the outbox actually drain.
/// </summary>
public class DataSyncViewController() : ModuleViewController(
    "Shiny.Data.Sync - NSUrlSession outbox. Queued work survives the app being killed"
)
{
    int counter;


    protected override void OnReady()
    {
        var log = Resolve<AppLog>();
        log.Written += (_, msg) => this.Log(msg);

        var sync = Resolve<IDataSyncManager>();
        sync.PendingCountChanged += (_, count) => this.Log($"pending operations: {count}");
        sync.Activity += (_, evt) => this.Log($"activity: {evt.Type} on {evt.EndpointKey} {evt.Operation?.EntityIdentifier ?? ""}".TrimEnd());

        this.AddAction("Queue a viewing", async () =>
        {
            var viewing = new Viewing(
                $"viewing-{++this.counter}",
                $"Episode {this.counter}",
                this.counter * 120,
                DateTimeOffset.Now
            );

            var op = await sync.Queue(SyncVerb.Create, viewing);
            this.Log($"queued {op.Identifier} ({viewing.Title})");
        });

        this.AddAction("Pending", async () =>
        {
            var pending = await sync.GetPending();
            this.Log($"{pending.Count} pending operation(s)");
            foreach (var op in pending)
                this.Log($"  {op.EntityIdentifier}  {op.State}  attempts={op.Attempts}  {op.LastError ?? ""}".TrimEnd());
        });

        this.AddAction("Cancel all", async () =>
        {
            await sync.CancelAll();
            this.Log("outbox cleared");
        });
    }
}
