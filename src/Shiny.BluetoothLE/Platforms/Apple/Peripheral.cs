using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        this.manager.Manager.CancelPeripheralConnection(this.Native);
    }


    public void Connect(ConnectionConfig? config = null)
    {
        var arc = config?.AutoConnect ?? true;
        if (arc)
        {
            this.autoReconnectSub = this
                .WhenDisconnected()
                .Skip(1)
                .Subscribe(_ => this.DoConnect());
        }
        this.DoConnect();
    }


    public IObservable<int> ReadRssi() => Observable.Create<int>(ob =>
    {
        var sub = this.rssiSubj.Subscribe(x =>
        {
            if (x.Exception == null)
                ob.OnNext(x.Rssi);
            else
                ob.OnError(x.Exception);
        });
        this.Native.ReadRSSI();

        return sub;
    });


    readonly Subject<(int Rssi, InvalidOperationException? Exception)> rssiSubj = new();
    public override void RssiRead(CBPeripheral peripheral, NSNumber rssi, NSError? error)
    {
        if (error == null)
            this.rssiSubj.OnNext((rssi.Int32Value, null));
        else
            this.rssiSubj.OnNext((0, new InvalidOperationException(error.LocalizedDescription)));
    }


    public IObservable<ConnectionState> WhenStatusChanged() => Observable.Create<ConnectionState>(ob =>
    {
        ob.OnNext(this.Status);
        var sub = this.connSubj.Subscribe(ob.OnNext);

        return () => sub.Dispose();
    });


    readonly Subject<ConnectionState> connSubj = new();
    internal void ReceiveStateChange(ConnectionState connStatus)
        => this.connSubj.OnNext(connStatus);


    readonly Subject<BleException> connFailedSubj = new();
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