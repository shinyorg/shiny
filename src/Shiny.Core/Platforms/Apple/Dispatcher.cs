using System;
using System.Threading.Tasks;
using Foundation;
using Shiny.Logging;


namespace Shiny
{
    public static class Dispatcher
    {

        public static void ExecuteBackgroundTask(Func<Task> task)
        {
#if __IOS__ || __TVOS__
            var taskId = UIKit.UIApplication.SharedApplication.BeginBackgroundTask(() => { });

            task().ContinueWith(x =>
            {
                if (x.IsFaulted)
                    Log.Write(x.Exception);

                UIKit.UIApplication.SharedApplication.EndBackgroundTask(taskId);
            });
#else
            task().ContinueWith(x =>
            {
                if (x.IsFaulted)
                    Log.Write(x.Exception);
            });
#endif
        }


#if __IOS__
        public static async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func)
        {
            if (NSThread.Current.IsMainThread)
                return func();

            var tcs = new TaskCompletionSource<T>();
            NSRunLoop.Main.BeginInvokeOnMainThread(() =>
            {
                var result = func();
                tcs.SetResult(result);
            });
            return await tcs.Task;
        }


        public static Task InvokeOnMainThreadAsync(Action action) => InvokeOnMainThreadAsync<object>(() =>
        {
            action();
            return null;
        });


        public static Task<T> InvokeOnMainThread<T>(Action<TaskCompletionSource<T>> action)
        {
            var tcs = new TaskCompletionSource<T>();
            var execute = new Action(() =>
            {
                try
                {
                    action(tcs);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            if (NSThread.Current.IsMainThread)
            {
                execute();
            }
            else
            {
                NSRunLoop.Main.BeginInvokeOnMainThread(execute);
            }
            return tcs.Task;
        }
#endif
    }
}
