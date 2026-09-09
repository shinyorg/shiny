using System;
using System.Threading.Tasks;

namespace Shiny.Beacons;


/// <summary>
/// Turns the device itself into a beacon.
/// </summary>
/// <remarks>
/// Only one advertisement runs at a time - starting a second replaces the first. Broadcasting is
/// suspended by the OS when the app is backgrounded on Apple platforms, and the payload iOS emits
/// in the background is not a beacon any other device can decode.
/// </remarks>
public interface IBeaconBroadcaster
{
    /// <summary>
    /// Whether an advertisement is currently running.
    /// </summary>
    bool IsBroadcasting { get; }

    /// <summary>
    /// Requests the permissions advertising needs.
    /// </summary>
    /// <returns>The resulting access state once the user has responded.</returns>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// Starts advertising as an iBeacon.
    /// </summary>
    /// <param name="uuid">The proximity UUID to advertise.</param>
    /// <param name="major">The major value.</param>
    /// <param name="minor">The minor value.</param>
    /// <param name="txPower">
    /// The power to advertise as measured at one metre. Defaults to -59 dBm.
    /// This is a number a receiver calibrates against; it does not change the actual radio power.
    /// </param>
    Task StartIBeacon(Guid uuid, ushort major, ushort minor, sbyte? txPower = null);

    /// <summary>
    /// Starts advertising an Eddystone-UID frame.
    /// </summary>
    /// <param name="uid">The namespace and instance to advertise.</param>
    /// <param name="txPower">The power to advertise as measured at zero metres. Defaults to -18 dBm.</param>
    Task StartEddystoneUid(EddystoneUid uid, sbyte? txPower = null);

    /// <summary>
    /// Starts advertising an Eddystone-URL frame.
    /// </summary>
    /// <param name="url">The URL to advertise. Must compress to 17 bytes or fewer.</param>
    /// <param name="txPower">The power to advertise as measured at zero metres. Defaults to -18 dBm.</param>
    Task StartEddystoneUrl(string url, sbyte? txPower = null);

    /// <summary>
    /// Stops the running advertisement.
    /// </summary>
    void Stop();
}
