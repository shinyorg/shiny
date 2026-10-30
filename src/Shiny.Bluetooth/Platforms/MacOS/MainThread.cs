using CoreFoundation;
using Foundation;

namespace Shiny.Bluetooth.MacOS;


/// <summary>
/// IOBluetooth delivers every callback on the run loop of the thread that started the operation, so
/// every call goes through the main thread - the one run loop an AppKit app is guaranteed to spin.
/// </summary>
internal static class MainThread
{
    public static Task<T> Run<T>(Func<T> func)
    {
        if (NSThread.IsMain)
        {
            try
            {
                return Task.FromResult(func());
            }
            catch (Exception ex)
            {
                return Task.FromException<T>(ex);
            }
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }


    public static Task Run(Action action) => Run(() =>
    {
        action();
        return true;
    });
}
