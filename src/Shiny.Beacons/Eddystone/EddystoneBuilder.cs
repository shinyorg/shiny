using System;

namespace Shiny.Beacons;


/// <summary>
/// Builds the Eddystone service data payloads a beacon broadcasts.
/// </summary>
public static class EddystoneBuilder
{
    /// <summary>
    /// The calibrated power assumed when a caller supplies none. Eddystone calibrates at zero
    /// metres rather than iBeacon's one metre, hence the stronger value.
    /// </summary>
    public const sbyte DefaultTxPower = -18;


    /// <summary>
    /// Builds an Eddystone-UID service data payload.
    /// </summary>
    /// <param name="uid">The namespace and instance to advertise.</param>
    /// <param name="txPower">The calibrated power at zero metres to advertise.</param>
    public static byte[] BuildUid(EddystoneUid uid, sbyte txPower = DefaultTxPower)
    {
        // frame type, tx power, 10-byte namespace, 6-byte instance; the two reserved bytes are
        // optional and omitted
        var payload = new byte[18];
        payload[0] = (byte)EddystoneFrameType.Uid;
        payload[1] = (byte)txPower;
        uid.NamespaceBytes.CopyTo(payload.AsSpan(2));
        uid.InstanceBytes.CopyTo(payload.AsSpan(12));

        return payload;
    }


    /// <summary>
    /// Builds an Eddystone-URL service data payload.
    /// </summary>
    /// <param name="url">The URL to advertise. Must compress to 17 bytes or fewer.</param>
    /// <param name="txPower">The calibrated power at zero metres to advertise.</param>
    public static byte[] BuildUrl(string url, sbyte txPower = DefaultTxPower)
    {
        var encoded = EddystoneUrlCodec.Encode(url);

        var payload = new byte[2 + encoded.Length];
        payload[0] = (byte)EddystoneFrameType.Url;
        payload[1] = (byte)txPower;
        encoded.CopyTo(payload, 2);

        return payload;
    }
}
