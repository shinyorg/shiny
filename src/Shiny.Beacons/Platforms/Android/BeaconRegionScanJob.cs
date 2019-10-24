using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;

namespace Shiny.Beacons
{
    public class BeaconRegionScanJob : IJob
    {
        public Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            //ShinyHost
            //    .Resolve<BackgroundTask>()
            //    .Run();
            throw new NotImplementedException();
        }
    }
}
