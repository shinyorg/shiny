using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;
using UIKit;


namespace Shiny.Jobs
{
    public class JobManagerImpl : AbstractJobManager
    {
        /// <summary>
        /// If you don't know what this does, don't touch it :)
        /// </summary>
        public static double? BackgroundFetchInterval { get; set;}


        public JobManagerImpl(IServiceProvider container,
                              IRepository repository,
                              IPowerManager powerManager,
                              IConnectivity connectivity) : base(container, repository, powerManager, connectivity)
        {
        }


        protected override bool CheckCriteria(JobInfo job) => true;


        public override Task<AccessState> RequestAccess()
        {
            if (!Permissions.HasBackgroundMode("fetch"))
                return Task.FromResult(AccessState.NotSetup);

            var app = UIApplication.SharedApplication;
            var fetch = BackgroundFetchInterval ?? UIApplication.BackgroundFetchIntervalMinimum;
            app.SetMinimumBackgroundFetchInterval(fetch);
            var status = app.BackgroundRefreshStatus;
            var grantResult = AccessState.Unknown;

            switch (status)
            {
                case UIBackgroundRefreshStatus.Available:
                    grantResult = AccessState.Available;
                    break;

                case UIBackgroundRefreshStatus.Denied:
                    grantResult = AccessState.Denied;
                    break;

                case UIBackgroundRefreshStatus.Restricted:
                    grantResult = AccessState.Restricted;
                    break;
            }

            return Task.FromResult(grantResult);
        }


        public override async Task<JobRunResult> Run(string jobName, CancellationToken cancelToken)
        {
            using (var cancelSrc = new CancellationTokenSource())
            {
                using (cancelToken.Register(() => cancelSrc.Cancel()))
                {
                    var app = UIApplication.SharedApplication;
                    var taskId = (int)app.BeginBackgroundTask(jobName, cancelSrc.Cancel);
                    var result = await base.Run(jobName, cancelSrc.Token);
                    app.EndBackgroundTask(taskId);
                    return result;
                }
            }
        }


        public override async void RunTask(string taskName, Func<CancellationToken, Task> task)
        {
            var app = UIApplication.SharedApplication;
            var taskId = 0;
            try
            {
                using (var cancelSrc = new CancellationTokenSource())
                {
                    taskId = (int)app.BeginBackgroundTask(taskName, cancelSrc.Cancel);
                    this.LogTask(JobState.Start, taskName);
                    await task(cancelSrc.Token).ConfigureAwait(false);
                    this.LogTask(JobState.Finish, taskName);
                }
            }
            catch (Exception ex)
            {
                this.LogTask(JobState.Error, taskName, ex);
            }
            finally
            {
                app.EndBackgroundTask(taskId);
            }
        }
    }
}
