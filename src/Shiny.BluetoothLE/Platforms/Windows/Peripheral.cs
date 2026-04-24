using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Shiny.BluetoothLE;


public partial class Peripheral : IPeripheral
{
    static readonly TimeSpan ConnectAttemptTimeout = TimeSpan.FromSeconds(8);
    static readonly TimeSpan SlowConnectWarningThreshold = TimeSpan.FromSeconds(2);
    static readonly TimeSpan DisconnectedCleanupDelay = TimeSpan.FromMilliseconds(1500);
    readonly BleManager manager;
    readonly ILogger logger;
    readonly Subject<ConnectionState> connSubj = new();
    readonly Subject<BleException> connFailedSubj = new();
    readonly Subject<Unit> servicesChangedSubj = new();
    readonly HashSet<GattDeviceService> trackedServices = new();
    readonly object connectSync = new();
    CancellationTokenSource? connectAttemptCts;
    CancellationTokenSource? delayedCleanupCts;
    bool connectInProgress;
    bool pendingDisconnectedCleanup;
    int connectAttemptId;


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
    internal bool CanReuse => this.Native != null;

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

            try
            {
                return this.Native.ConnectionStatus switch
                {
                    BluetoothConnectionStatus.Connected => ConnectionState.Connected,
                    _ => ConnectionState.Disconnected
                };
            }
            catch (ObjectDisposedException)
            {
                return ConnectionState.Disconnected;
            }
            catch (InvalidOperationException)
            {
                return ConnectionState.Disconnected;
            }
        }
    }


    public void Connect(ConnectionConfig? config = null)
    {
        if (this.IsNativeConnected())
            return;

        try
        {
            if (!this.StartConnectAttempt(out var connectToken))
            {
                this.logger.LogDebug("Connect already in progress for peripheral {Uuid}", this.Uuid);
                return;
            }

            var attemptId = Interlocked.Increment(ref this.connectAttemptId);
            this.connSubj.OnNext(ConnectionState.Connecting);
            var stopwatch = Stopwatch.StartNew();
            this.logger.LogDebug(
                "WIN-CONNECT-BEGIN: peripheral={PeripheralId}, attempt={AttemptId}, nativePresent={NativePresent}, nativeConnected={NativeConnected}, pendingCleanup={PendingCleanup}",
                this.DescribeIdentity(),
                attemptId,
                this.Native != null,
                this.IsNativeConnected(),
                this.pendingDisconnectedCleanup
            );

            // Windows BLE connections are implicit - they occur when you access GATT services
            // Calling GetGattServicesAsync forces the connection
            Observable.FromAsync(async ct =>
            {
                if (this.Native == null)
                    throw new BleException("Device is disposed");

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, connectToken);
                var result = await this.Native
                    .GetGattServicesAsync(BluetoothCacheMode.Uncached)
                    .AsTask(linkedCts.Token)
                    .ConfigureAwait(false);

                if (result.Status != Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success)
                    throw new BleException($"Failed to connect: {result.Status}");

                return result;
            })
            .Subscribe(
                _ =>
                {
                    stopwatch.Stop();

                    if (!this.IsNativeConnected())
                    {
                        this.logger.LogDebug(
                            "WIN-CONNECT-LATE-SUCCESS-BLOCKED: peripheral={PeripheralId}, attempt={AttemptId}, pendingCleanup={PendingCleanup}, nativeConnected={NativeConnected}",
                            this.DescribeIdentity(),
                            attemptId,
                            this.pendingDisconnectedCleanup,
                            this.IsNativeConnected()
                        );
                        this.FinishConnectAttempt();
                        this.HandleConnectFailure(new BleException("Connect completed after peripheral was disconnected"), stopwatch.Elapsed);
                        return;
                    }

                    this.LogConnectSuccess(stopwatch.Elapsed);
                    this.logger.LogDebug(
                        "WIN-CONNECT-SUCCESS: peripheral={PeripheralId}, attempt={AttemptId}, elapsedMs={ElapsedMs}",
                        this.DescribeIdentity(),
                        attemptId,
                        stopwatch.Elapsed.TotalMilliseconds
                    );
                    this.CancelDelayedCleanup();
                    this.connSubj.OnNext(ConnectionState.Connected);
                    this.manager.FirePeripheralStateChanged(this);
                    this.FinishConnectAttempt();
                },
                ex =>
                {
                    stopwatch.Stop();
                    this.logger.LogDebug(
                        "WIN-CONNECT-ERROR: peripheral={PeripheralId}, attempt={AttemptId}, elapsedMs={ElapsedMs}, errorType={ErrorType}, error={Error}",
                        this.DescribeIdentity(),
                        attemptId,
                        stopwatch.Elapsed.TotalMilliseconds,
                        ex.GetType().FullName,
                        ex.Message
                    );
                    this.HandleConnectFailure(ex, stopwatch.Elapsed);
                    this.FinishConnectAttempt();
                }
            );
        }
        catch (Exception ex)
        {
            this.HandleConnectFailure(ex, TimeSpan.Zero);
            this.FinishConnectAttempt();
        }
    }


    public void CancelConnection()
    {
        if (this.Native == null)
            return;

        this.connSubj.OnNext(ConnectionState.Disconnecting);
        this.CancelDelayedCleanup();
        this.CancelConnectAttempt();
        this.ReleaseNativeResources();
        this.FinishConnectAttempt();

        this.connSubj.OnNext(ConnectionState.Disconnected);
        this.manager.FirePeripheralStateChanged(this);
        this.manager.RemovePeripheral(this);
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
        var cleanupNow = false;

        this.logger.LogDebug("Peripheral {Uuid} connection status changed to {State}", this.Uuid, state);

        if (state == ConnectionState.Disconnected)
        {
            if (this.ShouldDelayDisconnectedCleanup())
            {
            }
            else
            {
                cleanupNow = true;
            }
        }

        this.connSubj.OnNext(state);
        this.manager.FirePeripheralStateChanged(this);

        if (cleanupNow)
            this.CleanupDisconnectedPeripheral();
    }


    void OnGattServicesChanged(BluetoothLEDevice sender, object args)
    {
        this.logger.LogDebug("Peripheral {Uuid} GATT services changed", this.Uuid);
        this.servicesChangedSubj.OnNext(Unit.Default);
    }


    protected void AssertConnection()
    {
        if (this.Native == null)
            throw new InvalidOperationException("Device is disposed");
    }


    protected GattDeviceService TrackService(GattDeviceService service)
    {
        lock (this.trackedServices)
            this.trackedServices.Add(service);

        return service;
    }


    void ReleaseNativeResources(bool disposeNative = true)
    {
        this.logger.LogDebug(
            "WIN-RELEASE-RESOURCES: peripheral={PeripheralId}, disposeNative={DisposeNative}, trackedServices={TrackedServices}, nativePresent={NativePresent}",
            this.DescribeIdentity(),
            disposeNative,
            this.trackedServices.Count,
            this.Native != null
        );
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
            this.logger.LogDebug("WIN-NATIVE-DISPOSE: peripheral={PeripheralId}", this.DescribeIdentity());
            this.Native.Dispose();
            this.Native = null;
        }
    }


    void StartConnectAttempt()
    {
        this.StartConnectAttempt(out _);
    }


    bool StartConnectAttempt(out CancellationToken connectToken)
    {
        lock (this.connectSync)
        {
            if (this.connectInProgress)
            {
                connectToken = CancellationToken.None;
                return false;
            }

            this.connectInProgress = true;
            this.pendingDisconnectedCleanup = false;
            this.connectAttemptCts?.Dispose();
            this.connectAttemptCts = new CancellationTokenSource(ConnectAttemptTimeout);
            connectToken = this.connectAttemptCts.Token;
            this.logger.LogDebug("WIN-CONNECT-STATE: peripheral={PeripheralId}, event=start_attempt", this.DescribeIdentity());
            return true;
        }
    }


    void FinishConnectAttempt()
    {
        var shouldCleanup = false;
        CancellationTokenSource? connectAttemptCts = null;

        lock (this.connectSync)
        {
            shouldCleanup = this.pendingDisconnectedCleanup;
            this.connectInProgress = false;
            this.pendingDisconnectedCleanup = false;
            connectAttemptCts = this.connectAttemptCts;
            this.connectAttemptCts = null;
        }

        this.logger.LogDebug(
            "WIN-CONNECT-STATE: peripheral={PeripheralId}, event=finish_attempt, shouldCleanup={ShouldCleanup}",
            this.DescribeIdentity(),
            shouldCleanup
        );

        connectAttemptCts?.Dispose();

        if (shouldCleanup)
            this.CleanupDisconnectedPeripheral();
    }


    bool ShouldDelayDisconnectedCleanup()
    {
        lock (this.connectSync)
        {
            if (!this.connectInProgress)
                return false;

            this.pendingDisconnectedCleanup = true;
            this.logger.LogDebug("WIN-DISCONNECT-DELAY-CLEANUP: peripheral={PeripheralId}", this.DescribeIdentity());
            return true;
        }
    }


    void CancelConnectAttempt()
    {
        CancellationTokenSource? connectAttemptCts = null;

        lock (this.connectSync)
        {
            connectAttemptCts = this.connectAttemptCts;
        }

        try
        {
            connectAttemptCts?.Cancel();
        }
        catch
        {
            // best effort cancellation
        }
    }


    void LogConnectSuccess(TimeSpan elapsed)
    {
        if (elapsed >= SlowConnectWarningThreshold)
        {
            this.logger.LogWarning(
                "Peripheral {Uuid} connect completed slowly in {ElapsedMs}ms",
                this.Uuid,
                elapsed.TotalMilliseconds
            );
        }
        else
        {
            this.logger.LogDebug(
                "Peripheral {Uuid} connect completed in {ElapsedMs}ms",
                this.Uuid,
                elapsed.TotalMilliseconds
            );
        }
    }


    void HandleConnectFailure(Exception ex, TimeSpan elapsed)
    {
        var elapsedMs = elapsed == TimeSpan.Zero ? ConnectAttemptTimeout.TotalMilliseconds : elapsed.TotalMilliseconds;
        var connectException = ex as BleException ?? new BleException(ex.Message, ex);
        var shouldCleanup =
            this.Native == null ||
            this.Status != ConnectionState.Connected ||
            ex is OperationCanceledException ||
            ex is TimeoutException ||
            ex.Message.Contains("disposed", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("failed to connect", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("unreachable", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("disconnected", StringComparison.OrdinalIgnoreCase);

        this.logger.LogWarning(
            ex,
            "Failed to connect to peripheral {Uuid} after {ElapsedMs}ms",
            this.Uuid,
            elapsedMs
        );

        this.connFailedSubj.OnNext(connectException);
        this.connSubj.OnNext(ConnectionState.Disconnected);

        if (shouldCleanup)
        {
            this.ScheduleDisconnectedCleanup("connect_failure");
        }
    }


    void CleanupDisconnectedPeripheral()
    {
        this.CancelDelayedCleanup();
        this.logger.LogDebug("WIN-CLEANUP-DISCONNECTED: peripheral={PeripheralId}", this.DescribeIdentity());
        this.ReleaseNativeResources();
        this.manager.RemovePeripheral(this);
    }



    internal void DisposeForManagerRemoval()
    {
        this.CancelDelayedCleanup();
        this.CancelConnectAttempt();
        this.ReleaseNativeResources();
        this.FinishConnectAttempt();
    }

    void ScheduleDisconnectedCleanup(string reason)
    {
        if (this.Native == null || !this.connectInProgress)
        {
            this.CleanupDisconnectedPeripheral();
            return;
        }

        this.CancelDelayedCleanup();

        var cts = new CancellationTokenSource();
        this.delayedCleanupCts = cts;
        this.logger.LogDebug(
            "WIN-SCHEDULE-CLEANUP: peripheral={PeripheralId}, reason={Reason}, delayMs={DelayMs}",
            this.DescribeIdentity(),
            reason,
            DisconnectedCleanupDelay.TotalMilliseconds
        );

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DisconnectedCleanupDelay, cts.Token).ConfigureAwait(false);
                if (!cts.IsCancellationRequested)
                    this.CleanupDisconnectedPeripheral();
            }
            catch (OperationCanceledException)
            {
            }
        }, cts.Token);
    }


    void CancelDelayedCleanup()
    {
        var cts = this.delayedCleanupCts;
        if (cts == null)
            return;

        this.delayedCleanupCts = null;
        try
        {
            cts.Cancel();
        }
        catch
        {
        }
        finally
        {
            cts.Dispose();
        }
    }


    string DescribeIdentity()
        => $"{this.Uuid}|{this.Name ?? "<no-name>"}|#{this.GetHashCode()}";


    bool IsNativeConnected()
    {
        if (this.Native == null)
            return false;

        try
        {
            return this.Native.ConnectionStatus == BluetoothConnectionStatus.Connected;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
