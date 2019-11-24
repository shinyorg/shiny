using System;
using Windows.ApplicationModel.Background;


namespace Shiny.Net.Http
{
    public sealed class ShinyBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var host = Type.GetType("Shiny.UwpShinyHost, Shiny.Core");
            var method = host.GetMethod("BackgroundRun");
            method.Invoke(host, new[] { taskInstance });
        }
    }
}
