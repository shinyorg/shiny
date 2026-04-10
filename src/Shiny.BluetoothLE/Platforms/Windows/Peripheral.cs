using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Shiny.BluetoothLE;


public partial class Peripheral : IPeripheral
{
    readonly BleManager manager;
    readonly ILogger logger;
    readonly Subject<ConnectionState> connSubj = new();
    readonly Subject<BleException> connFailedSubj = new();
    readonly Subject<Unit> servicesChangedSubj = new();
    readonly HashSet<GattDeviceService> trackedServices = new();
    readonly object connectSync = new();
    bool connectInProgress;
    bool pendingDisconnectedCleanup;


    public Peripheral(BleManager manager, BluetoothLEDevice device, ILogger<IPeripheral> logger)
    {
        this.manager = manager;
        this.logger = logger;
        this.Native = device;
        this.BluetoothAddress = device.BluetoothAddress;
        this.Uuid = device.GetDeviceId().ToString();

        this.HookNativeEvents();
    }


    public BluetoothLEDevice? Native { get; private set; }
    public ulong BluetoothAddress { get; }
    public DeviceInformation DeviceInfo => this.Native!.DeviceInformation;
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
            this.StartConnectAttempt();
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
                _ => this.FinishConnectAttempt(),
                ex =>
                {
                    this.logger.LogWarning(ex, "Failed to connect to peripheral");
                    this.connFailedSubj.OnNext(new BleException(ex.Message, ex));
                    this.connSubj.OnNext(ConnectionState.Disconnected);
                    this.FinishConnectAttempt();
                }
            );
        }
        catch (Exception ex)
        {
            this.connFailedSubj.OnNext(new BleException(ex.Message, ex));
            this.connSubj.OnNext(ConnectionState.Disconnected);
            this.FinishConnectAttempt();
        }
    }


    public void CancelConnection()
    {
        if (this.Native == null)
            return;

        this.connSubj.OnNext(ConnectionState.Disconnecting);
        this.ReleaseNativeResources();

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
        {
            if (this.ShouldDelayDisconnectedCleanup())
            {
            }
            else
            {
                this.CleanupDisconnectedPeripheral();
            }
        }

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


    protected GattDeviceService TrackService(GattDeviceService service)
    {
        lock (this.trackedServices)
            this.trackedServices.Add(service);

        return service;
    }


    void ReleaseNativeResources(bool disposeNative = true)
    {
        this.ClearNotifications();

        List<GattDeviceService> services;
        lock (this.trackedServices)
        {
            services = this.trackedServices.ToList();
            this.trackedServices.Clear();
        }

        foreach (var service in services)
        {
            try
            {
                service.Session?.Dispose();
                service.Dispose();
            }
            catch
            {
                // best effort cleanup
            }
        }

        if (disposeNative && this.Native != null)
        {
            this.Native.ConnectionStatusChanged -= this.OnConnectionStatusChanged;
            this.Native.GattServicesChanged -= this.OnGattServicesChanged;
            this.Native.Dispose();
            this.Native = null;
        }
    }


    void StartConnectAttempt()
    {
        lock (this.connectSync)
        {
            this.connectInProgress = true;
            this.pendingDisconnectedCleanup = false;
        }
    }


    void FinishConnectAttempt()
    {
        var shouldCleanup = false;

        lock (this.connectSync)
        {
            shouldCleanup = this.pendingDisconnectedCleanup;
            this.connectInProgress = false;
            this.pendingDisconnectedCleanup = false;
        }

        if (shouldCleanup)
        {
            this.CleanupDisconnectedPeripheral();
        }
    }


    bool ShouldDelayDisconnectedCleanup()
    {
        lock (this.connectSync)
        {
            if (!this.connectInProgress)
                return false;

            this.pendingDisconnectedCleanup = true;
            return true;
        }
    }


    void CleanupDisconnectedPeripheral()
    {
        this.ReleaseNativeResources();
        this.manager.RemovePeripheral(this);
    }
}
