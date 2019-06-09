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
            taskInstance.GetDeferral(); // run indefinitely - need to figure this out
            this.task.Run(); // TODO: this will execute and end - need to fix as this guy runs almost indefinitely
        }
    }
}
