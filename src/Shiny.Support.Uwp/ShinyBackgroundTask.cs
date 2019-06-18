using System;
using Windows.ApplicationModel.Background;


namespace Shiny.Support.Uwp
{
    public sealed class ShinyBackgroundTask : IBackgroundTask
    {
        public static IUwpBridge Bridge { get; set; }
        public void Run(IBackgroundTaskInstance taskInstance) => Bridge.Bridge(taskInstance);
    }
}
