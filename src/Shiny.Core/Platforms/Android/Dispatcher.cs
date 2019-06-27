using System;
using System.Threading.Tasks;
using Android.Content;
using Shiny.Logging;


namespace Shiny
{
    public static class Dispatcher
    {
        public static void Execute(this BroadcastReceiver receiver, Func<Task> task)
        {
            var pendingResult = receiver.GoAsync();
            task().ContinueWith(x =>
            {
                if (x.IsFaulted)
                    Log.Write(x.Exception);

                pendingResult.Finish();
            });
        }
    }
}
