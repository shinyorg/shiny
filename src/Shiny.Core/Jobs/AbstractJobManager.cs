using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Logging;
using Shiny.Net;
using Shiny.Power;


namespace Shiny.Jobs
{
    public abstract class AbstractJobManager : IJobManager
    {
        readonly IServiceProvider container;
        readonly IPowerManager powerManager;
        readonly IConnectivity connectivity;


        protected AbstractJobManager(IServiceProvider container,
                                     IRepository repository,
                                     IPowerManager powerManager,
                                     IConnectivity connectivity)
        {
            this.container = container;
            this.Repository = repository;
            this.powerManager = powerManager;
            this.connectivity = connectivity;
        }


        protected IRepository Repository { get; }


        protected virtual bool CheckCriteria(JobInfo job)
        {
            var pluggedIn = this.powerManager.IsPluggedIn();
            if (job.DeviceCharging && !pluggedIn)
                return false;

            if (job.BatteryNotLow && !pluggedIn && this.powerManager.BatteryLevel <= 0.2)
                return false;

            if (job.RequiredInternetAccess == InternetAccess.None)
                return true;

            var hasInternet = this.connectivity.IsInternetAvailable();
            var directConnect = this.connectivity.IsDirectConnect();

            switch (job.RequiredInternetAccess)
            {
                case InternetAccess.None:
                    return true;

                case InternetAccess.Direct:
                    return hasInternet && directConnect;

                case InternetAccess.Any:
                default:
                    return hasInternet;
            }
        }


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
            JobRunResult result = default;
            try
            {
                var job = await this.Repository.Get<JobInfo>(jobName);
                if (job == null)
                    throw new ArgumentException("No job found named " + jobName);

                result = await this.RunJob(job, cancelToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                result = new JobRunResult(false, new JobInfo
                {
                    Identifier = jobName
                }, ex);
            }

            return result;
        }


        public abstract Task<AccessState> RequestAccess();
        public virtual async Task<IEnumerable<JobInfo>> GetJobs() => await this.Repository.GetAll<JobInfo>();
        public Task<JobInfo> GetJob(string jobName) => this.Repository.Get<JobInfo>(jobName);
        public virtual Task Cancel(string jobName) => this.Repository.Remove<JobInfo>(jobName);
        public virtual Task CancelAll() => this.Repository.Clear<JobInfo>();
        public bool IsRunning { get; protected set; }
        public event EventHandler<JobInfo> JobStarted;
        public event EventHandler<JobRunResult> JobFinished;


        public virtual async Task Schedule(JobInfo jobInfo)
        {
            if (String.IsNullOrWhiteSpace(jobInfo.Identifier))
                throw new ArgumentException("No job name defined");

            if (jobInfo.Type == null)
                throw new ArgumentException("Type not set");

            // we do a force resolve here to ensure all is good
            this.ResolveJob(jobInfo);
            await this.Repository.Set(jobInfo.Identifier, jobInfo);
        }


        public virtual async Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken)
        {
            var list = new List<JobRunResult>();

            if (!this.IsRunning)
            {
                try
                {
                    this.IsRunning = true;
                    var jobs = await this.Repository.GetAll<JobInfo>();
                    var tasks = new List<Task<JobRunResult>>();

                    foreach (var job in jobs)
                    {
                        if (this.CheckCriteria(job))
                            tasks.Add(this.RunJob(job, cancelToken));
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    list.AddRange(tasks.Select(x => x.Result));
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
                finally
                {
                    this.IsRunning = false;
                }
            }
            return list;
        }


        protected virtual async Task<JobRunResult> RunJob(JobInfo job, CancellationToken cancelToken)
        {
            this.JobStarted?.Invoke(this, job);
            var result = default(JobRunResult);
            var cancel = false;

            try
            {
                this.LogJob(JobState.Start, job);
                var jobDelegate = this.ResolveJob(job);

                var newData = await jobDelegate
                    .Run(job, cancelToken)
                    .ConfigureAwait(false);

                if (!job.Repeat)
                {
                    await this.Cancel(job.Identifier);
                    cancel = true;
                }
                this.LogJob(JobState.Finish, job);
                result = new JobRunResult(newData, job, null);
            }
            catch (Exception ex)
            {
                this.LogJob(JobState.Error, job, ex);
                result = new JobRunResult(false, job, ex);
            }
            finally
            {
                if (!cancel)
                {
                    job.LastRunUtc = DateTime.UtcNow;
                    await this.Repository.Set(job.Identifier, job);
                }
            }
            this.JobFinished?.Invoke(this, result);
            return result;
        }


        protected virtual IJob ResolveJob(JobInfo jobInfo)
            => (IJob)ActivatorUtilities.GetServiceOrCreateInstance(this.container, jobInfo.Type);


        protected virtual void LogJob(JobState state,
                                      JobInfo job,
                                      Exception exception = null)
        {
            if (exception == null)
                Log.Write("Jobs", "Job Success", ("JobName", job.Identifier));
            else
                Log.Write(exception, ("JobName", job.Identifier));
        }


        protected virtual void LogTask(JobState state, string taskName, Exception exception = null)
        {
            if (exception == null)
                Log.Write("Jobs", "Task Success", ("TaskName", taskName));
            else
                Log.Write(exception, ("TaskName", taskName));
        }
    }
}
