using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace Shiny.BluetoothLE;


public partial class Peripheral : IPeripheral
{
    readonly BleManager manager;
    readonly ILogger logger;
    readonly Subject<ConnectionState> connSubj = new();
    readonly Subject<BleException> connFailedSubj = new();
    readonly Subject<Unit> servicesChangedSubj = new();


    public Peripheral(BleManager manager, BluetoothLEDevice device, ILogger<IPeripheral> logger)
    {
        this.manager = manager;
        this.logger = logger;
        this.Native = device;
        this.Uuid = device.GetDeviceId().ToString();

        this.HookNativeEvents();
    }


    public BluetoothLEDevice? Native { get; private set; }
    public string Uuid { get; }
    public string? Name => this.Native?.Name;

    public int Mtu
    {
        get
        {
            if (this.Native == null || this.Status != ConnectionState.Connected)
                return -1;

            // Windows doesn't expose MTU directly on BluetoothLEDevice
            // Default BLE MTU is 23, but negotiated MTU may be higher
            // The actual MTU is determined per-session when writing characteristics
            return -1;
        }
    }


    public ConnectionState Status
    {
        get
        {
            if (this.Native == null)
                return ConnectionState.Disconnected;

            return this.Native.ConnectionStatus switch
            {
                BluetoothConnectionStatus.Connected => ConnectionState.Connected,
                _ => ConnectionState.Disconnected
            };
        }
    }


    public void Connect(ConnectionConfig? config = null)
    {
        if (this.Native != null && this.Native.ConnectionStatus == BluetoothConnectionStatus.Connected)
            return;

        try
        {
            this.connSubj.OnNext(ConnectionState.Connecting);

            // Windows BLE connections are implicit - they occur when you access GATT services
            // Calling GetGattServicesAsync forces the connection
            Observable.FromAsync(async ct =>
            {
                if (this.Native == null)
                    throw new BleException("Device is disposed");

                var result = await this.Native
                    .GetGattServicesAsync(BluetoothCacheMode.Uncached)
                    .AsTask(ct)
                    .ConfigureAwait(false);

                if (result.Status != Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success)
                    throw new BleException($"Failed to connect: {result.Status}");
            })
            .Subscribe(
                _ => { },
                ex =>
                {
                    this.logger.LogWarning(ex, "Failed to connect to peripheral");
                    this.connFailedSubj.OnNext(new BleException(ex.Message, ex));
                    this.connSubj.OnNext(ConnectionState.Disconnected);
                }
            );
        }
        catch (Exception ex)
        {
            this.connFailedSubj.OnNext(new BleException(ex.Message, ex));
            this.connSubj.OnNext(ConnectionState.Disconnected);
        }
    }


    public void CancelConnection()
    {
        if (this.Native == null)
            return;

        this.connSubj.OnNext(ConnectionState.Disconnecting);

        this.ClearNotifications();

        foreach (var service in this.Native.GattServices)
        {
            service.Session?.Dispose();
            service.Dispose();
        }
        this.Native.Dispose();
        this.Native = null;

        this.connSubj.OnNext(ConnectionState.Disconnected);
        this.manager.FirePeripheralStateChanged(this);
    }


    public IObservable<ConnectionState> WhenStatusChanged() => Observable.Create<ConnectionState>(ob =>
    {
        ob.OnNext(this.Status);
        return this.connSubj.Subscribe(ob.OnNext);
    });


    public IObservable<BleException> WhenConnectionFailed() => this.connFailedSubj;


    public IObservable<Unit> WhenServicesChanged() => this.servicesChangedSubj;


    public IObservable<int> ReadRssi() => Observable.Empty<int>();
    // Windows UWP doesn't provide a way to read RSSI from a connected device
    // RSSI is only available during scanning via the advertisement


    void HookNativeEvents()
    {
        if (this.Native == null)
            return;

        this.Native.ConnectionStatusChanged += this.OnConnectionStatusChanged;
        this.Native.GattServicesChanged += this.OnGattServicesChanged;
    }


    void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        var state = sender.ConnectionStatus == BluetoothConnectionStatus.Connected
            ? ConnectionState.Connected
            : ConnectionState.Disconnected;

        this.logger.LogDebug("Peripheral {Uuid} connection status changed to {State}", this.Uuid, state);

        if (state == ConnectionState.Disconnected)
            this.ClearNotifications();

        this.connSubj.OnNext(state);
        this.manager.FirePeripheralStateChanged(this);
    }


    void OnGattServicesChanged(BluetoothLEDevice sender, object args)
    {
        this.logger.LogDebug("Peripheral {Uuid} GATT services changed", this.Uuid);
        this.servicesChangedSubj.OnNext(Unit.Default);
    }


    protected void AssertConnection()
    {
        if (this.Status != ConnectionState.Connected)
            throw new InvalidOperationException("GATT is not connected");
    }
}
