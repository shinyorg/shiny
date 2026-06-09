using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;

namespace Shiny.Data.Sync;


/// <summary>
/// Scheduled job that drains the inbox by pulling all registered endpoints. The platform
/// <see cref="IDataSyncManager"/> picks its native transport — NSURLSession download tasks on
/// Apple, in-process HttpClient elsewhere — so the same job behaves consistently per platform.
/// Registered automatically when <c>AddDataSync</c> is used.
/// </summary>
public class SyncJob(IDataSyncManager manager) : IJob
{
    public Task Run(CancellationToken cancelToken) => manager.PullAll(cancelToken);
}
