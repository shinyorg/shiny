using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackgroundTasks;
using CoreFoundation;

namespace Shiny.Jobs
{
    public class BgTasksJobManager : IJobManager
    {
        public bool IsRunning => throw new NotImplementedException();

        public event EventHandler<JobInfo> JobStarted;
        public event EventHandler<JobRunResult> JobFinished;

        public Task Cancel(string jobName)
        {
            throw new NotImplementedException();
        }

        public Task CancelAll()
        {
            BGTaskScheduler.Shared.CancelAll();
            return Task.CompletedTask;
        }

        public Task<JobInfo> GetJob(string jobIdentifier)
        {

            throw new NotImplementedException();
        }

        public Task<IEnumerable<JobInfo>> GetJobs()
        {
            throw new NotImplementedException();
        }

        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }

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
            //BGProcessingTask
            //BGTask
            //BGTaskScheduler
            //BGAppRefreshTask
            //BGAppRefreshTaskRequest

            var request = new BGProcessingTaskRequest(jobInfo.Identifier)
            {
                RequiresExternalPower = jobInfo.DeviceCharging,
                RequiresNetworkConnectivity = jobInfo.RequiredInternetAccess != InternetAccess.None
                //EarliestBeginDate
            };
            if (!BGTaskScheduler.Shared.Submit(request, out var error))
                throw new ArgumentException(error.LocalizedDescription);
            //var request = new BGProcessingTaskRequest()
            //BGTaskScheduler.Shared.Register(
            //    jobInfo.Identifier,
            //    DispatchQueue.DefaultGlobalQueue,
            //    task =>
            //    {
            //    }
            //);

            return Task.CompletedTask;
        }
    }
}
