using System;
using WatchKit;


namespace Shiny.Jobs
{
    //https://developer.apple.com/documentation/watchkit/wkapplicationrefreshbackgroundtask
    public class ShinyRefreshBackgroundTask : WKApplicationRefreshBackgroundTask
    {
    }
}
//namespace MonkeySoccer.MonkeySoccerExtension
//{
//    public class ExtensionDelegate : WKExtensionDelegate
//    {
//        #region Computed Properties
//        public List<WKRefreshBackgroundTask> PendingTasks { get; set; } = new List<WKRefreshBackgroundTask>();
//		#endregion

//		...

//		#region Public Methods
//		public void CompleteTask(WKRefreshBackgroundTask task)
//        {
//            // Mark the task completed and remove from the collection
//            task.SetTaskCompleted();
//            PendingTasks.Remove(task);
//        }
//        #endregion

//        #region Override Methods
//        public override void HandleBackgroundTasks(NSSet<WKRefreshBackgroundTask> backgroundTasks)
//        {
//            // Handle background request
//            foreach (WKRefreshBackgroundTask task in backgroundTasks)
//            {
//                // Is this a background session task?
//                var urlTask = task as WKUrlSessionRefreshBackgroundTask;
//                if (urlTask != null)
//                {
//                    // Create new configuration
//                    var configuration = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(urlTask.SessionIdentifier);

//                    // Create new session
//                    var backgroundSession = NSUrlSession.FromConfiguration(configuration, new BackgroundSessionDelegate(this, task), null);

//                    // Keep track of all pending tasks
//                    PendingTasks.Add(task);
//                }
//                else
//                {
//                    // Ensure that all tasks are completed
//                    task.SetTaskCompleted();
//                }
//            }
//        }
//		#endregion

//		...
//	}
//}


//private void ScheduleNextBackgroundUpdate()
//{
//    // Create a fire date 30 minutes into the future
//    var fireDate = NSDate.FromTimeIntervalSinceNow(30 * 60);

//    // Create
//    var userInfo = new NSMutableDictionary();
//    userInfo.Add(new NSString("LastActiveDate"), NSDate.FromTimeIntervalSinceNow(0));
//    userInfo.Add(new NSString("Reason"), new NSString("UpdateScore"));

//    // Schedule for update
//    WKExtension.SharedExtension.ScheduleBackgroundRefresh(fireDate, userInfo, (error) => {
//        // Was the Task successfully scheduled?
//        if (error == null)
//        {
//            // Yes, handle if needed
//        }
//        else
//        {
//            // No, report error
//        }
//    });
//}

//WKWatchConnectivityRefreshBackgroundTask
//scheduleBackgroundRefresh(withPreferredDate:userInfo:scheduledCompletion:)