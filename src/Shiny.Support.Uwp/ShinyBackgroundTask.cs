using System;
using Windows.ApplicationModel.Background;


namespace Shiny.Support.Uwp
{
    public sealed class ShinyBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var host = Type.GetType("Shiny.UwpPlatform, Shiny.Core");
            var method = host.GetMethod("BackgroundRun");
            method.Invoke(host, new[] { taskInstance });
        }
    }
}
