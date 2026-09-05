using Sample.tvOS.Infrastructure;
using Sample.tvOS.Services;
using Shiny;
using Shiny.Net.Http;

namespace Sample.tvOS.Pages;


/// <summary>
/// The same background NSUrlSession iOS uses, so a transfer survives the app being suspended.
/// The one thing to design around on tvOS is storage: an Apple TV has no user-visible file system
/// and a small, evictable container, so downloads go to the cache directory and are treated as
/// something the OS may reclaim between launches.
/// </summary>
public class TransfersViewController() : ModuleViewController(
    "Shiny.Net.Http - background NSUrlSession. Write to the cache directory: tvOS storage is evictable"
)
{
    protected override void OnReady()
    {
        var log = Resolve<AppLog>();
        log.Written += (_, msg) => this.Log(msg);

        var transfers = Resolve<IHttpTransferManager>();
        transfers.UpdateReceived += (_, result) =>
        {
            var progress = result.IsDeterministic
                ? $"{result.Progress.PercentComplete:P0}"
                : $"{result.Progress.BytesTransferred} bytes";

            this.Log($"{result.Request.Identifier}  {result.Status}  {progress}  ({result.Progress.BytesPerSecond} B/s)");
        };

        this.AddAction("Download 10MB", async () =>
        {
            var platform = Resolve<IPlatform>();
            var identifier = $"sample-{Guid.NewGuid():N}"[..20];

            // Cache, not AppData - a tvOS app container is small and the OS may reclaim it
            var path = Path.Combine(platform.Cache.FullName, $"{identifier}.bin");

            var transfer = await transfers.Queue(new HttpTransferRequest(
                identifier,
                "https://ash-speed.hetzner.com/10MB.bin",
                TransferType.Download,
                path
            ));
            this.Log($"queued {transfer.Identifier} -> {path}");
        });

        this.AddAction("List", async () =>
        {
            var all = await transfers.GetTransfers();
            this.Log($"{all.Count} transfer(s) in the queue");
            foreach (var t in all)
                this.Log($"  {t.Identifier}  {t.Status}  {t.BytesTransferred}/{t.BytesToTransfer?.ToString() ?? "?"}");
        });

        this.AddAction("Cancel all", async () =>
        {
            await transfers.CancelAll();
            this.Log("cancelled everything in the queue");
        });
    }
}
