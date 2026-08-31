using Tmds.DBus.Protocol;
using MessageNotification = Tmds.DBus.Protocol.Notification<Tmds.DBus.Protocol.Message>;

namespace Shiny.ScreenRecorder.Portal;


/// <summary>The result of one portal request.</summary>
internal sealed record PortalResponse(uint Code, Dictionary<string, VariantValue> Results);


/// <summary>
/// Listens for <c>org.freedesktop.portal.Request.Response</c> signals and routes each to whoever is
/// waiting on that request path.
/// </summary>
/// <remarks>
/// <para>Every portal call is asynchronous twice over: the method returns an object path
/// immediately, and the real answer arrives later as a signal on that path. Which means the signal
/// can - and on a fast portal does - arrive before the method reply that names the path.</para>
/// <para>The specification's own answer is to predict the request path from the client's unique
/// bus name and subscribe before calling. This does something simpler and less brittle: it watches
/// every Response signal and holds onto ones nobody is waiting for yet, so an early arrival is
/// matched as soon as the path is known. No bus-name mangling, and no race.</para>
/// </remarks>
internal sealed class PortalRequestWatcher : IAsyncDisposable
{
    readonly object gate = new();
    readonly Dictionary<string, TaskCompletionSource<PortalResponse>> waiters = new();
    readonly Dictionary<string, PortalResponse> early = new();

    IDisposable? subscription;


    public async Task Start(DBusConnection connection, CancellationToken ct)
    {
        if (this.subscription != null)
            return;

        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = PortalConstants.Service,
            Interface = PortalConstants.RequestInterface,
            Member = "Response"
        };

        this.subscription = await connection.AddMatchAsync(
            rule,
            static (Message message, object? _) => message,
            static (MessageNotification notification) =>
            {
                if (notification.Exception != null)
                    return;

                ((PortalRequestWatcher)notification.State!).Dispatch(notification.Value);
            },
            emitOnCapturedContext: false,
            ObserverFlags.None,
            this
        ).ConfigureAwait(false);
    }


    void Dispatch(Message message)
    {
        var path = message.PathAsString;
        if (path == null)
            return;

        var reader = message.GetBodyReader();
        var code = reader.ReadUInt32();
        var results = reader.ReadDictionaryOfStringToVariantValue();
        var response = new PortalResponse(code, results);

        lock (this.gate)
        {
            if (this.waiters.Remove(path, out var waiter))
            {
                waiter.TrySetResult(response);
                return;
            }

            // nobody has asked for this path yet - the method reply naming it is still in flight
            this.early[path] = response;
        }
    }


    /// <summary>Waits for the response to one request path.</summary>
    public Task<PortalResponse> Wait(string requestPath, CancellationToken ct)
    {
        lock (this.gate)
        {
            if (this.early.Remove(requestPath, out var already))
                return Task.FromResult(already);

            var tcs = new TaskCompletionSource<PortalResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.waiters[requestPath] = tcs;

            return tcs.Task.WaitAsync(ct);
        }
    }


    public ValueTask DisposeAsync()
    {
        this.subscription?.Dispose();
        this.subscription = null;

        lock (this.gate)
        {
            foreach (var waiter in this.waiters.Values)
                waiter.TrySetCanceled();

            this.waiters.Clear();
            this.early.Clear();
        }

        return ValueTask.CompletedTask;
    }
}
