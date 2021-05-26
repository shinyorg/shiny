using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;
using Shiny.Jobs.Infrastructure;


namespace Shiny.Jobs
{
    public abstract class AbstractJobManager : IJobManager
    {
        readonly IRepository repository;
        readonly IServiceProvider container;
        readonly Subject<JobRunResult> jobFinished;
        readonly Subject<JobInfo> jobStarted;


        protected AbstractJobManager(IServiceProvider container, IRepository repository, ILogger<IJobManager> logger)
        {
            this.container = container;
            this.repository = repository;
            this.Log = logger;
            this.jobStarted = new Subject<JobInfo>();
            this.jobFinished = new Subject<JobRunResult>();
        }


        protected ILogger<IJobManager> Log { get; }
        public abstract Task<AccessState> RequestAccess();
        protected abstract void RegisterNative(JobInfo jobInfo);
        protected abstract void CancelNative(JobInfo jobInfo);


        public virtual async void RunTask(string taskName, Func<CancellationToken, Task> task)
        {
            try
            {
                this.LogTask(JobState.Start, taskName);
                await task(CancellationToken.None).ConfigureAwait(false);
                this.LogTask(JobState.Finish, taskName);
            }
            catch (Exception ex)
            {
                this.LogTask(JobState.Error, taskName, ex);
            }
        }


        public virtual async Task<JobRunResult> Run(string jobName, CancellationToken cancelToken)
        {
            JobRunResult result;
            JobInfo? actual = null;
            try
            {
                var job = await this.repository.Get<PersistJobInfo>(jobName);
                if (job == null)
                    throw new ArgumentException("No job found named " + jobName);

                actual = PersistJobInfo.FromPersist(job);
                result = await this.RunJob(actual, cancelToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.Log.LogError(ex, "Error running job " + jobName);
                result = new JobRunResult(actual, ex);
            }

            return result;
        }


        public async Task<IEnumerable<JobInfo>> GetJobs()
        {
            var jobs = await this.repository.GetAll<PersistJobInfo>();
            return jobs.Select(PersistJobInfo.FromPersist);
        }


        public async Task<JobInfo?> GetJob(string jobName)
        {
            var job = await this.repository.Get<PersistJobInfo>(jobName);
            if (job == null)
                return null;

            return PersistJobInfo.FromPersist(job);
        }


        public async Task Cancel(string jobIdentifier)
        {
            var job = await this.repository.Get<PersistJobInfo>(jobIdentifier);
            if (job != null)
            {
                this.CancelNative(PersistJobInfo.FromPersist(job));
                await this.repository.Remove<PersistJobInfo>(jobIdentifier);
            }
        }


        public virtual async Task CancelAll()
        {
            var jobs = await this.repository.GetAllWithKeys<PersistJobInfo>();
            foreach (var job in jobs)
            {
                if (!job.Value.IsSystemJob)
                {
                    this.CancelNative(PersistJobInfo.FromPersist(job.Value));
                    await this.repository.Remove<PersistJobInfo>(job.Key);
                }
            }
        }


        public bool IsRunning { get; protected set; }
        public TimeSpan? MinimumAllowedPeriodicTime { get; }
        public IObservable<JobInfo> JobStarted => this.jobStarted;
        public IObservable<JobRunResult> JobFinished => this.jobFinished;


        public async Task Register(JobInfo jobInfo)
        {
            this.ResolveJob(jobInfo);
            this.RegisterNative(jobInfo);
            await this.repository.Set(jobInfo.Identifier, PersistJobInfo.ToPersist(jobInfo));
        }


        public async Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken, bool runSequentially)
        {
            var list = new List<JobRunResult>();

            if (!this.IsRunning)
            {
                try
                {
                    this.IsRunning = true;
                    var jobs = await this.repository.GetAll<PersistJobInfo>();
                    var tasks = new List<Task<JobRunResult>>();

                    if (runSequentially)
                    {
                        foreach (var job in jobs)
                        {
                            var actual = PersistJobInfo.FromPersist(job);
                            var result = await this
                                .RunJob(actual, cancelToken)
                                .ConfigureAwait(false);
                            list.Add(result);
                        }
                    }
                    else
                    {
                        foreach (var job in jobs)
                        {
                            var actual = PersistJobInfo.FromPersist(job);
                            tasks.Add(this.RunJob(actual, cancelToken));
                        }

                        await Task
                            .WhenAll(tasks)
                            .ConfigureAwait(false);
                        list.AddRange(tasks.Select(x => x.Result));
                    }
                }
                catch (Exception ex)
                {
                    this.Log.LogError(ex, "Error running job batch");
                }
                finally
                {
                    this.IsRunning = false;
                }
            }
            return list;
        }


        protected async Task<JobRunResult> RunJob(JobInfo job, CancellationToken cancelToken)
        {
            this.jobStarted.OnNext(job);
            var result = default(JobRunResult);
            var cancel = false;

            try
            {
                this.LogJob(JobState.Start, job);
                var jobDelegate = this.ResolveJob(job);

                await jobDelegate
                    .Run(job, cancelToken)
                    .ConfigureAwait(false);

                if (!job.Repeat)
                {
                    await this.Cancel(job.Identifier);
                    cancel = true;
                }
                this.LogJob(JobState.Finish, job);
                result = new JobRunResult(job, null);
            }
            catch (Exception ex)
            {
                this.LogJob(JobState.Error, job, ex);
                result = new JobRunResult(job, ex);
            }
            finally
            {
                if (!cancel)
                {
                    job.LastRunUtc = DateTime.UtcNow;
                    await this.repository.Set(job.Identifier, PersistJobInfo.ToPersist(job));
                }
            }
            this.jobFinished.OnNext(result);
            return result;
        }


        protected virtual IJob ResolveJob(JobInfo jobInfo)
            => (IJob)ActivatorUtilities.GetServiceOrCreateInstance(this.container, jobInfo.Type);


        protected virtual void LogJob(JobState state,
                                      JobInfo job,
                                      Exception? exception = null)
        {
            if (exception == null)
                this.Log.LogInformation(state == JobState.Finish ? "Job Success" : $"Job {state}", ("JobName", job.Identifier));
            else
                this.Log.LogError(exception, "Error running job " + job.Identifier);
        }


        protected virtual void LogTask(JobState state, string taskName, Exception? exception = null)
        {
            if (exception == null)
                this.Log.LogInformation(state == JobState.Finish ? "Task Success" : $"Task {state}", ("TaskName", taskName));
            else
                this.Log.LogError(exception, "Task failed - " + taskName);
        }
    }
}
