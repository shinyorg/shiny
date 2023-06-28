using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE.Intrastructure;

// The operation queue cannot crash if it holds state, but it needs to bubble it's error out

public interface IOperationQueue
{
    Task Queue(Func<Task> task, CancellationToken cancellToken, string? caller);
}


public class SemaphoreOperationQueue : IOperationQueue
{
    readonly ILogger logger;


    public SemaphoreOperationQueue(ILogger<SemaphoreOperationQueue> logger)
    {
        this.logger = logger;
    }


    readonly SemaphoreSlim semaphore = new(1);

    public async Task Queue(Func<Task> task, CancellationToken cancellationToken, [CallerMemberName] string? caller = null)
    {
        if (this.logger.IsEnabled(LogLevel.Debug))        
            this.logger.LogDebug($"[{caller}] awaiting at BLE operation lock");
        
        await this.semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (this.logger.IsEnabled(LogLevel.Debug))
            this.logger.LogDebug($"[{caller}] past BLE operation lock");

        try
        {
            await task.Invoke().ConfigureAwait(false);
        }
        finally
        {
            this.semaphore.Release();
            if (this.logger.IsEnabled(LogLevel.Debug))
                this.logger.LogDebug($"[{caller}] release BLE operation lock");
        }
    }
}


public static class OperationQueueExtensions
{
    public static IObservable<T> QueueToObservable<T>(this IOperationQueue queue, Func<CancellationToken, Task<T>> func, [CallerMemberName] string? caller = null) => Observable.FromAsync(async ct =>
    {
        T result = default!;
        await queue.Queue(async () => 
        {
            result = await func.Invoke(ct).ConfigureAwait(false);
        }, ct, caller).ConfigureAwait(false);

        return result;
    });
}