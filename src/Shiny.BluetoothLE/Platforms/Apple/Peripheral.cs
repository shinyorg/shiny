using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
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
        this.manager.Manager.CancelPeripheralConnection(this.Native);
    }


    public void Connect(ConnectionConfig? config = null)
    {
        var arc = config?.AutoConnect ?? true;
        if (arc)
        {
            this.autoReconnectSub?.Dispose();

            var composite = new CompositeDisposable();

            // Re-issue connect on disconnect. Skip(1) drops the seeded Disconnected
            // value emitted by the BehaviorSubject on subscribe.
            composite.Add(this
                .WhenDisconnected()
                .Skip(1)
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
        var task = this.rssiSubj.Take(1).ToTask(ct);
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
        if (connStatus == ConnectionState.Disconnected)
            this.ClearNotifiers();

        if (this.connSubj.Value != connStatus)
            this.connSubj.OnNext(connStatus);
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


    protected void DoConnect() => this.manager
        .Manager
        .ConnectPeripheral(this.Native, new PeripheralConnectionOptions
        {
            NotifyOnDisconnection = true,
            NotifyOnConnection = true,
            NotifyOnNotification = true
        });


    protected static BleOperationException ToException(NSError error, string message = "") =>
#if XAMARIN
        new (message + error.LocalizedDescription, (int)error.Code);
#else
        new(message + error.LocalizedDescription, error.Code.ToInt32());
#endif
}