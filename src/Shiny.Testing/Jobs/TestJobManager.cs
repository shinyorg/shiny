using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Testing.Jobs
{
    public class TestJobManager : IJobManager
    {
        public bool IsRunning { get; set; }

        public Subject<JobInfo> JobStartedSubject { get; } = new Subject<JobInfo>();
        public Subject<JobRunResult> JobFinishedSubject { get; } = new Subject<JobRunResult>();

        public IObservable<JobInfo> JobStarted => this.JobStartedSubject;
        public IObservable<JobRunResult> JobFinished => this.JobFinishedSubject;

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

        public TimeSpan? MinimumAllowedPeriodicTime => throw new NotImplementedException();

        public Task<AccessState> RequestAccess() => Task.FromResult(this.ReturnStatus);


        public Task<JobRunResult> Run(string jobIdentifier, CancellationToken cancelToken = default)
        {
            this.IsRunning = true;

            this.IsRunning = false;
            return Task.FromResult(new JobRunResult(true, null, null));
        }

        public Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default, bool runSequentially = false)
        {
            this.IsRunning = true;

            this.IsRunning = false;
            return Task.FromResult(Enumerable.Empty<JobRunResult>());
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
