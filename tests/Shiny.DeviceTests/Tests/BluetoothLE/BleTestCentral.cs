using System.Buffers.Binary;
using System.Globalization;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// The central half of the loop: finds the peer's hosted BleTestProfile by its per-session service UUID and runs
/// one scenario against it. Shared by the BleCentral suite (run locally) and the ble.central.run peer command
/// (run on the other device while this one hosts), so both roles execute the same code on both platforms.
/// Scenarios throw on failure and SkipException when the platform can't do it; they return what the hosting side
/// needs to cross-check.
/// </summary>
public sealed class BleTestCentral(IBleManager bleManager)
{
    public static readonly string[] Scenarios = ["connect", "read", "echo", "wwr", "notify", "indicate", "message", "mtu", "l2cap", "reconnect"];

    public const int WwrPacketCount = 200;
    public const int NotifyCount = 50;
    public const int IndicateCount = 20;
    public const int MessageSize = 4096;
    public const int L2CapPayloadSize = 8192;

    IPeripheral? peripheral;
    string? serviceUuid;


    public async Task<Dictionary<string, string>> RunAsync(string scenario, string serviceUuid, Action<string> log, CancellationToken ct)
    {
        var p = await this.EnsureConnectedAsync(serviceUuid, log, ct).ConfigureAwait(false);
        return scenario switch
        {
            "connect" => await Connect(p, serviceUuid, log, ct).ConfigureAwait(false),
            "read" => await Read(p, serviceUuid, ct).ConfigureAwait(false),
            "echo" => await Echo(p, serviceUuid, ct).ConfigureAwait(false),
            "wwr" => await WriteWithoutResponse(p, serviceUuid, log, ct).ConfigureAwait(false),
            "notify" => await Burst(p, serviceUuid, BleTestProfile.NotifyUuid, BleTestProfile.ControlNotifyBurst, NotifyCount, false, log, ct).ConfigureAwait(false),
            "indicate" => await Burst(p, serviceUuid, BleTestProfile.IndicateUuid, BleTestProfile.ControlIndicateBurst, IndicateCount, true, log, ct).ConfigureAwait(false),
            "message" => await Message(p, serviceUuid, log, ct).ConfigureAwait(false),
            "mtu" => await Mtu(p, log, ct).ConfigureAwait(false),
            "l2cap" => await L2Cap(p, serviceUuid, log, ct).ConfigureAwait(false),
            "reconnect" => await this.Reconnect(p, serviceUuid, log, ct).ConfigureAwait(false),
            _ => throw new ArgumentException($"Unknown BLE scenario '{scenario}'")
        };
    }


    public async Task DisconnectAsync()
    {
        var p = this.peripheral;
        this.peripheral = null;
        this.serviceUuid = null;

        if (p != null)
        {
            try
            {
                await p.DisconnectAsync(CancellationToken.None, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // CancelConnection has been issued - nothing more to do
            }
        }
    }


    async Task<IPeripheral> EnsureConnectedAsync(string serviceUuid, Action<string> log, CancellationToken ct)
    {
        if (this.peripheral is { Status: ConnectionState.Connected } existing && this.serviceUuid == serviceUuid)
            return existing;

        await this.DisconnectAsync().ConfigureAwait(false);

        var access = await bleManager.RequestAccessAsync().ConfigureAwait(false);
        if (access != AccessState.Available)
            throw new SkipException($"BLE central is not available: {access}");

        if (bleManager.IsScanning)
            bleManager.StopScan();

        log($"Scanning for service {serviceUuid}");
        var result = await bleManager
            .Scan(new ScanConfig(serviceUuid))
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(30))
            .ToTask(ct)
            .ConfigureAwait(false);

        var p = result.Peripheral;
        log($"Found {p.Name ?? "(no name)"} [{p.Uuid}] advertising '{result.AdvertisementData.LocalName}' - connecting");

        // AutoConnect off: a faster first connection, and a deliberate disconnect stays disconnected
        await p.ConnectAsync(new ConnectionConfig(false), ct, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        await p.GetCharacteristicsAsync(serviceUuid, ct, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
        log($"Connected, payload MTU {p.Mtu}");

        this.peripheral = p;
        this.serviceUuid = serviceUuid;
        return p;
    }


    static async Task<Dictionary<string, string>> Connect(IPeripheral p, string serviceUuid, Action<string> log, CancellationToken ct)
    {
        var characteristics = await p.GetCharacteristicsAsync(serviceUuid, ct, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
        var found = characteristics.Select(x => x.Uuid).ToList();
        log($"Characteristics: {String.Join(", ", found)}");

        var missing = BleTestProfile
            .AllCharacteristics
            .Where(x => !found.Contains(x, StringComparer.OrdinalIgnoreCase))
            .ToList();
        Assert.True(missing.Count == 0, $"Characteristics missing from the hosted profile: {String.Join(", ", missing)}");

        var props = characteristics.ToDictionary(x => x.Uuid.ToLowerInvariant(), x => x);
        Assert.True(props[BleTestProfile.ReadUuid].CanRead(), "read characteristic is not readable");
        Assert.True(props[BleTestProfile.EchoUuid].CanWriteWithResponse(), "echo characteristic is not writable with response");
        Assert.True(props[BleTestProfile.WriteWithoutResponseUuid].CanWriteWithoutResponse(), "write-without-response characteristic does not allow it");
        Assert.True(props[BleTestProfile.NotifyUuid].CanNotify(), "notify characteristic cannot notify");
        Assert.True(props[BleTestProfile.IndicateUuid].CanIndicate(), "indicate characteristic cannot indicate");

        return new()
        {
            ["name"] = p.Name ?? "",
            ["mtu"] = p.Mtu.ToString(CultureInfo.InvariantCulture),
            ["characteristics"] = found.Count.ToString(CultureInfo.InvariantCulture)
        };
    }


    static async Task<Dictionary<string, string>> Read(IPeripheral p, string serviceUuid, CancellationToken ct)
    {
        var result = await p.ReadCharacteristicAsync(serviceUuid, BleTestProfile.ReadUuid, ct, 10000).ConfigureAwait(false);
        Assert.Equal(BleTestProfile.ReadValue, result.Data);
        return new() { ["value"] = BleTestProfile.Hex(result.Data) };
    }


    static async Task<Dictionary<string, string>> Echo(IPeripheral p, string serviceUuid, CancellationToken ct)
    {
        var payload = BleTestProfile.RandomBytes(16);
        await p.WriteCharacteristicAsync(serviceUuid, BleTestProfile.EchoUuid, payload, true, ct, 10000).ConfigureAwait(false);

        var read = await p.ReadCharacteristicAsync(serviceUuid, BleTestProfile.EchoUuid, ct, 10000).ConfigureAwait(false);
        Assert.Equal(payload, read.Data);
        return new() { ["echo"] = BleTestProfile.Hex(payload) };
    }


    // the regression this guards: Apple writes without response used to stall once CoreBluetooth's buffer filled
    static async Task<Dictionary<string, string>> WriteWithoutResponse(IPeripheral p, string serviceUuid, Action<string> log, CancellationToken ct)
    {
        var size = Math.Clamp(p.Mtu, 20, 244);
        long bytes = 0;
        for (var i = 0; i < WwrPacketCount; i++)
        {
            var packet = BleTestProfile.Packet(i, size);
            await p
                .WriteCharacteristicAsync(serviceUuid, BleTestProfile.WriteWithoutResponseUuid, packet, false, ct, 10000)
                .ConfigureAwait(false);
            bytes += packet.Length;
        }
        log($"Sent {WwrPacketCount} packets of {size} bytes without response");

        return new()
        {
            ["packets"] = WwrPacketCount.ToString(CultureInfo.InvariantCulture),
            ["bytes"] = bytes.ToString(CultureInfo.InvariantCulture)
        };
    }


    static async Task<Dictionary<string, string>> Burst(
        IPeripheral p,
        string serviceUuid,
        string characteristicUuid,
        byte command,
        int count,
        bool indicate,
        Action<string> log,
        CancellationToken ct
    )
    {
        var received = new List<int>();
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var sub = p
            .NotifyCharacteristic(serviceUuid, characteristicUuid, indicate)
            .Where(x => x.Event == BleCharacteristicEvent.Notification && x.Data != null)
            .Subscribe(
                x =>
                {
                    lock (received)
                    {
                        received.Add(BleTestProfile.ReadSequence(x.Data!));
                        if (received.Count >= count)
                            done.TrySetResult();
                    }
                },
                ex => done.TrySetException(ex)
            );

        // the host waits for the subscription to land before it starts sending
        await p.WriteCharacteristicAsync(serviceUuid, BleTestProfile.ControlUuid, [command, (byte)count], true, ct, 10000).ConfigureAwait(false);

        try
        {
            await done.Task.WaitAsync(TimeSpan.FromSeconds(45), ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            int got;
            lock (received)
                got = received.Count;

            throw new TimeoutException($"Received {got} of {count} {(indicate ? "indications" : "notifications")}");
        }

        List<int> sequences;
        lock (received)
            sequences = received.Take(count).ToList();

        log($"Received {sequences.Count} {(indicate ? "indications" : "notifications")}");
        Assert.Equal(Enumerable.Range(0, count), sequences);
        return new() { ["received"] = sequences.Count.ToString(CultureInfo.InvariantCulture) };
    }


    static async Task<Dictionary<string, string>> Message(IPeripheral p, string serviceUuid, Action<string> log, CancellationToken ct)
    {
        var payload = BleTestProfile.RandomBytes(MessageSize);
        var reply = p
            .NotifyCharacteristicMessages(serviceUuid, BleTestProfile.MessageUuid)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(60))
            .ToTask(ct);

        // the reply is a notification, so let the subscription settle before sending
        await Task.Delay(500, ct).ConfigureAwait(false);
        await p
            .WriteCharacteristicMessageAsync(serviceUuid, BleTestProfile.MessageUuid, payload, true, ct, 10000)
            .ConfigureAwait(false);

        var echoed = await reply.ConfigureAwait(false);
        log($"Framed {payload.Length} bytes out and {echoed.Length} back at payload MTU {p.Mtu}");
        Assert.Equal(payload, echoed);
        return new() { ["bytes"] = echoed.Length.ToString(CultureInfo.InvariantCulture) };
    }


    static async Task<Dictionary<string, string>> Mtu(IPeripheral p, Action<string> log, CancellationToken ct)
    {
        int mtu;
        if (p.CanRequestMtu())
        {
            mtu = await p.TryRequestMtuAsync(185, 10000, ct).ConfigureAwait(false);
            log($"Requested ATT MTU 185, payload is now {mtu}");
        }
        else
        {
            // iOS / Mac Catalyst negotiate the MTU themselves - it can only be read
            mtu = p.Mtu;
            log($"MTU cannot be requested here; negotiated payload is {mtu}");
        }

        Assert.True(mtu >= 20, $"payload MTU {mtu} is below the 20 byte minimum");
        return new() { ["mtu"] = mtu.ToString(CultureInfo.InvariantCulture) };
    }


    static async Task<Dictionary<string, string>> L2Cap(IPeripheral p, string serviceUuid, Action<string> log, CancellationToken ct)
    {
        if (!p.IsL2CapAvailable())
            throw new SkipException("This central platform has no L2CAP support");

        var psmRead = await p.ReadCharacteristicAsync(serviceUuid, BleTestProfile.PsmUuid, ct, 10000).ConfigureAwait(false);
        var psm = psmRead.Data is { Length: 2 } ? BinaryPrimitives.ReadUInt16LittleEndian(psmRead.Data) : (ushort)0;
        if (psm == 0)
            throw new SkipException("The host could not publish an L2CAP PSM");

        log($"Opening L2CAP channel on PSM {psm}");
        using var channel = await p
            .OpenL2CapChannelAsync(psm, false, ct)
            .WaitAsync(TimeSpan.FromSeconds(30), ct)
            .ConfigureAwait(false);

        var payload = BleTestProfile.RandomBytes(L2CapPayloadSize);
        var echoed = new MemoryStream();
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sub = channel.DataReceived.Subscribe(
            data =>
            {
                lock (echoed)
                {
                    echoed.Write(data);
                    if (echoed.Length >= payload.Length)
                        done.TrySetResult();
                }
            },
            ex => done.TrySetException(ex),
            () => done.TrySetException(new InvalidOperationException("The host closed the L2CAP channel before echoing everything"))
        );

        for (var offset = 0; offset < payload.Length; offset += 512)
        {
            var chunk = payload[offset..Math.Min(offset + 512, payload.Length)];
            await channel.Write(chunk).ToTask(ct).ConfigureAwait(false);
        }

        await done.Task.WaitAsync(TimeSpan.FromSeconds(45), ct).ConfigureAwait(false);
        byte[] received;
        lock (echoed)
            received = echoed.ToArray();

        log($"Echoed {received.Length} bytes over L2CAP");
        Assert.Equal(payload, received);
        return new() { ["bytes"] = received.Length.ToString(CultureInfo.InvariantCulture) };
    }


    async Task<Dictionary<string, string>> Reconnect(IPeripheral p, string serviceUuid, Action<string> log, CancellationToken ct)
    {
        await p.DisconnectAsync(ct, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
        Assert.Equal(ConnectionState.Disconnected, p.Status);
        log("Disconnected - reconnecting the same peripheral");

        await p.ConnectAsync(new ConnectionConfig(false), ct, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        Assert.Equal(ConnectionState.Connected, p.Status);

        // GATT works again after the reconnect
        var read = await p.ReadCharacteristicAsync(serviceUuid, BleTestProfile.ReadUuid, ct, 10000).ConfigureAwait(false);
        Assert.Equal(BleTestProfile.ReadValue, read.Data);

        this.peripheral = p;
        this.serviceUuid = serviceUuid;
        return new() { ["status"] = p.Status.ToString() };
    }
}
