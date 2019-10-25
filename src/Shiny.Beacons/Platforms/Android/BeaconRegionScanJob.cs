using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;

namespace Shiny.Beacons
{
    public class BeaconRegionScanJob : IJob
    {
        readonly BackgroundTask task;


        public BeaconRegionScanJob(BackgroundTask task) => this.task = task;
        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var tcs = new TaskCompletionSource<object>();
            using (cancelToken.Register(() =>
            {
                this.task.StopScan();
                tcs.SetResult(null);
            }))
            {
                task.StartScan();
                await tcs.Task;
            }
            return true;
        }
    }
}
