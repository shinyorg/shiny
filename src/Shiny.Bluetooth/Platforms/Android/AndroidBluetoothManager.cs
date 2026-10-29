using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Android;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Microsoft.Extensions.Logging;
using Shiny.Bluetooth.Infrastructure;
using Java.Util;
using BluetoothDevice = Shiny.Bluetooth.Infrastructure.BluetoothDevice;
using NativeDevice = Android.Bluetooth.BluetoothDevice;

namespace Shiny.Bluetooth;


public class AndroidBluetoothManager(
    AndroidPlatform platform,
    ILogger<AndroidBluetoothManager> logger
) : IBluetoothManager, IBluetoothDeviceOperations
{
    public BluetoothCapabilities Capabilities =>
        BluetoothCapabilities.Discovery |
        BluetoothCapabilities.Pairing |
        BluetoothCapabilities.Connect |
        BluetoothCapabilities.Listen |
        BluetoothCapabilities.Discoverable;


    BluetoothAdapter? NativeAdapter => platform
        .GetSystemService<BluetoothManager>(Context.BluetoothService)
        .Adapter;


    BluetoothAdapter GetAdapter() => this.NativeAdapter
        ?? throw new BluetoothNotSupportedException("This device has no Bluetooth adapter");


    public async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        var adapter = this.NativeAdapter;
        if (adapter == null)
            return AccessState.NotSupported;

        var permissions = GetPermissions();
        if (!permissions.All(platform.IsInManifest))
            return AccessState.NotSetup;

        var result = await platform
            .RequestPermissions(ct, permissions)
            .ConfigureAwait(false);

        if (!result.IsSuccess())
            return AccessState.Denied;

        return adapter.IsEnabled ? AccessState.Available : AccessState.Disabled;
    }


    public Task<IReadOnlyList<IBluetoothDevice>> GetPairedDevices(CancellationToken ct = default)
    {
        IReadOnlyList<IBluetoothDevice> devices = this
            .GetAdapter()
            .BondedDevices?
            .Where(x => x.Type != BluetoothDeviceType.Le)
            .Select(this.ToDevice)
            .ToList() ?? [];

        return Task.FromResult(devices);
    }


    public Task<IBluetoothDevice> GetDevice(string address, CancellationToken ct = default)
    {
        // Android hands out a device for any well-formed address, known to it or not
        var native = this.GetAdapter().GetRemoteDevice(BluetoothAddress.Normalize(address))!;
        return Task.FromResult<IBluetoothDevice>(this.ToDevice(native));
    }


    public async IAsyncEnumerable<BluetoothDiscoveryResult> Discover([EnumeratorCancellation] CancellationToken ct = default)
    {
        var adapter = this.GetAdapter();

        // an inquiry another caller left running would finish under us and end this one at once
        if (adapter.IsDiscovering)
            adapter.CancelDiscovery();

        var found = Channel.CreateUnbounded<BluetoothDiscoveryResult>();
        var started = false;
        var receiver = new ActionBroadcastReceiver
        {
            OnAction = intent =>
            {
                switch (intent.Action)
                {
                    case BluetoothAdapter.ActionDiscoveryStarted:
                        started = true;
                        break;

                    // the finished broadcast for a cancelled earlier inquiry can still be in flight, so only
                    // a finish that follows this inquiry's own start ends it
                    case BluetoothAdapter.ActionDiscoveryFinished when started:
                        found.Writer.TryComplete();
                        break;

                    case NativeDevice.ActionFound:
                        var native = GetDeviceExtra(intent);
                        if (native != null && native.Type != BluetoothDeviceType.Le)
                        {
                            var rssi = intent.GetShortExtra(NativeDevice.ExtraRssi, short.MinValue);
                            found.Writer.TryWrite(new BluetoothDiscoveryResult(this.ToDevice(native), rssi == short.MinValue ? null : rssi));
                        }
                        break;
                }
            }
        };

        var filter = new IntentFilter();
        filter.AddAction(BluetoothAdapter.ActionDiscoveryStarted);
        filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
        filter.AddAction(NativeDevice.ActionFound);
        platform.AppContext.RegisterReceiver(receiver, filter);

        try
        {
            if (!adapter.StartDiscovery())
                throw new BluetoothException("Android refused to start discovery - check that Bluetooth is on and RequestAccess was granted");

            var seen = new HashSet<string>();
            await foreach (var result in found.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                if (seen.Add(result.Device.Address))
                    yield return result;
            }
        }
        finally
        {
            adapter.CancelDiscovery();
            platform.AppContext.UnregisterReceiver(receiver);
        }
    }


    async Task<bool> IBluetoothDeviceOperations.Pair(BluetoothDevice device, CancellationToken ct)
    {
        var native = this.GetAdapter().GetRemoteDevice(device.Address)!;
        if (native.BondState == Bond.Bonded)
            return true;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var registration = ActionBroadcastReceiver.Register(platform, NativeDevice.ActionBondStateChanged, intent =>
        {
            if (GetDeviceExtra(intent)?.Address != native.Address)
                return;

            var state = (Bond)intent.GetIntExtra(NativeDevice.ExtraBondState, (int)Bond.None);
            var previous = (Bond)intent.GetIntExtra(NativeDevice.ExtraPreviousBondState, (int)Bond.None);
            if (state == Bond.Bonded)
                tcs.TrySetResult(true);
            else if (state == Bond.None && previous == Bond.Bonding)
                tcs.TrySetResult(false);
        });

        try
        {
            // already mid-bond (the user started it, or an earlier call did) - just wait it out
            if (native.BondState != Bond.Bonding && !native.CreateBond())
            {
                logger.LogWarning("CreateBond refused for {Address}", device.Address);
                return false;
            }

            using var _ = ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            ActionBroadcastReceiver.UnRegister(platform, registration);
        }
    }


    async Task<BluetoothConnection> IBluetoothDeviceOperations.Connect(BluetoothDevice device, Guid serviceUuid, bool secure, CancellationToken ct)
    {
        var adapter = this.GetAdapter();
        var native = adapter.GetRemoteDevice(device.Address)!;

        // Android's own advice - an inquiry in progress slows the connect to a crawl
        adapter.CancelDiscovery();

        var uuid = UUID.FromString(serviceUuid.ToString())!;
        var socket = secure
            ? native.CreateRfcommSocketToServiceRecord(uuid)!
            : native.CreateInsecureRfcommSocketToServiceRecord(uuid)!;

        try
        {
            // Connect blocks for the SDP lookup and the RFCOMM handshake; closing the socket is the only
            // way to abort it
            using (ct.Register(socket.Close))
                await Task.Run(socket.Connect, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            socket.Close();
            socket.Dispose();
            ct.ThrowIfCancellationRequested();
            throw new BluetoothConnectionException($"Could not connect to {serviceUuid} on {device.Address} - {ex.Message}", ex);
        }

        logger.LogDebug("Connected to {Uuid} on {Address}", serviceUuid, device.Address);
        return new BluetoothConnection(this.ToDevice(native), serviceUuid, Wrap(socket));
    }


    public Task<BluetoothListener> Listen(Guid serviceUuid, string serviceName, bool secure = true, CancellationToken ct = default)
    {
        var adapter = this.GetAdapter();
        var uuid = UUID.FromString(serviceUuid.ToString())!;

        BluetoothServerSocket server;
        try
        {
            server = secure
                ? adapter.ListenUsingRfcommWithServiceRecord(serviceName, uuid)!
                : adapter.ListenUsingInsecureRfcommWithServiceRecord(serviceName, uuid)!;
        }
        catch (Java.IO.IOException ex)
        {
            throw new BluetoothException($"Could not publish {serviceUuid} - {ex.Message}", ex);
        }

        var listener = new BluetoothListener(serviceUuid, serviceName, () =>
        {
            // closing the server socket unblocks the pending Accept, which ends the loop
            server.Close();
            server.Dispose();
            return default;
        });
        _ = Task.Run(() => this.AcceptLoop(server, listener), CancellationToken.None);
        return Task.FromResult(listener);
    }


    void AcceptLoop(BluetoothServerSocket server, BluetoothListener listener)
    {
        while (true)
        {
            BluetoothSocket socket;
            try
            {
                socket = server.Accept()!;
            }
            catch (Exception ex)
            {
                // also how a disposed listener ends - Fault is then a no-op, the channel is already complete
                logger.LogDebug(ex, "RFCOMM accept loop for {Uuid} ended", listener.ServiceUuid);
                listener.Fault(ex);
                return;
            }

            var remote = socket.RemoteDevice!;
            listener.Enqueue(new BluetoothConnection(this.ToDevice(remote), listener.ServiceUuid, Wrap(socket)));
        }
    }


    public async Task<bool> RequestDiscoverable(TimeSpan duration, CancellationToken ct = default)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var access = await platform.RequestAccess(Manifest.Permission.BluetoothAdvertise).ConfigureAwait(false);
            if (access != AccessState.Available)
                throw new BluetoothPermissionException("RequestDiscoverable needs BLUETOOTH_ADVERTISE in the manifest, granted at runtime");
        }

        var intent = new Intent(BluetoothAdapter.ActionRequestDiscoverable);
        intent.PutExtra(BluetoothAdapter.ExtraDiscoverableDuration, (int)duration.TotalSeconds);

        // the result code is the granted duration in seconds, or RESULT_CANCELED when the user declines
        var result = await platform.StartActivityForResult(intent, ct).ConfigureAwait(false);
        return result.ResultCode != Result.Canceled;
    }


    static Stream Wrap(BluetoothSocket socket) => new DuplexStream(
        socket.InputStream!,
        socket.OutputStream!,
        () =>
        {
            socket.Close();
            socket.Dispose();
        }
    );


    BluetoothDevice ToDevice(NativeDevice native) => new(this, native.Address!)
    {
        Name = native.Name,
        Paired = native.BondState == Bond.Bonded,
        ClassOfDevice = native.BluetoothClass is { } cls ? (uint)(int)cls.DeviceClass : null
    };


    static NativeDevice? GetDeviceExtra(Intent intent)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return intent.GetParcelableExtra(NativeDevice.ExtraDevice, Java.Lang.Class.FromType(typeof(NativeDevice))) as NativeDevice;

#pragma warning disable CA1422
        return intent.GetParcelableExtra(NativeDevice.ExtraDevice) as NativeDevice;
#pragma warning restore CA1422
    }


    static string[] GetPermissions()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            return
            [
                Manifest.Permission.BluetoothScan,
                Manifest.Permission.BluetoothConnect
            ];
        }
        return
        [
            Manifest.Permission.Bluetooth,
            Manifest.Permission.BluetoothAdmin,
            Manifest.Permission.AccessFineLocation
        ];
    }
}
