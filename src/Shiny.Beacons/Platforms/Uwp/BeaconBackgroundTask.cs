using System;
using Windows.ApplicationModel.Background;


namespace Shiny.Beacons
{
    public class BeaconBackgroundTask : IBackgroundTaskProcessor
    {
        readonly BackgroundTask task;
        public BeaconBackgroundTask(BackgroundTask task) => this.task = task;


        public void Process(IBackgroundTaskInstance taskInstance)
        {
            // this task basically runs indefinitely
            taskInstance.GetDeferral();
            taskInstance.Canceled += (obj, sender) => this.task.StopScan();
            this.task.Run();
        }
    }
}
