using System;
using Windows.ApplicationModel.Background;
using Shiny;


namespace Shiny.Support.Uwp
{
    public sealed class ShinyBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
            => ShinyHost.Resolve<UwpContext>().Bridge(taskInstance);
    }
}
