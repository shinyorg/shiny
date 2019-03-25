using System;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Jobs
{
    public interface IJob
    {
        /// <summary>
        /// Runs your code
        /// </summary>
        /// <returns>Return true if new data is received, otherwise false (don't like about this or iOS will haunt you)</returns>
        /// <param name="jobInfo"></param>
        /// <param name="cancelToken"></param>
        Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken);
    }
}
