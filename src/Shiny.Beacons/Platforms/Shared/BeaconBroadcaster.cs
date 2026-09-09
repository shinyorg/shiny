using System;
using System.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;

namespace Shiny.Beacons;


/// <summary>
/// Broadcasts the device as an iBeacon or an Eddystone beacon.
/// </summary>
/// <remarks>
/// <para>
/// This is a thin layer over <see cref="IBleHostingManager"/> - the payloads are assembled here and
/// the platform advertising is left to the hosting module.
/// </para>
/// <para>
/// Platform limits worth knowing before you design against this:
/// </para>
/// <list type="bullet">
/// <item>
/// <b>Apple cannot broadcast Eddystone at all.</b> CoreBluetooth's <c>startAdvertising</c> accepts a
/// local name and service UUIDs and nothing else, so there is no way to put service data on the air.
/// iBeacon works because Apple exposes a private advertisement key for exactly that purpose.
/// </item>
/// <item>
/// <b>Apple stops broadcasting a usable beacon when the app is backgrounded.</b> iOS moves the
/// advertisement into an overflow area that only other iOS devices explicitly scanning for the same
/// service can see - it is not a beacon any more.
/// </item>
/// <item>
/// <b>Linux broadcasts through BlueZ</b>, which calls back into the process to read the payload -
/// so the app has to stay alive and connected to the system bus for the advertisement to keep
/// running, and the number of concurrent advertising instances is capped by the adapter.
/// </item>
/// </list>
/// </remarks>
public class BeaconBroadcaster(IBleHostingManager hostingManager) : IBeaconBroadcaster
{
    /// <inheritdoc />
    public bool IsBroadcasting => hostingManager.IsAdvertising;

    /// <inheritdoc />
    public Task<AccessState> RequestAccess() => hostingManager.RequestAccess(true, false);


    /// <inheritdoc />
    public async Task StartIBeacon(Guid uuid, ushort major, ushort minor, sbyte? txPower = null)
    {
        if (uuid == Guid.Empty)
            throw new ArgumentException("A beacon requires a UUID", nameof(uuid));

        await this.EnsureStopped().ConfigureAwait(false);
        await hostingManager
            .AdvertiseBeacon(uuid, major, minor, txPower ?? IBeaconPacket.DefaultTxPower)
            .ConfigureAwait(false);
    }


    /// <inheritdoc />
    public Task StartEddystoneUid(EddystoneUid uid, sbyte? txPower = null)
        => this.StartEddystone(EddystoneBuilder.BuildUid(uid, txPower ?? EddystoneBuilder.DefaultTxPower));


    /// <inheritdoc />
    public Task StartEddystoneUrl(string url, sbyte? txPower = null)
        => this.StartEddystone(EddystoneBuilder.BuildUrl(url, txPower ?? EddystoneBuilder.DefaultTxPower));


    /// <inheritdoc />
    public void Stop() => hostingManager.StopAdvertising();


#if APPLE
    Task StartEddystone(byte[] payload)
        // Thrown here rather than letting the hosting manager reject the options further down, so
        // the message names Eddystone and the caller is not left guessing which part is unsupported.
        => throw new PlatformNotSupportedException("Eddystone broadcasting is not possible on Apple platforms - CoreBluetooth cannot advertise service data. Use StartIBeacon instead.");
#else
    async Task StartEddystone(byte[] payload)
    {
        await this.EnsureStopped().ConfigureAwait(false);

        await hostingManager
            .StartAdvertising(new AdvertisementOptions(null, EddystoneParser.ServiceUuid)
            {
                ServiceData = [new AdvertisementServiceData(EddystoneParser.ServiceUuid, payload)],
                IsConnectable = false
            })
            .ConfigureAwait(false);
    }
#endif


    async Task EnsureStopped()
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();

        // the platforms allow exactly one advertisement, and Android throws rather than replacing it
        if (this.IsBroadcasting)
            hostingManager.StopAdvertising();
    }
}
