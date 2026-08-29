using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral : BluetoothGattCallback, IPeripheral
{
    readonly AndroidPlatform platform;
    readonly BleManager manager;
    readonly IOperationQueue operations;
    readonly ILogger logger;


    public Peripheral(
        BleManager manager,
        AndroidPlatform platform,
        BluetoothDevice native,
        IOperationQueue operations,
        ILogger<IPeripheral> logger
    )
    {
        this.manager = manager;
        this.platform = platform;
        this.Native = native;
        this.operations = operations;
        this.logger = logger;
    }

    protected static BleOperationException ToException(string message, GattStatus status) =>
        new (message, (int)status);

    public BluetoothDevice Native { get; }
    public BluetoothGatt? Gatt { get; private set; }

    public string? Name => this.Native.Name;

    string? uuid;
    public string Uuid => this.uuid ??= GetUuid(this.Native);

    public ConnectionState Status
    {
        get
        {
            var status = ConnectionState.Disconnected;
            if (this.Gatt != null)
            {
                status = this.manager
                    .Native
                    .GetConnectionState(this.Native, ProfileType.Gatt)
                    .ToStatus();
            }
            return status;
        }
    }


    public void CancelConnection()
    {
        var gatt = this.Gatt;
        if (gatt == null)
            return;

        try
        {
            this.RequiresServiceDiscovery = true;
            this.Gatt = null;
            gatt.Disconnect();
            gatt.Close();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "BLE Peripheral did not cleanly disconnect");
        }
        // Gatt.Close() prevents the framework from firing OnConnectionStateChange, so
        // break in-flight operations and emit Disconnected here (deduped by EmitConnectionState).
        this.BreakOperations();
        this.EmitConnectionState(ConnectionState.Disconnected);
    }


    public void Connect(ConnectionConfig? config)
    {
        try
        {
            AndroidConnectionConfig cfg = null!;
            if (config == null)
                cfg = new();
            else if (config is AndroidConnectionConfig cfg1)
                cfg = cfg1;
            else
                cfg = new AndroidConnectionConfig(config.AutoConnect);

            // Close any prior GATT client before opening a new one — otherwise
            // each retry leaks a client and Android's 7-client limit kicks in,
            // producing status 133 on subsequent connects.
            this.CloseExistingGatt();

            this.Gatt = this.Native.ConnectGatt(
                this.platform.AppContext,
                cfg.AutoConnect,
                this,
                BluetoothTransports.Le
            );
            if (this.Gatt == null)
                throw new BleException("GATT connection could not be established");

            this.Gatt.RequestConnectionPriority(cfg.ConnectionPriority);

            this.EmitConnectionState(ConnectionState.Connecting);
        }
        catch (BleException ex)
        {
            this.connFailSubj.OnNext(ex);
            this.logger.LogWarning(ex, "Failed to connect");
        }
        catch (Exception ex)
        {
            this.connFailSubj.OnNext(new("Failed to connect", ex));
            this.logger.LogWarning(ex, "Failed to connect");
        }
    }


    void CloseExistingGatt()
    {
        var prior = this.Gatt;
        if (prior == null)
            return;

        this.Gatt = null;
        try
        {
            prior.Disconnect();
            prior.Close();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error closing prior GATT client before reconnect");
        }
    }


    readonly ReplaySubject<BleException> connFailSubj = new(1, TimeSpan.FromSeconds(5));
    public IObservable<BleException> WhenConnectionFailed() => this.connFailSubj;

    public IObservable<int> ReadRssi() => this.operations.QueueToObservable(async ct =>
    {
        this.AssertConnection();

        this.rssiSubj ??= new();
        var task = this.WaitForOperation(this.rssiSubj, ct);
        this.Gatt!.ReadRemoteRssi();

        var result = await task.ConfigureAwait(false);
        if (result.Status != GattStatus.Success)
            throw new InvalidOperationException("Failed to retrieve RSSI: " + result.Status);

        return result.Rssi;
    });
   

    readonly BehaviorSubject<ConnectionState> connSubj = new(ConnectionState.Disconnected);
    public IObservable<ConnectionState> WhenStatusChanged() => this.connSubj.DistinctUntilChanged();


    void EmitConnectionState(ConnectionState state)
    {
        if (this.connSubj.Value != state)
            this.connSubj.OnNext(state);
    }


    // GATT callbacks are the only thing that completes a queued operation. Once the link
    // drops the GATT client is closed and those callbacks never fire again, so an in-flight
    // operation would hold the operation queue lock forever (issue #1637). Cancelling this
    // on disconnect gives every in-lock wait a terminal signal so the queue's finally runs.
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
    /// Awaits the first value from a GATT callback stream, aborting if the peripheral
    /// disconnects while the operation queue lock is held.
    /// </summary>
    /// <remarks>
    /// The returned task subscribes to <paramref name="observable"/> synchronously (before this
    /// method yields), so callers can safely issue the GATT call after this returns and before
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


    Subject<(GattStatus Status, int Rssi)>? rssiSubj;
    public override void OnReadRemoteRssi(BluetoothGatt? gatt, int rssi, GattStatus status)
        => this.rssiSubj?.OnNext((status, rssi));


    public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
    {
        // the BleDelegate is fired by the BleManager.Start under ShinyBleBroadcastReceiver
        this.logger.ConnectionStateChange(status, newState);

        if (newState == ProfileState.Disconnected)
        {
            this.RequiresServiceDiscovery = true;
            this.ClearNotifications();
            this.ClearNotifiers();
            this.BreakOperations();

            try
            {
                gatt?.Close();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Error closing GATT on disconnect");
            }
            this.Gatt = null;
        }

        // Push subscriber notifications off the GATT callback thread. The Binder
        // callback thread is single-threaded; subscribers that await on a queued
        // operation deadlock further callbacks otherwise.
        var nextState = newState.ToStatus();
        Task.Run(() => this.EmitConnectionState(nextState));
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


    // The inverse of GetUuid. GetUuid parks the six MAC bytes in the last six bytes of the GUID,
    // which a GUID renders verbatim as its final group, so the encoding is lossless and an
    // identifier this platform produced round-trips back to an address BluetoothAdapter accepts.
    // Returns null for anything not shaped like one of our identifiers.
    internal static string? GetAddress(string peripheralUuid)
    {
        if (!Guid.TryParse(peripheralUuid, out var guid))
            return null;

        var bytes = guid.ToByteArray();
        for (var i = 0; i < 10; i++)
        {
            if (bytes[i] != 0)
                return null;
        }

        // BluetoothAdapter.CheckBluetoothAddress requires the upper-case AA:BB:CC:DD:EE:FF form.
        return String.Join(':', bytes.Skip(10).Select(x => x.ToString("X2")));
    }


    protected void AssertConnection()
    {
        if (this.Status != ConnectionState.Connected)
            throw new InvalidOperationException("GATT is not connected");
    }
}
