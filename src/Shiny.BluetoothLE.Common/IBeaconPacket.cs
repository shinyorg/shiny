using System;
using System.Buffers.Binary;

namespace Shiny.BluetoothLE;


/// <summary>
/// Reads and writes the 23-byte Apple iBeacon manufacturer-data payload.
/// </summary>
/// <remarks>
/// Lives here rather than in Shiny.Beacons because both roles need it: the central parses these
/// bytes out of a scan result, and the peripheral writes them into an advertisement.
/// </remarks>
/// <remarks>
/// Layout, after the two-byte company identifier has been stripped:
/// <code>
/// offset  size  field
/// 0       1     beacon type, always 0x02
/// 1       1     payload length, always 0x15 (21)
/// 2       16    proximity UUID, big-endian (RFC 4122 wire order)
/// 18      2     major, big-endian
/// 20      2     minor, big-endian
/// 22      1     measured power at 1m, signed
/// </code>
/// Every multi-byte field is big-endian. <see cref="Guid.ToByteArray()"/> and
/// <see cref="BitConverter.GetBytes(ushort)"/> are little-endian on the platforms this ships to, so
/// they must never be used here - doing so byte-swaps the UUID's first three fields and both the
/// major and minor, which is exactly the bug the pre-revival broadcaster shipped with.
/// </remarks>
public static class IBeaconPacket
{
    /// <summary>Bluetooth SIG company identifier for Apple, Inc.</summary>
    public const ushort AppleCompanyId = 0x004C;

    /// <summary>The first byte of an iBeacon payload.</summary>
    public const byte BeaconType = 0x02;

    /// <summary>The second byte of an iBeacon payload - the length of everything that follows.</summary>
    public const byte BeaconLength = 0x15;

    /// <summary>The total size of the payload, company identifier excluded.</summary>
    public const int PayloadLength = 23;

    /// <summary>The measured power assumed when a beacon reports none, or a caller supplies none.</summary>
    public const sbyte DefaultTxPower = -59;


    /// <summary>
    /// Whether the supplied manufacturer payload is a well-formed iBeacon advertisement.
    /// </summary>
    /// <param name="companyId">The company identifier the payload arrived under.</param>
    /// <param name="data">The manufacturer data with the company identifier already stripped.</param>
    public static bool IsIBeacon(ushort companyId, ReadOnlySpan<byte> data)
        => companyId == AppleCompanyId && IsIBeacon(data);


    /// <summary>
    /// Whether the supplied manufacturer payload is a well-formed iBeacon advertisement, ignoring
    /// which company identifier it arrived under.
    /// </summary>
    /// <param name="data">The manufacturer data with the company identifier already stripped.</param>
    /// <remarks>
    /// Some vendors ship iBeacon-shaped payloads under their own company identifier. Callers that
    /// want to accept those use this overload; <see cref="IsIBeacon(ushort, ReadOnlySpan{byte})"/>
    /// is the strict check.
    /// </remarks>
    public static bool IsIBeacon(ReadOnlySpan<byte> data)
        => data.Length >= PayloadLength && data[0] == BeaconType && data[1] == BeaconLength;


    /// <summary>
    /// Reads the identity and calibrated power out of an iBeacon payload.
    /// </summary>
    /// <param name="data">The manufacturer data with the company identifier already stripped.</param>
    /// <returns>The decoded values, or null when the payload is not an iBeacon.</returns>
    public static (Guid Uuid, ushort Major, ushort Minor, sbyte TxPower)? Read(ReadOnlySpan<byte> data)
    {
        if (!IsIBeacon(data))
            return null;

        var uuid = new Guid(data.Slice(2, 16), bigEndian: true);
        var major = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(18, 2));
        var minor = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(20, 2));
        var txPower = (sbyte)data[22];

        return (uuid, major, minor, txPower);
    }


    /// <summary>
    /// Builds the 23-byte iBeacon manufacturer payload, company identifier excluded.
    /// </summary>
    /// <param name="uuid">The beacon proximity UUID.</param>
    /// <param name="major">The major value.</param>
    /// <param name="minor">The minor value.</param>
    /// <param name="txPower">The power to advertise as measured at one metre.</param>
    public static byte[] Build(Guid uuid, ushort major, ushort minor, sbyte txPower = DefaultTxPower)
    {
        var buffer = new byte[PayloadLength];
        Write(buffer, uuid, major, minor, txPower);
        return buffer;
    }


    /// <summary>
    /// Writes the 23-byte iBeacon manufacturer payload into the supplied buffer.
    /// </summary>
    /// <param name="destination">A buffer of at least <see cref="PayloadLength"/> bytes.</param>
    /// <param name="uuid">The beacon proximity UUID.</param>
    /// <param name="major">The major value.</param>
    /// <param name="minor">The minor value.</param>
    /// <param name="txPower">The power to advertise as measured at one metre.</param>
    public static void Write(Span<byte> destination, Guid uuid, ushort major, ushort minor, sbyte txPower = DefaultTxPower)
    {
        if (destination.Length < PayloadLength)
            throw new ArgumentException($"An iBeacon payload needs {PayloadLength} bytes", nameof(destination));

        destination[0] = BeaconType;
        destination[1] = BeaconLength;

        if (!uuid.TryWriteBytes(destination.Slice(2, 16), bigEndian: true, out _))
            throw new ArgumentException("Could not write the beacon UUID", nameof(uuid));

        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(18, 2), major);
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(20, 2), minor);
        destination[22] = (byte)txPower;
    }
}
