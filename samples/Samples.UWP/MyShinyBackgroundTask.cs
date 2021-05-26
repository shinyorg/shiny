using System;
using Windows.ApplicationModel.Background;
using Shiny;


namespace Samples.UWP
{
    public sealed class MyShinyBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
            => this.ShinyRunBackgroundTask(taskInstance, new SampleStartup());
    }
}
