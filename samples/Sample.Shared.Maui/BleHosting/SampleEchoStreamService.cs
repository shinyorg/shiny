using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE.Hosting;

namespace Sample.Shared.Maui.BleHosting;


/// <summary>
/// An L2CAP echo listener. <c>PsmService</c> / <c>PsmCharacteristic</c> publish the platform-assigned
/// PSM as a read characteristic on the sample GATT service, which is the only in-band way a central
/// can learn it. The generator opens the listener before AddService so an immediate read is live.
/// </summary>
[L2CapService(
    Secure = false,
    PsmService = SampleGattUuids.Service,
    PsmCharacteristic = SampleGattUuids.Psm,
    Name = "SampleEchoStream"
)]
public partial class SampleEchoStreamService(SampleBleHostingActivity activity)
{
    [OnChannelOpened]
    async Task Echo(L2CapChannel channel, BleL2CapContext context, CancellationToken cancellationToken)
    {
        activity.Update(() => activity.Status = $"L2CAP {context.PeerIdentifier} connected on PSM {context.Psm}");

        // CoC is a byte stream, not a message bus - a logical message may span buffers
        await foreach (var buffer in channel.ReadAll(cancellationToken))
            await channel.Write(buffer).ToTask(cancellationToken);

        activity.Update(() => activity.Status = $"L2CAP {context.PeerIdentifier} closed");
    }


    partial void OnL2CapChannelError(L2CapChannel channel, Exception exception)
        => activity.Update(() => activity.Status = $"L2CAP {channel.Identifier} failed: {exception.Message}");
}
