using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Bluetooth;
using Java.Lang.Annotation;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


public partial class Peripheral : BluetoothGattCallback, IPeripheral
{
    readonly AndroidPlatform platform;
    readonly BleManager manager;
    readonly ILogger logger;

    public Peripheral(
        BleManager manager,
        AndroidPlatform platform,
        BluetoothDevice native,
        ILogger<IPeripheral> logger
    )
    {
        this.manager = manager;
        this.platform = platform;
        this.Native = native;
        this.logger = logger;
    }


    public BluetoothDevice Native { get; }
    public BluetoothGatt? Gatt { get; private set; }

    public string? Name => this.Native.Name;

    string? uuid;
    public string Uuid => this.uuid ??= GetUuid(this.Native);


    public ConnectionState Status
    {
        get
        {
            if (this.Gatt == null)
                return ConnectionState.Disconnected;

            return this.manager
                .Native
                .GetConnectionState(this.Native, ProfileType.Gatt)
                .ToStatus();
        }
    }


    // TODO: after disconnecting GATT, refresh services must be called
    public void CancelConnection()
    {
        try
        {
            this.Gatt?.Close();
            this.Gatt = null;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "BLE Peripheral did not cleanly disconnect");
        }
        this.connSubj.OnNext(ConnectionState.Disconnected);
    }


    public void Connect(ConnectionConfig? config)
    {
        AndroidConnectionConfig cfg = null!;
        if (config == null)
            cfg = new();
        else if (config is AndroidConnectionConfig cfg1)
            cfg = cfg1;
        else
            cfg = new AndroidConnectionConfig(cfg.AutoConnect);

        this.Gatt = this.Native.ConnectGatt(
            this.platform.AppContext,
            config?.AutoConnect ?? true,
            this,
            BluetoothTransports.Le
        );
        if (this.Gatt == null)
            throw new BleException("GATT connection could not be established");

        this.Gatt.RequestConnectionPriority(cfg.ConnectionPriority);
        this.connSubj.OnNext(ConnectionState.Connecting);
    }


    public IObservable<int> ReadRssi() => Observable.Create<int>(ob =>
    {
        //        this.AssertConnection();

        var sub = this.rssiSubj.Subscribe(x =>
        {
            if (x.Status == GattStatus.Success)
                ob.OnNext(x.Rssi);
            else
                throw new BleException("Failed to get RSSI - " + x.Status);
        });
        this.Gatt!.ReadRemoteRssi();

        return sub;
    });
   

    readonly Subject<ConnectionState> connSubj = new();
    public IObservable<ConnectionState> WhenStatusChanged() => this.connSubj;


    readonly Subject<(GattStatus Status, int Rssi)> rssiSubj = new();
    public override void OnReadRemoteRssi(BluetoothGatt? gatt, int rssi, GattStatus status)
    {
        if (status == GattStatus.Success)
            this.rssiSubj.OnNext((status, rssi));
        else
            this.rssiSubj.OnNext((status, rssi));
    }


    public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
    {
        // on disconnect I should kill services
        this.connSubj.OnNext(newState.ToStatus());
    }


    static string GetUuid(BluetoothDevice device)
    {
        var deviceGuid = new byte[16];
        var mac = device.Address!.Replace(":", "");
        var macBytes = Enumerable
            .Range(0, mac.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(mac.Substring(x, 2), 16))
            .ToArray();

        macBytes.CopyTo(deviceGuid, 10);
        return new Guid(deviceGuid).ToString();
    }
}


//    public override IObservable<BleException> WhenConnectionFailed() => this.Context.ConnectionFailed;

//    public override IObservable<string> WhenNameUpdated() => this.Context
//        .ManagerContext
//        .ListenForMe(BluetoothDevice.ActionNameChanged, this)
//        .Select(_ => this.Name);


//readonly SemaphoreSlim semaphore = new(1, 1);

//public IObservable<T> Invoke<T>(IObservable<T> observable)
//{
//    if (!this.ManagerContext.Configuration.UseInternalSyncQueue)
//        return observable;

//    return Observable.FromAsync(async ct =>
//    {
//        await this.semaphore
//            .WaitAsync(ct)
//            .ConfigureAwait(false);

//        try
//        {
//            return await observable
//                .ToTask(ct)
//                .ConfigureAwait(false);
//        }
//        finally
//        {
//            this.semaphore.Release();
//        }
//    });
//}


//readonly Handler handler = new(Looper.MainLooper!);
//public void InvokeOnMainThread(Action action)
//{
//    if (this.ManagerContext.Configuration.InvokeCallsOnMainThread)
//        this.handler.Post(action);
//    else
//        action();
//}
