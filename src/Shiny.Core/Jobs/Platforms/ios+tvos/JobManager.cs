using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;
using UIKit;


namespace Shiny.Jobs
{
    public class JobManager : AbstractJobManager
    {
        /// <summary>
        /// If you don't know what this does, don't touch it :)
        /// </summary>
        public static double? BackgroundFetchInterval { get; set; }
        readonly IPlatform platform;


        public JobManager(
            IPlatform platform,
            IServiceProvider container,
            IRepository repository,
            ILogger<IJobManager> logger
        ) : base(
            container,
            repository,
            logger
        )
        {
            this.platform = platform;
        }


        protected override void RegisterNative(JobInfo jobInfo) { }
        protected override void CancelNative(JobInfo jobInfo) { }


        public override async Task<AccessState> RequestAccess()
        {
            if (!PlatformExtensions.HasBackgroundMode("fetch"))
                return AccessState.NotSetup;

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

            await this.platform.InvokeOnMainThreadAsync(() =>
                UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(
                    BackgroundFetchInterval ?? UIApplication.BackgroundFetchIntervalMinimum
                )
            );

            //UIApplication.SharedApplication.ObserveValue(UIApplication.BackgroundRefreshStatusDidChangeNotification)

            return grantResult;
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


        public static async void OnBackgroundFetch(Action<UIBackgroundFetchResult> completionHandler)
        {
            var jobManager = ShinyHost.Resolve<IJobManager>();

            var result = UIBackgroundFetchResult.NoData;
            var app = UIApplication.SharedApplication;
            var taskId = 0;

            try
            {
                using (var cancelSrc = new CancellationTokenSource())
                {
                    taskId = (int)app.BeginBackgroundTask("RunAll", cancelSrc.Cancel);
                    var results = await jobManager
                        .RunAll(cancelSrc.Token, true)
                        .ConfigureAwait(false);

                    result = UIBackgroundFetchResult.NewData;
                }
            }
            catch (Exception ex)
            {
                result = UIBackgroundFetchResult.Failed;
                ShinyHost.LoggerFactory.CreateLogger<ILogger<IJobManager>>().LogError(ex, "Failed to run background fetch");
            }
            finally
            {
                completionHandler(result);
                app.EndBackgroundTask(taskId);
            }
        }
    }
}
