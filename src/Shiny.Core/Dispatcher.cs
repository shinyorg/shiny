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
                Task.Run(async () =>
                {
                    try
                    {
                        await functor().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        onError?.SafeExecute(ex);
                        Log.Write(ex);
                    }
                    finally
                    {
                        block.Set();
                    }
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
