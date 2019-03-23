using System;
using Windows.ApplicationModel.Background;


namespace Shiny.Beacons
{
    public class BeaconBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var beaconDelegate = ShinyHost.Resolve<IBeaconDelegate>();
        }
    }
}
