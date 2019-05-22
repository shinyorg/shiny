using System;
using Windows.ApplicationModel.Background;


namespace Shiny.Beacons
{
    public class BeaconBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
            => ShinyHost.Resolve<BackgroundTask>().Run();
    }
}
