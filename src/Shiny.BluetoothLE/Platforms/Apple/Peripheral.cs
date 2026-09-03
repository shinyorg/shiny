using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral : CBPeripheralDelegate, IPeripheral
{
    readonly BleManager manager;
    readonly ILogger logger;
    readonly IOperationQueue operations;
    IDisposable? autoReconnectSub;


    public Peripheral(
        BleManager manager,
        CBPeripheral native,
        IOperationQueue operations,
        ILogger<IPeripheral> logger
    )
    {
        this.manager = manager;
        this.Native = native;
        this.operations = operations;
        this.logger = logger;

        this.Uuid = native.Identifier.ToString();
        this.Native.Delegate = this;
    }


    public CBPeripheral Native { get; }

    public string Uuid { get; }
    public string? Name => this.Native.Name;
    public int Mtu => (int)this
        .Native
        .GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse);

    
    public ConnectionState Status => this.Native.State switch
    {
        CBPeripheralState.Connected => ConnectionState.Connected,
        CBPeripheralState.Connecting => ConnectionState.Connecting,
        CBPeripheralState.Disconnected => ConnectionState.Disconnected,
        CBPeripheralState.Disconnecting => ConnectionState.Disconnecting,
        _ => ConnectionState.Disconnected
    };


    public void CancelConnection()
    {
        this.autoReconnectSub?.Dispose();
        this.autoReconnectSub = null;
        this.pendingAdapterConnect = false;
        this.manager.Manager.CancelPeripheralConnection(this.Native);
    }


    public void Connect(ConnectionConfig? config = null)
    {
        // Disposed unconditionally. A Connect that downgrades to AutoConnect: false has to take
        // the previous subscription with it - otherwise the peripheral keeps reconnecting on a
        // config that no longer asks for it, and OnAdapterAvailable (which keys off this being
        // armed) reconnects it across an adapter power cycle too.
        this.autoReconnectSub?.Dispose();
        this.autoReconnectSub = null;

        var arc = config?.AutoConnect ?? true;
        if (arc)
        {
            var composite = new CompositeDisposable();

            // Re-issue connect on disconnect. Skip the value the BehaviorSubject replays on
            // subscribe, and skip it BEFORE filtering - filtering first (which WhenDisconnected
            // does) means a replayed Connected is what gets dropped, and the first real disconnect
            // is swallowed instead, so a Connect() on an already-connected peripheral never
            // re-armed.
            composite.Add(this
                .WhenStatusChanged()
                .Skip(1)
                .Where(x => x == ConnectionState.Disconnected)
                .Subscribe(_ => this.DoReconnect())
            );

            // Cold-start ConnectPeripheral failures (FailedToConnectPeripheral)
            // do not fire a Disconnected status change, so subscribe to retry here
            // as well. Small delay avoids tight retry loops.
            composite.Add(this
                .WhenConnectionFailed()
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    if (this.Status != ConnectionState.Connected && this.Status != ConnectionState.Connecting)
                        this.DoReconnect();
                })
            );

            this.autoReconnectSub = composite;
        }
        this.DoConnect();
    }


    void DoReconnect()
    {
        // A Disconnected can be emitted while the link is already back up - ReceiveStateChange
        // synthesizes one to repair a stale subject, and the adapter teardown emits one for a
        // peripheral CoreBluetooth may reconnect on its own. Cancelling then would tear down a
        // live connection.
        if (this.Status is ConnectionState.Connected or ConnectionState.Connecting)
            return;

        // Cancel any stale pending connection slot before re-issuing — iOS holds
        // the previous pending connection until you explicitly cancel it.
        try
        {
            this.manager.Manager.CancelPeripheralConnection(this.Native);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error cancelling prior connection before reconnect");
        }
        this.DoConnect();
    }


    public IObservable<int> ReadRssi() => this.operations.QueueToObservable(async ct =>
    {
        var task = this.WaitForOperation(this.rssiSubj, ct);
        this.Native.ReadRSSI();

        var result = await task.ConfigureAwait(false);
        if (result.Exception != null)
            throw result.Exception;

        return result.Rssi;
    });


    readonly Subject<(int Rssi, InvalidOperationException? Exception)> rssiSubj = new();
    public override void RssiRead(CBPeripheral peripheral, NSNumber rssi, NSError? error)
    {
        if (error == null)
            this.rssiSubj.OnNext((rssi.Int32Value, null));
        else
            this.rssiSubj.OnNext((0, new InvalidOperationException(error.LocalizedDescription)));
    }


    public IObservable<ConnectionState> WhenStatusChanged() => this.connSubj.DistinctUntilChanged();


    readonly BehaviorSubject<ConnectionState> connSubj = new(ConnectionState.Disconnected);
    internal void ReceiveStateChange(ConnectionState connStatus)
    {
        // CoreBluetooth only calls didConnectPeripheral for a brand new link, so a Connected
        // arriving while the subject still says Connected means the previous link died without a
        // didDisconnectPeripheral. Swallowing it would leave a live peripheral whose status stream
        // never fired and whose managed notifications were never re-hooked (issue #1652), so
        // deliver the transition the platform never gave us instead.
        if (connStatus == ConnectionState.Connected && this.connSubj.Value == ConnectionState.Connected)
            this.ReceiveStateChange(ConnectionState.Disconnected);

        if (connStatus == ConnectionState.Disconnected)
        {
            this.ClearNotifiers();
            this.BreakOperations();
        }

        if (this.connSubj.Value != connStatus)
            this.connSubj.OnNext(connStatus);
    }


    /// <summary>
    /// The adapter went below PoweredOn. CoreBluetooth does not deliver didDisconnectPeripheral for
    /// this, but the link is gone - Status already reads Disconnected off the platform - so run the
    /// teardown a real disconnect would have (issue #1652).
    /// </summary>
    internal void OnAdapterUnavailable()
    {
        if (this.connSubj.Value is not (ConnectionState.Connected or ConnectionState.Connecting))
            return;

        this.logger.LogDebug("Adapter unavailable - disconnecting peripheral {Uuid}", this.Uuid);
        this.ReceiveStateChange(ConnectionState.Disconnected);
    }


    /// <summary>
    /// The adapter is back. Re-issue any connect that was parked while it was down, and re-arm
    /// peripherals that were connected with AutoConnect (issue #1652).
    /// </summary>
    internal void OnAdapterAvailable()
    {
        if (!this.pendingAdapterConnect && this.autoReconnectSub == null)
            return;

        if (this.Status is ConnectionState.Connected or ConnectionState.Connecting)
            return;

        this.logger.LogDebug("Adapter available - reconnecting peripheral {Uuid}", this.Uuid);
        this.DoReconnect();
    }


    // CoreBluetooth delegate callbacks are the only thing that completes a queued operation.
    // A dead peripheral never fires them again, so an in-flight operation would hold the
    // operation queue lock forever (issue #1637). Cancelling this on disconnect gives every
    // in-lock wait a terminal signal so the queue's finally can release the lock.
    CancellationTokenSource disconnectCts = new();

    void BreakOperations()
    {
        var cts = Interlocked.Exchange(ref this.disconnectCts, new());
        try
        {
            cts.Cancel();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error cancelling in-flight operations on disconnect");
        }
        finally
        {
            cts.Dispose();
        }
    }


    /// <summary>
    /// Awaits the first value from a CoreBluetooth delegate stream, aborting if the peripheral
    /// disconnects while the operation queue lock is held.
    /// </summary>
    /// <remarks>
    /// The returned task subscribes to <paramref name="observable"/> synchronously (before this
    /// method yields), so callers can safely issue the native call after this returns and before
    /// awaiting. Do not add an await ahead of the subscription - callbacks that fire immediately
    /// would be missed.
    /// </remarks>
    protected async Task<T> WaitForOperation<T>(IObservable<T> observable, CancellationToken ct, [CallerMemberName] string? caller = null)
    {
        var cts = this.disconnectCts;
        if (this.Status != ConnectionState.Connected || cts.IsCancellationRequested)
            throw new BleException($"[{caller}] Peripheral is not connected");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
        try
        {
            return await observable.Take(1).ToTask(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new BleException($"[{caller}] Peripheral disconnected during operation");
        }
    }


    readonly ReplaySubject<BleException> connFailedSubj = new(1, TimeSpan.FromSeconds(5));
    public IObservable<BleException> WhenConnectionFailed() => this.connFailedSubj;


    internal void ConnectionFailed(NSError? error)
    {
        var ex = new BleException(error?.LocalizedDescription ?? "Connection Failed");
        this.connFailedSubj.OnNext(ex);
    }


    protected void AssertConnnection()
    {
        if (this.Status != ConnectionState.Connected)
            throw new InvalidOperationException("GATT is not connected");
    }


    // Set when a connect was requested while the adapter was down. ConnectPeripheral below
    // PoweredOn is an API-misuse no-op that CoreBluetooth never reports on, so the request is
    // parked here and the manager replays it on power-on (issue #1652).
    bool pendingAdapterConnect;


    /// <summary>
    /// Whether this peripheral is waiting on a reconnect - an armed auto-reconnect, or a connect
    /// parked until the adapter returns. Starting a scan must not evict these (issue #1652): the
    /// manager's adapter hooks only reach peripherals still in its dictionary, and the fresh
    /// wrapper a later lookup mints has neither the subscription nor the parked request.
    /// </summary>
    internal bool IsAwaitingReconnect => this.autoReconnectSub != null || this.pendingAdapterConnect;

    protected void DoConnect()
    {
        if (!this.manager.IsAdapterAvailable)
        {
            this.pendingAdapterConnect = true;
            this.logger.LogDebug("Connect deferred - bluetooth adapter is not available: {Uuid}", this.Uuid);
            return;
        }
        this.pendingAdapterConnect = false;

        this.manager
            .Manager
            .ConnectPeripheral(this.Native, new PeripheralConnectionOptions
            {
                NotifyOnDisconnection = true,
                NotifyOnConnection = true,
                NotifyOnNotification = true
            });
    }


    protected static BleOperationException ToException(NSError error, string message = "") =>
#if XAMARIN
        new (message + error.LocalizedDescription, (int)error.Code);
#else
        new(message + error.LocalizedDescription, error.Code.ToInt32());
#endif
}