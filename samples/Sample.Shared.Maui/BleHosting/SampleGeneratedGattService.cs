using System.Text;
using Shiny.BluetoothLE.Hosting;

namespace Sample.Shared.Maui.BleHosting;


/// <summary>
/// The same GATT server the hand-written <see cref="Pages.BLE.BleHostingViewModel"/> builds with
/// <c>AddService(...)</c>, expressed as attributes instead. The generator emits the builder calls,
/// the offset/IsReplyNeeded handling, and the <c>NotifyTicker</c> / <c>TickerSubscribers</c> members.
/// </summary>
[BleService(SampleGattUuids.Service, Advertise = true, Name = "SampleGeneratedGatt")]
public partial class SampleGeneratedGattService(SampleBleHostingActivity activity)
{
    int reads;


    /// <summary>Reads a greeting. The context is per connected central and survives across requests.</summary>
    [ReadCharacteristic(SampleGattUuids.Greeting)]
    Task<byte[]> ReadGreeting(SampleGeneratedGattServiceContext context)
    {
        var count = Interlocked.Increment(ref this.reads);
        context.Items["lastRead"] = DateTimeOffset.Now;

        activity.Update(() =>
        {
            activity.Reads = count;
            activity.LastCentral = context.ConnectionId;
            activity.Status = $"Read #{count} from {context.ConnectionId}";
        });

        return Task.FromResult(Encoding.UTF8.GetBytes($"Hello #{count} @ {DateTime.Now:T}"));
    }


    /// <summary>
    /// Returning a <see cref="GattState"/> is what gets responded to the central - and only when the
    /// central asked for a reply. Returning nothing would respond Success, or Failure on a throw.
    /// </summary>
    [WriteCharacteristic(SampleGattUuids.Command)]
    Task<GattState> WriteCommand(byte[] data, int offset, SampleGeneratedGattServiceContext context)
    {
        if (offset != 0)
            return Task.FromResult(GattState.InvalidOffset);

        if (data.Length == 0)
            return Task.FromResult(GattState.InvalidAttributeLength);

        var value = Encoding.UTF8.GetString(data);
        context.Items["lastCommand"] = value;

        activity.Update(() =>
        {
            activity.Writes++;
            activity.LastWrite = value;
            activity.Status = $"Write from {context.ConnectionId}: {value}";
        });

        return Task.FromResult(GattState.Success);
    }


    /// <summary>
    /// The hook is optional - the generator emits NotifyTicker / TickerSubscribers /
    /// HasTickerSubscribers whether or not you write one.
    /// </summary>
    [NotifyCharacteristic(SampleGattUuids.Ticker, Name = "Ticker")]
    Task OnTickerSubscription(BleSubscription subscription, SampleGeneratedGattServiceContext context)
    {
        var count = this.TickerSubscribers.Count;
        activity.Update(() =>
        {
            activity.Subscribers = count;
            activity.Status = subscription.IsSubscribing
                ? $"{context.ConnectionId} subscribed ({count})"
                : $"{context.ConnectionId} unsubscribed ({count})";
        });
        return Task.CompletedTask;
    }


    /// <summary>
    /// A GATT write response cannot carry a payload, so the returned bytes come back as a
    /// notification addressed to whichever central wrote - it has to be subscribed first.
    /// </summary>
    [RequestResponseCharacteristic(SampleGattUuids.Echo, Name = "Echo")]
    Task<byte[]> Exchange(byte[] request, SampleGeneratedGattServiceContext context)
        => Task.FromResult(Encoding.UTF8.GetBytes($"echo({Encoding.UTF8.GetString(request)})"));


    // opt-in hooks - the compiler drops the generated call sites when they are not implemented
    partial void OnBleHandlerError(string characteristicUuid, Exception exception)
        => activity.Update(() => activity.Status = $"{characteristicUuid} failed: {exception.Message}");


    partial void OnBleResponseDropped(string characteristicUuid, Shiny.BluetoothLE.Hosting.IPeripheral peripheral, byte[] data)
        => activity.Update(() => activity.Status = $"Echo reply dropped - {peripheral.Uuid} is not subscribed");
}


/// <summary>
/// Your half of the generated context. Anything you add here rides along with the connected central
/// and is visible to every handler on the service.
/// </summary>
public partial class SampleGeneratedGattServiceContext
{
    /// <summary>When this central first touched the service.</summary>
    public DateTimeOffset FirstSeen { get; } = DateTimeOffset.Now;
}
