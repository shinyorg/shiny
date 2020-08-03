using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.DataSync.Infrastructure
{
    public class SyncJob : IJob
    {
        readonly IDataSyncDelegate sdelegate;


        public SyncJob(IDataSyncDelegate sdelegate)
        {
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            return true;
        }
    }
}
