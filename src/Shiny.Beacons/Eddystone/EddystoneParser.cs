using System;
using System.Buffers.Binary;
using Shiny.Beacons.Infrastructure;

namespace Shiny.Beacons;


/// <summary>
/// Decodes the Eddystone service data carried under the 0xFEAA service UUID.
/// </summary>
public static class EddystoneParser
{
    /// <summary>The Eddystone service UUID in 16-bit short form.</summary>
    public const string ServiceUuidShort = "FEAA";

    /// <summary>The Eddystone service UUID expanded into the Bluetooth base UUID.</summary>
    public const string ServiceUuid = "0000FEAA-0000-1000-8000-00805F9B34FB";

    /// <summary>Reported when a beacon does not have a temperature sensor.</summary>
    const short TemperatureUnsupported = unchecked((short)0x8000);


    /// <summary>
    /// Whether a service UUID from an advertisement refers to Eddystone.
    /// </summary>
    /// <param name="uuid">The UUID in any of the forms the platforms produce.</param>
    /// <remarks>
    /// Apple hands back the 16-bit short form <c>"FEAA"</c>, Android the lowercase 128-bit
    /// expansion, Windows and BlueZ the uppercase one. Everything is normalized before comparison.
    /// </remarks>
    public static bool IsEddystoneServiceUuid(string? uuid)
    {
        if (uuid.IsEmpty())
            return false;

        var trimmed = uuid!.Trim();

        if (trimmed.Length == 4)
            return trimmed.Equals(ServiceUuidShort, StringComparison.OrdinalIgnoreCase);

        // "0000feaa-…" and "feaa-…" both appear in the wild
        if (trimmed.Length == 36)
            return trimmed.Equals(ServiceUuid, StringComparison.OrdinalIgnoreCase);

        return false;
    }


    /// <summary>
    /// Decodes one Eddystone service data payload.
    /// </summary>
    /// <param name="data">The service data bytes, service UUID excluded.</param>
    /// <param name="peripheralId">The platform identifier of the transmitting peripheral.</param>
    /// <param name="rssi">The signal strength for this observation, already filtered where possible.</param>
    /// <param name="options">Supplies the distance estimator, thresholds and clock.</param>
    /// <param name="filteredRssi">
    /// The filtered signal strength used for the distance calculation. Defaults to
    /// <paramref name="rssi"/> when no filter has been applied.
    /// </param>
    /// <returns>The decoded frame, or null when the payload is not a frame this library understands.</returns>
    public static EddystoneFrame? Parse(
        ReadOnlySpan<byte> data,
        string peripheralId,
        int rssi,
        BeaconRangingOptions options,
        double? filteredRssi = null
    )
    {
        if (data.Length < 2)
            return null;

        var timestamp = options.TimeProvider.GetUtcNow();
        var signal = filteredRssi ?? rssi;

        switch ((EddystoneFrameType)data[0])
        {
            case EddystoneFrameType.Uid when data.Length >= 18:
            {
                var txPower = (sbyte)data[1];
                var uid = new EddystoneUid(data.Slice(2, 10), data.Slice(12, 6));
                var (distance, proximity) = Measure(signal, txPower, options);
                return new EddystoneUidFrame(peripheralId, rssi, timestamp, uid, txPower, distance, proximity);
            }

            case EddystoneFrameType.Url when data.Length >= 3:
            {
                var txPower = (sbyte)data[1];
                var url = EddystoneUrlCodec.Decode(data.Slice(2));
                if (url == null)
                    return null;

                var (distance, proximity) = Measure(signal, txPower, options);
                return new EddystoneUrlFrame(peripheralId, rssi, timestamp, url, txPower, distance, proximity);
            }

            case EddystoneFrameType.Tlm:
                return ParseTlm(data, peripheralId, rssi, timestamp);

            case EddystoneFrameType.Eid when data.Length >= 10:
            {
                var txPower = (sbyte)data[1];
                var (distance, proximity) = Measure(signal, txPower, options);
                return new EddystoneEidFrame(peripheralId, rssi, timestamp, data.Slice(2, 8).ToArray(), txPower, distance, proximity);
            }

            default:
                return null;
        }
    }


    static EddystoneFrame? ParseTlm(ReadOnlySpan<byte> data, string peripheralId, int rssi, DateTimeOffset timestamp)
    {
        var version = data[1];

        if (version == 0x01)
        {
            // Encrypted TLM: 12 bytes of ciphertext, a 2-byte salt and a 2-byte integrity check.
            // Decoding needs the beacon's identity key, so the payload is handed over untouched.
            if (data.Length < 14)
                return null;

            return new EddystoneTlmFrame(peripheralId, rssi, timestamp, version, null, null, 0, TimeSpan.Zero, data.Slice(2).ToArray());
        }

        if (data.Length < 14)
            return null;

        var millivolts = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(2, 2));
        var rawTemperature = BinaryPrimitives.ReadInt16BigEndian(data.Slice(4, 2));
        var advertisementCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(6, 4));
        var deciseconds = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(10, 4));

        // Zero volts means the beacon is mains powered rather than flat
        double? volts = millivolts == 0 ? null : millivolts / 1000d;

        // 8.8 fixed point, with 0x8000 reserved for "no sensor fitted"
        double? celsius = rawTemperature == TemperatureUnsupported ? null : rawTemperature / 256d;

        return new EddystoneTlmFrame(
            peripheralId,
            rssi,
            timestamp,
            version,
            volts,
            celsius,
            advertisementCount,
            TimeSpan.FromMilliseconds(deciseconds * 100d),
            null
        );
    }


    static (double Distance, Proximity Proximity) Measure(double rssi, sbyte txPower, BeaconRangingOptions options)
    {
        // Eddystone calibrates at 0m while iBeacon calibrates at 1m. The estimators all assume the
        // iBeacon convention, so the advertised value is shifted by the 41 dBm the specification
        // gives as the difference between the two reference distances.
        var adjusted = (sbyte)Math.Clamp(txPower - 41, SByte.MinValue, SByte.MaxValue);
        var distance = options.DistanceEstimator.Estimate(rssi, adjusted);
        return (distance, options.ToProximity(distance));
    }
}
