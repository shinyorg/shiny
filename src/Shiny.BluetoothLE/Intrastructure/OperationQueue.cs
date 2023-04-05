using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Intrastructure;

// The operation queue cannot crash if it holds state, but it needs to bubble it's error out

public interface IOperationQueue
{
    Task Queue(Func<Task> task); 
}


public class FallthroughOperationQueue : IOperationQueue
{
    public Task Queue(Func<Task> task) => task.Invoke();
}


public class SemaphoreOperationQueue : IOperationQueue
{
    readonly SemaphoreSlim semaphore = new(1);

    public async Task Queue(Func<Task> task)
    {
        await this.semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            await task.Invoke().ConfigureAwait(false);
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}


//public static class OperationQueueExtensions
//{
//    public static IObservable<T> Execute<T>(this IOperationQueue queue, Func<Task> task)
//    {

//    }
//}