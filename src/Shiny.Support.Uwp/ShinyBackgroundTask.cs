using System;
using Windows.ApplicationModel.Background;


namespace Shiny.Support.Uwp
{
    public sealed class ShinyBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
            => UwpShinyHost.Bridge(taskInstance);
    }
}
