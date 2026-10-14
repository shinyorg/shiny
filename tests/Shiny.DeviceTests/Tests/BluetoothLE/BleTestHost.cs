using System.Collections.Concurrent;
using System.Globalization;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Hosts BleTestProfile on this device (the peripheral half of the loop) and counts what centrals do to it, so
/// either side can verify a write actually landed. Used by the BleHosting suite locally and by the
/// ble.host.* peer commands when the other device is the central.
/// </summary>
public sealed class BleTestHost(IBleHostingManager hosting, ILogger<BleTestHost> logger)
{
    readonly ConcurrentDictionary<string, BleMessageReassembler> reassemblers = new();
    readonly object sync = new();

    L2CapInstance? l2cap;
    IGattCharacteristic? notify;
    IGattCharacteristic? indicate;
    IGattCharacteristic? message;

    byte[] echo = BleTestProfile.InitialEcho;
    int reads;
    int wwrPackets;
    long wwrBytes;
    int wwrOutOfOrder;
    int wwrNextSequence;
    int notificationsSent;
    int messagesEchoed;
    long l2capBytesEchoed;
    string? lastError;


    public string? ServiceUuid { get; private set; }
    public ushort Psm { get; private set; }


    public async Task<string> StartAsync(CancellationToken cancellationToken)
    {
        await this.StopAsync().ConfigureAwait(false);

        var access = await hosting.RequestAccess().ConfigureAwait(false);
        if (access != AccessState.Available)
            throw new SkipException($"BLE hosting is not available: {access}");

        this.ResetStats();
        var serviceUuid = BleTestProfile.NewServiceUuid();
        await hosting.AddService(serviceUuid, true, this.Build).ConfigureAwait(false);
        this.ServiceUuid = serviceUuid;

        // L2CAP is optional - the PSM characteristic serves 0 when the platform cannot publish one
        try
        {
            this.l2cap = await hosting
                .OpenL2Cap(false, channel => _ = this.EchoL2CapAsync(channel))
                .ConfigureAwait(false);
            this.Psm = this.l2cap.Value.Psm;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not publish an L2CAP PSM");
            this.Psm = 0;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await hosting
            .StartAdvertising(new AdvertisementOptions(BleTestProfile.LocalName, serviceUuid))
            .ConfigureAwait(false);

        logger.LogInformation("Hosting BLE test profile {Service} (PSM {Psm})", serviceUuid, this.Psm);
        return serviceUuid;
    }


    public Task StopAsync()
    {
        if (hosting.IsAdvertising)
            hosting.StopAdvertising();

        if (this.ServiceUuid != null)
            hosting.RemoveService(this.ServiceUuid);

        this.l2cap?.Dispose();
        this.l2cap = null;
        this.Psm = 0;
        this.ServiceUuid = null;
        this.notify = null;
        this.indicate = null;
        this.message = null;
        this.reassemblers.Clear();
        return Task.CompletedTask;
    }


    public void ResetStats()
    {
        lock (this.sync)
        {
            this.echo = BleTestProfile.InitialEcho;
            this.reads = 0;
            this.wwrPackets = 0;
            this.wwrBytes = 0;
            this.wwrOutOfOrder = 0;
            this.wwrNextSequence = 0;
            this.notificationsSent = 0;
            this.messagesEchoed = 0;
            this.l2capBytesEchoed = 0;
            this.lastError = null;
        }
    }


    public BleHostStats GetStats()
    {
        lock (this.sync)
        {
            return new BleHostStats(
                this.reads,
                this.wwrPackets,
                this.wwrBytes,
                this.wwrOutOfOrder,
                this.notificationsSent,
                this.messagesEchoed,
                this.l2capBytesEchoed,
                (this.notify?.SubscribedCentrals.Count ?? 0) + (this.indicate?.SubscribedCentrals.Count ?? 0) + (this.message?.SubscribedCentrals.Count ?? 0),
                BleTestProfile.Hex(this.echo),
                this.lastError
            );
        }
    }


    void Build(IGattServiceBuilder sb)
    {
        sb.AddCharacteristic(BleTestProfile.ReadUuid, cb => cb.SetRead(req =>
        {
            lock (this.sync)
                this.reads++;

            return Task.FromResult(GattResult.Success(BleTestProfile.FromOffset(BleTestProfile.ReadValue, req.Offset)));
        }));

        sb.AddCharacteristic(BleTestProfile.EchoUuid, cb => cb
            .SetRead(req =>
            {
                byte[] value;
                lock (this.sync)
                    value = this.echo;

                return Task.FromResult(GattResult.Success(BleTestProfile.FromOffset(value, req.Offset)));
            })
            .SetWrite(req =>
            {
                lock (this.sync)
                    this.echo = req.Data;

                if (req.IsReplyNeeded)
                    req.Respond(GattState.Success);

                return Task.CompletedTask;
            })
        );

        // must stay synchronous - the platforms dispatch writes in order, and an await here would let them race
        sb.AddCharacteristic(BleTestProfile.WriteWithoutResponseUuid, cb => cb.SetWrite(req =>
        {
            lock (this.sync)
            {
                var sequence = BleTestProfile.ReadSequence(req.Data);
                if (sequence != this.wwrNextSequence)
                    this.wwrOutOfOrder++;

                this.wwrNextSequence = sequence + 1;
                this.wwrPackets++;
                this.wwrBytes += req.Data.Length;
            }
            return Task.CompletedTask;
        }, WriteOptions.WriteWithoutResponse));

        sb.AddCharacteristic(BleTestProfile.ControlUuid, cb => cb.SetWrite(req =>
        {
            var valid = req.Data is { Length: 2 } && req.Data[0] is BleTestProfile.ControlNotifyBurst or BleTestProfile.ControlIndicateBurst;
            if (req.IsReplyNeeded)
                req.Respond(valid ? GattState.Success : GattState.RequestNotSupported);

            // the burst runs after the write is answered - never inside the platform's write callback
            if (valid)
            {
                var target = req.Data[0] == BleTestProfile.ControlNotifyBurst ? this.notify : this.indicate;
                _ = Task.Run(() => this.SendBurstAsync(target!, req.Data[1]));
            }
            return Task.CompletedTask;
        }));

        this.notify = sb.AddCharacteristic(BleTestProfile.NotifyUuid, cb => cb.SetNotification(options: NotificationOptions.Notify));
        this.indicate = sb.AddCharacteristic(BleTestProfile.IndicateUuid, cb => cb.SetNotification(options: NotificationOptions.Indicate));

        // framed request/response: reassemble the central's message and send the same bytes back as a framed notify
        this.message = sb.AddCharacteristic(BleTestProfile.MessageUuid, cb => cb
            .SetNotification(sub =>
            {
                if (!sub.IsSubscribing)
                    this.reassemblers.TryRemove(sub.Peripheral.Uuid, out _);

                return Task.CompletedTask;
            })
            .SetWrite(async req =>
            {
                var reassembler = this.reassemblers.GetOrAdd(req.Peripheral.Uuid, _ => new BleMessageReassembler());
                BleMessageFrameResult frame;
                byte[]? complete;
                lock (reassembler)
                    frame = reassembler.Push(req.Data, out complete);

                if (req.IsReplyNeeded)
                    req.Respond(frame is BleMessageFrameResult.Partial or BleMessageFrameResult.Complete ? GattState.Success : GattState.Failure);

                if (frame == BleMessageFrameResult.Complete)
                {
                    try
                    {
                        await req.Characteristic.NotifyMessage(complete!, req.Peripheral).ConfigureAwait(false);
                        lock (this.sync)
                            this.messagesEchoed++;
                    }
                    catch (Exception ex)
                    {
                        this.RecordError("message echo", ex);
                    }
                }
            })
        );

        sb.AddCharacteristic(BleTestProfile.PsmUuid, cb => cb.SetRead(_ =>
        {
            var psm = this.Psm;
            return Task.FromResult(GattResult.Success([(byte)(psm & 0xFF), (byte)(psm >> 8)]));
        }));
    }


    async Task SendBurstAsync(IGattCharacteristic characteristic, int count)
    {
        try
        {
            // the central writes the control command right after subscribing; give the CCCD write time to land
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (characteristic.SubscribedCentrals.Count == 0)
                await Task.Delay(50, cts.Token).ConfigureAwait(false);

            for (var i = 0; i < count; i++)
            {
                await characteristic
                    .Notify(BleTestProfile.Packet(i, BleTestProfile.BurstPacketSize), CancellationToken.None)
                    .ConfigureAwait(false);

                lock (this.sync)
                    this.notificationsSent++;
            }
        }
        catch (Exception ex)
        {
            this.RecordError($"burst on {characteristic.Uuid}", ex);
        }
    }


    async Task EchoL2CapAsync(L2CapChannel channel)
    {
        try
        {
            using (channel)
            {
                await foreach (var buffer in channel.ReadAll())
                {
                    await channel.Write(buffer).ToTask().ConfigureAwait(false);
                    lock (this.sync)
                        this.l2capBytesEchoed += buffer.Length;
                }
            }
        }
        catch (Exception ex)
        {
            this.RecordError("l2cap echo", ex);
        }
    }


    void RecordError(string where, Exception ex)
    {
        logger.LogError(ex, "BLE test host failure in {Where}", where);
        lock (this.sync)
            this.lastError = $"{where}: {ex.Message}";
    }
}


public record BleHostStats(
    int Reads,
    int WwrPackets,
    long WwrBytes,
    int WwrOutOfOrder,
    int NotificationsSent,
    int MessagesEchoed,
    long L2CapBytesEchoed,
    int Subscribers,
    string EchoHex,
    string? LastError
)
{
    public Dictionary<string, string> ToValues() => new()
    {
        [nameof(this.Reads)] = this.Reads.ToString(CultureInfo.InvariantCulture),
        [nameof(this.WwrPackets)] = this.WwrPackets.ToString(CultureInfo.InvariantCulture),
        [nameof(this.WwrBytes)] = this.WwrBytes.ToString(CultureInfo.InvariantCulture),
        [nameof(this.WwrOutOfOrder)] = this.WwrOutOfOrder.ToString(CultureInfo.InvariantCulture),
        [nameof(this.NotificationsSent)] = this.NotificationsSent.ToString(CultureInfo.InvariantCulture),
        [nameof(this.MessagesEchoed)] = this.MessagesEchoed.ToString(CultureInfo.InvariantCulture),
        [nameof(this.L2CapBytesEchoed)] = this.L2CapBytesEchoed.ToString(CultureInfo.InvariantCulture),
        [nameof(this.Subscribers)] = this.Subscribers.ToString(CultureInfo.InvariantCulture),
        [nameof(this.EchoHex)] = this.EchoHex,
        [nameof(this.LastError)] = this.LastError ?? ""
    };


    public static BleHostStats FromValues(Dictionary<string, string> values) => new(
        Int(values, nameof(Reads)),
        Int(values, nameof(WwrPackets)),
        Int64.Parse(values[nameof(WwrBytes)], CultureInfo.InvariantCulture),
        Int(values, nameof(WwrOutOfOrder)),
        Int(values, nameof(NotificationsSent)),
        Int(values, nameof(MessagesEchoed)),
        Int64.Parse(values[nameof(L2CapBytesEchoed)], CultureInfo.InvariantCulture),
        Int(values, nameof(Subscribers)),
        values[nameof(EchoHex)],
        String.IsNullOrEmpty(values[nameof(LastError)]) ? null : values[nameof(LastError)]
    );


    static int Int(Dictionary<string, string> values, string key) => Int32.Parse(values[key], CultureInfo.InvariantCulture);
}
