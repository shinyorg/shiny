using System;
using Windows.ApplicationModel.Background;


namespace Shiny.BluetoothLE
{
    public sealed class ShinyBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var host = Type.GetType("Shiny.BluetoothLE, Shiny.BluetoothLE");
            var method = host.GetMethod("BackgroundRun");
            method.Invoke(host, new[] { taskInstance });
        }
    }
}
