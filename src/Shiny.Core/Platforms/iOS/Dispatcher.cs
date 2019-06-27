using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Logging;
using UIKit;


namespace Shiny
{
    public static class Dispatcher
    {
        public static void Execute(Func<Task> task)
        {
            var taskId = UIApplication.SharedApplication.BeginBackgroundTask(() => { });

            task().ContinueWith(x =>
            {
                if (x.IsFaulted)
                    Log.Write(x.Exception);

                UIApplication.SharedApplication.EndBackgroundTask(taskId);
            });
        }
    }
}
