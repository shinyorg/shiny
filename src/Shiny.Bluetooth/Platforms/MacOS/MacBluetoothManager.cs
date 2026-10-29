using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CoreBluetooth;
using CoreFoundation;
using Foundation;
using Microsoft.Extensions.Logging;
using ObjCRuntime;
using Shiny.Bluetooth.Infrastructure;
using Shiny.Bluetooth.MacOS;
using static Shiny.Bluetooth.MacOS.IOBluetooth;

namespace Shiny.Bluetooth;


/// <summary>
/// Classic Bluetooth on macOS (AppKit) over IOBluetooth.
/// </summary>
/// <remarks>
/// IOBluetooth has no .NET binding, so it is driven through the Objective-C runtime (see
/// <see cref="IOBluetooth"/>). Every call runs on the main thread, whose run loop delivers the
/// framework's callbacks - so the app must be running an AppKit main loop.
/// </remarks>
public class MacBluetoothManager(ILogger<MacBluetoothManager> logger) : IBluetoothManager, IBluetoothDeviceOperations
{
    const byte InquirySeconds = 10;

    // UUIDs IOBluetooth's SDP dictionaries take as raw NSData
    static readonly byte[] L2capUuid = [0x01, 0x00];
    static readonly byte[] RfcommUuid = [0x00, 0x03];
    static readonly byte[] PublicBrowseRootUuid = [0x10, 0x02];


    public BluetoothCapabilities Capabilities =>
        BluetoothCapabilities.Discovery |
        BluetoothCapabilities.Pairing |
        BluetoothCapabilities.Connect |
        BluetoothCapabilities.Listen;


    public async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        // IOBluetooth shares CoreBluetooth's TCC entry; a central manager is what raises the prompt and
        // reports the radio state
        var tcs = new TaskCompletionSource<CBManagerState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = await MainThread.Run(() =>
        {
            var central = new CBCentralManager(DispatchQueue.MainQueue);
            central.UpdatedState += (_, _) =>
            {
                if (central.State is not (CBManagerState.Unknown or CBManagerState.Resetting))
                    tcs.TrySetResult(central.State);
            };
            return central;
        }).ConfigureAwait(false);

        try
        {
            var state = await tcs.Task.WaitAsync(ct).ConfigureAwait(false);
            return state switch
            {
                CBManagerState.PoweredOn => AccessState.Available,
                CBManagerState.PoweredOff => AccessState.Disabled,
                CBManagerState.Unauthorized => AccessState.Denied,
                CBManagerState.Unsupported => AccessState.NotSupported,
                _ => AccessState.Unknown
            };
        }
        finally
        {
            manager.Dispose();
        }
    }


    public Task<IReadOnlyList<IBluetoothDevice>> GetPairedDevices(CancellationToken ct = default) => MainThread.Run<IReadOnlyList<IBluetoothDevice>>(() =>
    {
        var handle = IntPtr_objc_msgSend(Class("IOBluetoothDevice"), Sel("pairedDevices"));
        if (handle == IntPtr.Zero)
            return [];

        var array = Runtime.GetNSObject<NSArray>(handle)!;
        var list = new List<IBluetoothDevice>((int)array.Count);
        for (nuint i = 0; i < array.Count; i++)
            list.Add(this.ToDevice(array.ValueAt(i)));

        return list;
    });


    // IOBluetooth hands out a device for any well-formed address, known to it or not
    public Task<IBluetoothDevice> GetDevice(string address, CancellationToken ct = default)
        => MainThread.Run<IBluetoothDevice>(() => this.ToDevice(GetNativeDevice(address).Handle));


    public async IAsyncEnumerable<BluetoothDiscoveryResult> Discover([EnumeratorCancellation] CancellationToken ct = default)
    {
        var found = Channel.CreateUnbounded<BluetoothDiscoveryResult>();
        var inquiryDelegate = new InquiryDelegate(
            device => found.Writer.TryWrite(new BluetoothDiscoveryResult(this.ToDevice(device), ReadRssi(device))),
            (error, aborted) => found.Writer.TryComplete(error == Success || aborted
                ? null
                : new BluetoothException($"The inquiry failed ({Describe(error)})")
            )
        );

        var inquiry = await MainThread.Run(() =>
        {
            var handle = IntPtr_objc_msgSend_IntPtr(Class("IOBluetoothDeviceInquiry"), Sel("inquiryWithDelegate:"), inquiryDelegate.Handle);
            var retained = Runtime.GetNSObject(handle)!;

            // name requests would stretch the inquiry by seconds per device; the name IOBluetooth already
            // has cached comes through regardless
            Void_objc_msgSend_Byte(handle, Sel("setUpdateNewDeviceNames:"), 0);
            Void_objc_msgSend_Byte(handle, Sel("setInquiryLength:"), InquirySeconds);

            var rc = Int32_objc_msgSend(handle, Sel("start"));
            if (rc != Success)
                throw new BluetoothException($"Could not start the inquiry ({Describe(rc)}) - is Bluetooth on?");

            return retained;
        }).ConfigureAwait(false);

        try
        {
            var seen = new HashSet<string>();
            await foreach (var result in found.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                if (seen.Add(result.Device.Address))
                    yield return result;
            }
        }
        finally
        {
            await MainThread.Run(() =>
            {
                Int32_objc_msgSend(inquiry.Handle, Sel("stop"));
                inquiry.Dispose();
                GC.KeepAlive(inquiryDelegate);
            }).ConfigureAwait(false);
        }
    }


    async Task<bool> IBluetoothDeviceOperations.Pair(BluetoothDevice device, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pairingDelegate = new PairingDelegate(error => tcs.TrySetResult(error));

        var pairing = await MainThread.Run(() =>
        {
            var native = GetNativeDevice(device.Address);
            if (Byte_objc_msgSend(native.Handle, Sel("isPaired")) != 0)
                return null;

            var handle = IntPtr_objc_msgSend_IntPtr(Class("IOBluetoothDevicePair"), Sel("pairWithDevice:"), native.Handle);
            var retained = Runtime.GetNSObject(handle)!;
            Void_objc_msgSend_IntPtr(handle, Sel("setDelegate:"), pairingDelegate.Handle);

            var rc = Int32_objc_msgSend(handle, Sel("start"));
            if (rc != Success)
                throw new BluetoothException($"Could not start pairing with {device.Address} ({Describe(rc)})");

            return retained;
        }).ConfigureAwait(false);

        if (pairing == null)
            return true;

        try
        {
            using var _ = ct.Register(() => tcs.TrySetCanceled(ct));
            var error = await tcs.Task.ConfigureAwait(false);
            logger.LogDebug("Pairing {Address} finished with {Result}", device.Address, Describe(error));
            return error == Success;
        }
        finally
        {
            await MainThread.Run(() =>
            {
                Void_objc_msgSend(pairing.Handle, Sel("stop"));
                pairing.Dispose();
                GC.KeepAlive(pairingDelegate);
            }).ConfigureAwait(false);
        }
    }


    async Task<BluetoothConnection> IBluetoothDeviceOperations.Connect(BluetoothDevice device, Guid serviceUuid, bool secure, CancellationToken ct)
    {
        // IOBluetooth has no per-connection security switch - the system negotiates authentication itself
        var sdpDone = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sdpDelegate = new SdpQueryDelegate(status => sdpDone.TrySetResult(status));

        var (native, sdpUuid) = await MainThread.Run(() =>
        {
            var target = GetNativeDevice(device.Address);
            var uuid = CreateSdpUuid(serviceUuid);
            using var uuids = NSArray.FromNSObjects(uuid);

            // a fresh query rather than the cached records, so a service the device added since is found
            var rc = Int32_objc_msgSend_IntPtr_IntPtr(target.Handle, Sel("performSDPQuery:uuids:"), sdpDelegate.Handle, uuids.Handle);
            if (rc != Success)
                throw new BluetoothConnectionException($"Could not query {device.Address} for services ({Describe(rc)})");

            return (target, uuid);
        }).ConfigureAwait(false);

        var status = await sdpDone.Task.WaitAsync(ct).ConfigureAwait(false);
        GC.KeepAlive(sdpDelegate);
        if (status != Success)
            throw new BluetoothConnectionException($"The service query to {device.Address} failed ({Describe(status)}) - is it in range?");

        var channelId = await MainThread.Run(() =>
        {
            var record = IntPtr_objc_msgSend_IntPtr(native.Handle, Sel("getServiceRecordForUUID:"), sdpUuid.Handle);
            if (record == IntPtr.Zero)
                throw new BluetoothConnectionException($"{device.Address} does not publish {serviceUuid}");

            var rc = Int32_objc_msgSend_OutByte(record, Sel("getRFCOMMChannelID:"), out var id);
            if (rc != Success)
                throw new BluetoothConnectionException($"{serviceUuid} on {device.Address} is not an RFCOMM service");

            return id;
        }).ConfigureAwait(false);

        var channel = new MacRfcommChannel();
        await MainThread.Run(() =>
        {
            var rc = Int32_objc_msgSend_OutIntPtr_Byte_IntPtr(
                native.Handle,
                Sel("openRFCOMMChannelAsync:withChannelID:delegate:"),
                out var handle,
                channelId,
                channel.DelegateHandle
            );
            if (rc != Success)
                throw new BluetoothConnectionException($"Could not open RFCOMM channel {channelId} on {device.Address} ({Describe(rc)})");

            channel.Attach(handle);
        }).ConfigureAwait(false);

        int opened;
        try
        {
            opened = await channel.Opened.WaitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await channel.Stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        if (opened != Success)
        {
            await channel.Stream.DisposeAsync().ConfigureAwait(false);
            throw new BluetoothConnectionException($"{device.Address} refused the RFCOMM connection ({Describe(opened)})");
        }

        var remote = await MainThread.Run(() => this.ToDevice(native.Handle)).ConfigureAwait(false);
        return new BluetoothConnection(remote, serviceUuid, channel.Stream);
    }


    public async Task<BluetoothListener> Listen(Guid serviceUuid, string serviceName, bool secure = true, CancellationToken ct = default)
    {
        BluetoothListener? listener = null;
        var observer = new ChannelOpenObserver(handle =>
        {
            // runs on the main thread, where IOBluetooth delivers the notification
            try
            {
                var channel = new MacRfcommChannel();
                channel.AdoptIncoming(handle);
                var remote = this.ToDevice(channel.GetDevice());
                listener?.Enqueue(new BluetoothConnection(remote, serviceUuid, channel.Stream));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Dropped an incoming RFCOMM connection on {Uuid}", serviceUuid);
            }
        });

        var (record, notification) = await MainThread.Run(() =>
        {
            using var dictionary = BuildServiceRecord(serviceUuid, serviceName);
            var recordHandle = IntPtr_objc_msgSend_IntPtr(Class("IOBluetoothSDPServiceRecord"), Sel("publishedServiceRecordWithDictionary:"), dictionary.Handle);
            if (recordHandle == IntPtr.Zero)
                throw new BluetoothException($"macOS refused to publish {serviceUuid} - is Bluetooth on?");

            var published = Runtime.GetNSObject(recordHandle)!;

            // the channel in the dictionary is only a request; the record carries the one actually assigned
            var rc = Int32_objc_msgSend_OutByte(recordHandle, Sel("getRFCOMMChannelID:"), out var channelId);
            if (rc != Success)
            {
                Int32_objc_msgSend(recordHandle, Sel("removeServiceRecord"));
                throw new BluetoothException($"The published record for {serviceUuid} has no RFCOMM channel ({Describe(rc)})");
            }

            var notificationHandle = IntPtr_objc_msgSend_IntPtr_IntPtr_Byte_Int32(
                Class("IOBluetoothRFCOMMChannel"),
                Sel("registerForChannelOpenNotifications:selector:withChannel:direction:"),
                observer.Handle,
                Sel(ChannelOpenObserver.SelectorName),
                channelId,
                ChannelDirectionIncoming
            );
            logger.LogDebug("Published {Uuid} on RFCOMM channel {Channel}", serviceUuid, channelId);
            return (published, Runtime.GetNSObject(notificationHandle));
        }).ConfigureAwait(false);

        listener = new BluetoothListener(serviceUuid, serviceName, () => new ValueTask(MainThread.Run(() =>
        {
            if (notification != null)
            {
                Void_objc_msgSend(notification.Handle, Sel("unregister"));
                notification.Dispose();
            }
            Int32_objc_msgSend(record.Handle, Sel("removeServiceRecord"));
            record.Dispose();
            GC.KeepAlive(observer);
        })));
        return listener;
    }


    public Task<bool> RequestDiscoverable(TimeSpan duration, CancellationToken ct = default)
        => throw BluetoothNotSupportedException.For(
            BluetoothCapabilities.Discoverable,
            "macOS makes a Mac discoverable only while its Bluetooth settings are open"
        );


    // main thread only
    static NSObject GetNativeDevice(string address)
    {
        using var dashed = new NSString(BluetoothAddress.ToDashed(address));
        var handle = IntPtr_objc_msgSend_IntPtr(Class("IOBluetoothDevice"), Sel("deviceWithAddressString:"), dashed.Handle);
        if (handle == IntPtr.Zero)
            throw new BluetoothConnectionException($"IOBluetooth rejected the address {address}");

        return Runtime.GetNSObject(handle)!;
    }


    // main thread only
    static unsafe NSObject CreateSdpUuid(Guid uuid)
    {
        var bytes = uuid.ToByteArray(true);
        fixed (byte* p = bytes)
        {
            var handle = IntPtr_objc_msgSend_IntPtr_UInt32(Class("IOBluetoothSDPUUID"), Sel("uuidWithBytes:length:"), (IntPtr)p, 16);
            return Runtime.GetNSObject(handle)!;
        }
    }


    // main thread only
    BluetoothDevice ToDevice(IntPtr native) => new(this, CFString.FromHandle(IntPtr_objc_msgSend(native, Sel("addressString")))!)
    {
        Name = CFString.FromHandle(IntPtr_objc_msgSend(native, Sel("name"))),
        Paired = Byte_objc_msgSend(native, Sel("isPaired")) != 0,
        ClassOfDevice = UInt32_objc_msgSend(native, Sel("classOfDevice"))
    };


    // main thread only - the RSSI of the inquiry response; BluetoothHCIRSSIValue 127 is "not available"
    static int? ReadRssi(IntPtr native)
    {
        var raw = SByte_objc_msgSend(native, Sel("rawRSSI"));
        return raw == 127 ? null : raw;
    }


    /// <summary>
    /// The dictionary form <c>publishedServiceRecordWithDictionary:</c> takes - keys are the hex
    /// attribute id followed by a description, UUIDs are raw big-endian NSData, and integers are
    /// spelled out as DataElement dictionaries.
    /// </summary>
    static NSDictionary BuildServiceRecord(Guid serviceUuid, string serviceName)
    {
        var channel = NSDictionary.FromObjectsAndKeys(
            new NSObject[] { NSNumber.FromInt32(1), NSNumber.FromInt32(1), NSNumber.FromInt32(1) },
            new NSObject[] { new NSString("DataElementType"), new NSString("DataElementSize"), new NSString("DataElementValue") }
        );

        var protocols = NSArray.FromNSObjects(
            NSArray.FromNSObjects(NSData.FromArray(L2capUuid)),
            NSArray.FromNSObjects(NSData.FromArray(RfcommUuid), channel)
        );

        return NSDictionary.FromObjectsAndKeys(
            new NSObject[]
            {
                NSArray.FromNSObjects(NSData.FromArray(serviceUuid.ToByteArray(true))),
                protocols,
                NSArray.FromNSObjects(NSData.FromArray(PublicBrowseRootUuid)),
                new NSString(serviceName)
            },
            new NSObject[]
            {
                new NSString("0001 - ServiceClassIDList"),
                new NSString("0004 - ProtocolDescriptorList"),
                new NSString("0005 - BrowseGroupList*"),
                new NSString("0100 - ServiceName*")
            }
        );
    }
}
