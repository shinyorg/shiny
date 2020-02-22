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

        //static Handler? handler;
        //public static void Dispatch(this Action action)
        //{
        //    if (handler == null || handler.Looper != Looper.MainLooper)
        //        handler = new Handler(Looper.MainLooper);

        //    handler.Post(action);
        //}
    }
}
