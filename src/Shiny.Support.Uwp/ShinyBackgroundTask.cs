using System;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public sealed class ShinyBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance) => Router.Run(
            taskInstance,
            "Shiny.UwpShinyHost, Shiny.Core",
            "BackgroundRun"
        );
    }
}
