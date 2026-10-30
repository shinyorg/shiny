using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Shiny.Bluetooth.Infrastructure;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using BluetoothDevice = Shiny.Bluetooth.Infrastructure.BluetoothDevice;
using NativeDevice = Windows.Devices.Bluetooth.BluetoothDevice;

namespace Shiny.Bluetooth;


public class WindowsBluetoothManager(ILogger<WindowsBluetoothManager> logger) : IBluetoothManager, IBluetoothDeviceOperations
{
    // association endpoints on the BR/EDR protocol - paired or not, which BluetoothDevice's own selectors cannot express in one query
    const string ClassicProtocolSelector = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"";
    const string AddressProperty = "System.Devices.Aep.DeviceAddress";
    const string IsPairedProperty = "System.Devices.Aep.IsPaired";
    const string SignalStrengthProperty = "System.Devices.Aep.SignalStrength";


    public BluetoothCapabilities Capabilities =>
        BluetoothCapabilities.Discovery |
        BluetoothCapabilities.Pairing |
        BluetoothCapabilities.Connect |
        BluetoothCapabilities.Listen;


    public async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        var adapter = await BluetoothAdapter.GetDefaultAsync().AsTask(ct).ConfigureAwait(false);
        if (adapter == null || !adapter.IsClassicSupported)
            return AccessState.NotSupported;

        var access = await Radio.RequestAccessAsync().AsTask(ct).ConfigureAwait(false);
        if (access != RadioAccessStatus.Allowed)
            return AccessState.Denied;

        var radio = await adapter.GetRadioAsync().AsTask(ct).ConfigureAwait(false);
        return radio?.State == RadioState.On ? AccessState.Available : AccessState.Disabled;
    }


    public async Task<IReadOnlyList<IBluetoothDevice>> GetPairedDevices(CancellationToken ct = default)
    {
        var infos = await DeviceInformation
            .FindAllAsync(NativeDevice.GetDeviceSelectorFromPairingState(true))
            .AsTask(ct)
            .ConfigureAwait(false);

        var list = new List<IBluetoothDevice>(infos.Count);
        foreach (var info in infos)
        {
            using var native = await NativeDevice.FromIdAsync(info.Id).AsTask(ct).ConfigureAwait(false);
            if (native != null)
                list.Add(this.ToDevice(native));
        }
        return list;
    }


    public async Task<IBluetoothDevice> GetDevice(string address, CancellationToken ct = default)
    {
        var value = BluetoothAddress.ToUInt64(address);
        using var native = await NativeDevice.FromBluetoothAddressAsync(value).AsTask(ct).ConfigureAwait(false);

        // null for an address Windows has never seen - it can still be connected to
        return native == null
            ? new BluetoothDevice(this, address)
            : this.ToDevice(native);
    }


    public async IAsyncEnumerable<BluetoothDiscoveryResult> Discover([EnumeratorCancellation] CancellationToken ct = default)
    {
        var found = Channel.CreateUnbounded<BluetoothDiscoveryResult>();
        var watcher = DeviceInformation.CreateWatcher(
            ClassicProtocolSelector,
            [AddressProperty, IsPairedProperty, SignalStrengthProperty],
            DeviceInformationKind.AssociationEndpoint
        );
        watcher.Added += (_, info) =>
        {
            var result = this.FromWatcher(info);
            if (result != null)
                found.Writer.TryWrite(result);
        };
        watcher.Stopped += (_, _) => found.Writer.TryComplete();

        // the watcher keeps inquiring until stopped; EnumerationCompleted only marks the end of the first
        // pass, which can be the cached paired list alone - so the inquiry runs for a fixed window instead
        using var window = CancellationTokenSource.CreateLinkedTokenSource(ct);
        window.CancelAfter(IBluetoothManager.InquiryDuration);

        watcher.Start();
        try
        {
            var seen = new HashSet<string>();
            var reader = found.Reader;
            while (await WaitToRead(reader, window.Token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var result))
                {
                    if (seen.Add(result.Device.Address))
                        yield return result;
                }
            }
            ct.ThrowIfCancellationRequested();
        }
        finally
        {
            if (watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
                watcher.Stop();
        }
    }


    // the inquiry window running out is the normal end of a discovery, not a cancellation
    static async Task<bool> WaitToRead(ChannelReader<BluetoothDiscoveryResult> reader, CancellationToken window)
    {
        try
        {
            return await reader.WaitToReadAsync(window).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }


    async Task<bool> IBluetoothDeviceOperations.Pair(BluetoothDevice device, CancellationToken ct)
    {
        using var native = await GetNative(device, ct).ConfigureAwait(false);
        var pairing = native.DeviceInformation.Pairing;
        if (pairing.IsPaired)
            return true;

        if (!pairing.CanPair)
            return false;

        // the basic ceremony - Windows raises its own confirmation / PIN UI for the methods that need one
        var result = await pairing.PairAsync().AsTask(ct).ConfigureAwait(false);
        logger.LogDebug("Pairing {Address} finished with {Status}", device.Address, result.Status);
        return result.Status is DevicePairingResultStatus.Paired or DevicePairingResultStatus.AlreadyPaired;
    }


    async Task<BluetoothConnection> IBluetoothDeviceOperations.Connect(BluetoothDevice device, Guid serviceUuid, bool secure, CancellationToken ct)
    {
        var native = await GetNative(device, ct).ConfigureAwait(false);
        RfcommDeviceService? service = null;
        var socket = new StreamSocket();
        try
        {
            var lookup = await native
                .GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(serviceUuid), BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            if (lookup.Error != BluetoothError.Success)
                throw new BluetoothConnectionException($"SDP lookup of {serviceUuid} on {device.Address} failed - {lookup.Error}");

            service = lookup.Services.FirstOrDefault()
                ?? throw new BluetoothConnectionException($"{device.Address} does not publish {serviceUuid}");

            await socket
                .ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName, GetProtection(secure))
                .AsTask(ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            socket.Dispose();
            service?.Dispose();
            native.Dispose();

            if (ex is BluetoothException or OperationCanceledException)
                throw;
            throw new BluetoothConnectionException($"Could not connect to {serviceUuid} on {device.Address} - {ex.Message}", ex);
        }

        var connected = this.ToDevice(native);
        return new BluetoothConnection(connected, serviceUuid, Wrap(socket, () =>
        {
            service.Dispose();
            native.Dispose();
        }));
    }


    public async Task<BluetoothListener> Listen(Guid serviceUuid, string serviceName, bool secure = true, CancellationToken ct = default)
    {
        RfcommServiceProvider provider;
        try
        {
            provider = await RfcommServiceProvider
                .CreateAsync(RfcommServiceId.FromUuid(serviceUuid))
                .AsTask(ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new BluetoothException($"Could not create the RFCOMM provider for {serviceUuid} - {ex.Message}", ex);
        }

        var socketListener = new StreamSocketListener();
        var listener = new BluetoothListener(serviceUuid, serviceName, () =>
        {
            provider.StopAdvertising();
            socketListener.Dispose();
            return default;
        });

        socketListener.ConnectionReceived += (_, args) =>
        {
            try
            {
                // a Bluetooth peer's host name is its address, "(aa:bb:cc:dd:ee:ff)"
                var device = new BluetoothDevice(this, args.Socket.Information.RemoteHostName.RawName);
                listener.Enqueue(new BluetoothConnection(device, serviceUuid, Wrap(args.Socket, null)));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Dropped an incoming RFCOMM connection on {Uuid}", serviceUuid);
                args.Socket.Dispose();
            }
        };

        try
        {
            await socketListener
                .BindServiceNameAsync(provider.ServiceId.AsString(), GetProtection(secure))
                .AsTask(ct)
                .ConfigureAwait(false);

            var writer = new DataWriter();
            writer.WriteBytes(SdpEncoding.Text(serviceName));
            provider.SdpRawAttributes.Add(SdpEncoding.ServiceNameAttributeId, writer.DetachBuffer());

            // radioDiscoverable: true makes the service visible to unpaired devices while the provider is up
            provider.StartAdvertising(socketListener, true);
        }
        catch (Exception ex)
        {
            await listener.DisposeAsync().ConfigureAwait(false);
            if (ex is OperationCanceledException)
                throw;
            throw new BluetoothException($"Could not publish {serviceUuid} - {ex.Message}", ex);
        }

        return listener;
    }


    public Task<bool> RequestDiscoverable(TimeSpan duration, CancellationToken ct = default)
        => throw BluetoothNotSupportedException.For(
            BluetoothCapabilities.Discoverable,
            "Windows makes a PC discoverable only while its Bluetooth settings page is open"
        );


    static async Task<NativeDevice> GetNative(BluetoothDevice device, CancellationToken ct)
    {
        var native = await NativeDevice
            .FromBluetoothAddressAsync(BluetoothAddress.ToUInt64(device.Address))
            .AsTask(ct)
            .ConfigureAwait(false);

        return native ?? throw new BluetoothConnectionException($"Windows does not know a device at {device.Address}");
    }


    static SocketProtectionLevel GetProtection(bool secure) => secure
        ? SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication
        : SocketProtectionLevel.PlainSocket;


    static Stream Wrap(StreamSocket socket, Action? onClose) => new DuplexStream(
        socket.InputStream.AsStreamForRead(0),
        socket.OutputStream.AsStreamForWrite(0),
        () =>
        {
            socket.Dispose();
            onClose?.Invoke();
        }
    );


    BluetoothDevice ToDevice(NativeDevice native)
        => new(this, BluetoothAddress.FromUInt64(native.BluetoothAddress))
        {
            Name = native.Name,
            Paired = native.DeviceInformation.Pairing.IsPaired,
            ClassOfDevice = native.ClassOfDevice?.RawValue
        };


    BluetoothDiscoveryResult? FromWatcher(DeviceInformation info)
    {
        if (info.Properties.TryGetValue(AddressProperty, out var address) && address is string value)
        {
            info.Properties.TryGetValue(SignalStrengthProperty, out var rssi);
            info.Properties.TryGetValue(IsPairedProperty, out var paired);
            var device = new BluetoothDevice(this, value)
            {
                Name = String.IsNullOrWhiteSpace(info.Name) ? null : info.Name,
                Paired = paired is true
            };
            return new BluetoothDiscoveryResult(device, rssi as int?);
        }
        return null;
    }
}
