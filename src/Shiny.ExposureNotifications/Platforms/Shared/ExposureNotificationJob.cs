using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.ExposureNotifications
{
    public class ExposureNotificationJob : IJob
    {
        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            return true;
        }
    }
}
