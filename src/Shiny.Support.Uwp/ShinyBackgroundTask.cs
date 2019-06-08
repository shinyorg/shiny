using System;
using System.Threading;
using Windows.ApplicationModel.Background;
using Shiny;


namespace Shiny.Support.Uwp
{
    public sealed class ShinyBackgroundTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            using (var cancelSrc = new CancellationTokenSource())
            {
                taskInstance.Canceled += (sender, args) => cancelSrc.Cancel();
                var processors = ShinyHost.ResolveAll<IBackgroundTaskProcessor>();
                foreach (var processor in processors)
                    await processor.Process(taskInstance, cancelSrc.Token);

                deferral.Complete();
            }
        }
    }
}
