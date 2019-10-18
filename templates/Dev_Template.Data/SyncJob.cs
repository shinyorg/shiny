using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace $safeprojectname$
{
    public class SyncJob : IJob
    {
        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            return false;
        }
    }
}
