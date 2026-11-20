using Shiny.BluetoothLE;

namespace Shiny.Beacons.Tests;


public class BeaconScanConfigFactoryTests
{
    static readonly Guid Uuid = new("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0");


    [Fact]
    public void IBeaconFilter_MatchesARealPacket()
    {
        // Android matches manufacturer data as a prefix of the payload after the company identifier.
        // If this filter does not match what a beacon actually sends, the background scan is
        // filtered down to nothing - which looks exactly like "no beacon in range".
        var filter = BeaconScanConfigFactory.GetIBeaconFilter(new BeaconRangingOptions());
        var packet = IBeaconPacket.Build(Uuid, 1, 2);

        Assert.NotNull(filter);
        Assert.Equal(IBeaconPacket.AppleCompanyId, filter!.Value.CompanyId);
        Assert.NotEmpty(filter.Value.Prefix);
        Assert.Equal(filter.Value.Prefix, packet.AsSpan(0, filter.Value.Prefix.Length).ToArray());
    }


    [Fact]
    public void IBeaconFilter_IsOffWhenNonAppleCompanyIdsAreAllowed()
    {
        // those payloads arrive under another company identifier - an Apple filter would drop them
        var filter = BeaconScanConfigFactory.GetIBeaconFilter(new BeaconRangingOptions { AllowNonAppleCompanyId = true });

        Assert.Null(filter);
    }
}
