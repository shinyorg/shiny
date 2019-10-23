//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using BackgroundTasks;
//using CoreFoundation;

//namespace Shiny.Jobs
//{
//    public class BgTasksJobManager : IJobManager
//    {
//        public const string JobPowerAndNetwork = "1";
//        public const string JobNetwork = "2";


//        public bool IsRunning => throw new NotImplementedException();

//        public event EventHandler<JobInfo> JobStarted;
//        public event EventHandler<JobRunResult> JobFinished;

//        public Task Cancel(string jobName)
//        {
//            throw new NotImplementedException();
//        }

//        public Task CancelAll()
//        {
//            BGTaskScheduler.Shared.CancelAll();
//            return Task.CompletedTask;
//        }

//        public Task<JobInfo> GetJob(string jobIdentifier)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<JobInfo>> GetJobs()
//        {
//            throw new NotImplementedException();
//        }

//        public Task<AccessState> RequestAccess()
//        {
//            throw new NotImplementedException();
//        }

//        public Task<JobRunResult> Run(string jobIdentifier, CancellationToken cancelToken = default)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default)
//        {
//            throw new NotImplementedException();
//        }

//        public void RunTask(string taskName, Func<CancellationToken, Task> task)
//        {
//            throw new NotImplementedException();
//        }

//        public Task Schedule(JobInfo jobInfo)
//        {
//            BGTaskScheduler.Shared.Register(JobPowerAndNetwork, null, task => {
//                var process = task as BGProcessingTask;
//                var refresh = task as BGAppRefreshTask;

//                new BGAppRefreshTaskRequest("").
//                //HandleAppRefresh(task as BGAppRefreshTask);
//            });
//            return Task.CompletedTask;
//        }
//    }
//}


////        // MARK: Registering Launch Handlers for Tasks
////        // Downcast the parameter to an app refresh task as this identifier is used for a refresh request.
////        BGTaskScheduler.Shared.Register(RefreshTaskId, null, task => HandleAppRefresh(task as BGAppRefreshTask));

////        // Downcast the parameter to a processing task as this identifier is used for a processing request.
////        BGTaskScheduler.Shared.Register(CleaningDbTaskId, null, task => HandleDatabaseCleaning(task as BGProcessingTask));

////        public Task Schedule(JobInfo jobInfo)
////        {
////            //BGProcessingTask
////            //BGTask
////            //BGTaskScheduler
////            //BGAppRefreshTask
////            //BGAppRefreshTaskRequest

////            var request = new BGProcessingTaskRequest(jobInfo.Identifier)
////            {
////                RequiresExternalPower = jobInfo.DeviceCharging,
////                RequiresNetworkConnectivity = jobInfo.RequiredInternetAccess != InternetAccess.None
////                //EarliestBeginDate
////            };
////            if (!BGTaskScheduler.Shared.Submit(request, out var error))
////                throw new ArgumentException(error.LocalizedDescription);
////            //var request = new BGProcessingTaskRequest()
////            //BGTaskScheduler.Shared.Register(
////            //    jobInfo.Identifier,
////            //    DispatchQueue.DefaultGlobalQueue,
////            //    task =>
////            //    {
////            //    }
////            //);

////            return Task.CompletedTask;
////        }
////    }
////}
////public class AppDelegate : UIApplicationDelegate
////{
////    static readonly string caveat = "Change me!!";

////    public static string RefreshTaskId { get; } = "com.xamarin.ColorFeed.refresh";
////    public static NSString RefreshSuccessNotificationName { get; } = new NSString($"{RefreshTaskId}.success");

////    public static string CleaningDbTaskId { get; } = "com.xamarin.ColorFeed.cleaning_db";
////    public static NSString CleaningDbSuccessNotificationName { get; } = new NSString($"{CleaningDbTaskId}.success");


////    public override void DidEnterBackground(UIApplication application)
////    {
////        ScheduleAppRefresh();
////        ScheduleDatabaseCleaningIfNeeded();
////    }


////    void ScheduleAppRefresh()
////    {
////        NSNotificationCenter.DefaultCenter.AddObserver(RefreshSuccessNotificationName, RefreshSuccess);

////        var request = new BGAppRefreshTaskRequest(RefreshTaskId)
////        {
////            EarliestBeginDate = (NSDate)DateTime.Now.AddMinutes(15) // Fetch no earlier than 15 minutes from now
////        };

////        BGTaskScheduler.Shared.Submit(request, out NSError error);

////        if (error != null)
////            Debug.WriteLine($"Could not schedule app refresh: {error}");
////    }

////    void ScheduleDatabaseCleaningIfNeeded()
////    {
////        var lastCleanDate = DBManager.SharedInstance.LastCleaned ?? DateTime.MinValue;
////        var now = DateTime.Now;

////        // Clean the database at most once per week.
////        if (now <= lastCleanDate.AddDays(7))
////            return;

////        var request = new BGProcessingTaskRequest(CleaningDbTaskId)
////        {
////            RequiresNetworkConnectivity = false,
////            RequiresExternalPower = true
////        };

////        BGTaskScheduler.Shared.Submit(request, out NSError error);

////        if (error != null)
////            Debug.WriteLine($"Could not schedule app refresh: {error}");
////    }

////    #endregion

////    #region Handling Launch for Tasks

////    // Fetch the latest feed entries from server.
////    void HandleAppRefresh(BGAppRefreshTask task)
////    {
////        ScheduleAppRefresh();

////        task.ExpirationHandler = () => operations.CancelOperations();

////        operations.FetchLatestPosts(task);
////    }

////    void RefreshSuccess(NSNotification notification)
////    {
////        NSNotificationCenter.DefaultCenter.RemoveObserver(RefreshSuccessNotificationName);
////        var task = notification.Object as BGAppRefreshTask;
////        task?.SetTaskCompleted(true);
////    }

////    // Delete feed entries older than one day.
////    void HandleDatabaseCleaning(BGProcessingTask task)
////    {
////        var beforeDate = DateTime.Now.AddDays(-1);

////        task.ExpirationHandler = () => operations.CancelOperations();

////        operations.DeletePosts(beforeDate, task);
////    }

////    private void Operations_PostsDeleted(object sender, PostsDeletedEventArgs e)
////    {
////        DBManager.SharedInstance.LastCleaned = DateTime.Now;
////        e.Task?.SetTaskCompleted(true);
////    }