using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;
using Shiny.Logging;


namespace Shiny.DataSync.Infrastructure
{
    public class SyncJob : IJob
    {
        public static string JobName { get; } = $"{typeof(SyncJob).Namespace}.{typeof(SyncJob).Name}";
        readonly IDataSyncManager manager;
        readonly IDataSyncDelegate sdelegate;


        public SyncJob(IDataSyncManager manager, IDataSyncDelegate sdelegate)
        {
            this.manager = manager;
            this.sdelegate = sdelegate;
        }


        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            if (!this.manager.Enabled)
                return;

            var items = await this.manager.GetPendingItems();
            foreach (var item in items)
            {
                try
                {
                    await this.sdelegate.Push(item);
                    await this.manager.Remove(item.Id);
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            }
        }
    }
}
