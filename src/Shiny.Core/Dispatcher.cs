using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Logging;


namespace Shiny
{
    public static class Dispatcher
    {
        public static void SmartExecuteSync(Func<Task> functor, Action<Exception> onError = null)
        {
            using (var block = new ManualResetEvent(false))
            {
                functor().ContinueWith(x =>
                {
                    if (x.IsFaulted)
                    {
                        onError?.SafeExecute(x.Exception);
                        Log.Write(x.Exception);
                    }
                    block.Set();
                });
                block.WaitOne();
            }
        }


        public static void SafeExecute(this Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        public static void SafeExecute<T>(this Action<T> action, T args)
        {
            try
            {
                action.Invoke(args);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
