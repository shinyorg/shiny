using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Testing.Jobs
{
    public class TestJobManager : IJobManager
    {
        public bool IsRunning => throw new NotImplementedException();
        public event EventHandler<JobInfo> JobStarted;
        public event EventHandler<JobRunResult> JobFinished;

        public Task Cancel(string jobName) => Task.CompletedTask;
        public Task CancelAll() => Task.CompletedTask;

        public Task<JobInfo> GetJob(string jobIdentifier)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<JobInfo>> GetJobs()
        {
            throw new NotImplementedException();
        }


        public AccessState ReturnStatus { get; set; } = AccessState.Available;
        public Task<AccessState> RequestAccess() => Task.FromResult(this.ReturnStatus);


        public Task<JobRunResult> Run(string jobIdentifier, CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        public void RunTask(string taskName, Func<CancellationToken, Task> task)
        {
            throw new NotImplementedException();
        }


        public Task Schedule(JobInfo jobInfo)
        {
            throw new NotImplementedException();
        }
    }
}
