using System;

namespace Shiny.Net.Http;


/// <summary>
/// BCL IObservable wrapping the manager's UpdateReceived event — no Rx dependency.
/// </summary>
public sealed class HttpTransferUpdateObservable(IHttpTransferManager manager) : IObservable<HttpTransferResult>
{
    public IDisposable Subscribe(IObserver<HttpTransferResult> observer)
    {
        EventHandler<HttpTransferResult> handler = (_, r) => observer.OnNext(r);
        manager.UpdateReceived += handler;
        return new Unsubscriber(() => manager.UpdateReceived -= handler);
    }

    sealed class Unsubscriber(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}


/// <summary>
/// BCL IObservable that monitors a single transfer and completes/errors when it finishes.
/// </summary>
public sealed class WatchTransferObservable(IHttpTransferManager manager, string identifier) : IObservable<HttpTransferResult>
{
    public IDisposable Subscribe(IObserver<HttpTransferResult> observer)
    {
        EventHandler<HttpTransferResult> handler = null!;
        handler = (_, result) =>
        {
            if (!result.Request.Identifier.Equals(identifier, StringComparison.InvariantCultureIgnoreCase))
                return;

            if (result.Exception != null)
            {
                manager.UpdateReceived -= handler;
                observer.OnError(result.Exception);
            }
            else if (result.Status == HttpTransferState.Completed)
            {
                manager.UpdateReceived -= handler;
                observer.OnNext(result);
                observer.OnCompleted();
            }
            else
            {
                observer.OnNext(result);
            }
        };
        manager.UpdateReceived += handler;
        return new Unsubscriber(() => manager.UpdateReceived -= handler);
    }

    sealed class Unsubscriber(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
