using System;

namespace Shiny.Beacons;


/// <summary>
/// The Eddystone frame types defined by the specification.
/// </summary>
public enum EddystoneFrameType : byte
{
    /// <summary>A 16-byte beacon identity - see <see cref="EddystoneUidFrame"/>.</summary>
    Uid = 0x00,

    /// <summary>A compressed URL - see <see cref="EddystoneUrlFrame"/>.</summary>
    Url = 0x10,

    /// <summary>Telemetry about the beacon itself - see <see cref="EddystoneTlmFrame"/>.</summary>
    Tlm = 0x20,

    /// <summary>A rotating encrypted identity - see <see cref="EddystoneEidFrame"/>.</summary>
    Eid = 0x30
}


/// <summary>
/// A single observed Eddystone advertisement.
/// </summary>
/// <param name="PeripheralId">
/// The platform's identifier for the transmitting peripheral. Use this to correlate a TLM frame
/// with the UID or URL frame the same beacon interleaves it with.
/// </param>
/// <param name="Rssi">The received signal strength in dBm for this observation.</param>
/// <param name="Timestamp">When the observation was taken.</param>
public abstract record EddystoneFrame(
    string PeripheralId,
    int Rssi,
    DateTimeOffset Timestamp
)
{
    /// <summary>Which kind of frame this is.</summary>
    public abstract EddystoneFrameType FrameType { get; }
}


/// <summary>
/// An Eddystone-UID frame, carrying the beacon's namespace and instance identity.
/// </summary>
/// <param name="Uid">The 16-byte identity.</param>
/// <param name="TxPower">The beacon's calibrated power at zero metres, as advertised.</param>
/// <param name="Distance">The estimated distance in metres.</param>
/// <param name="Proximity">The distance bucket derived from <paramref name="Distance"/>.</param>
public sealed record EddystoneUidFrame(
    string PeripheralId,
    int Rssi,
    DateTimeOffset Timestamp,
    EddystoneUid Uid,
    sbyte TxPower,
    double Distance,
    Proximity Proximity
) : EddystoneFrame(PeripheralId, Rssi, Timestamp)
{
    /// <inheritdoc />
    public override EddystoneFrameType FrameType => EddystoneFrameType.Uid;
}


/// <summary>
/// An Eddystone-URL frame, carrying a URL the beacon broadcasts.
/// </summary>
/// <param name="Url">The decompressed URL.</param>
/// <param name="TxPower">The beacon's calibrated power at zero metres, as advertised.</param>
/// <param name="Distance">The estimated distance in metres.</param>
/// <param name="Proximity">The distance bucket derived from <paramref name="Distance"/>.</param>
public sealed record EddystoneUrlFrame(
    string PeripheralId,
    int Rssi,
    DateTimeOffset Timestamp,
    string Url,
    sbyte TxPower,
    double Distance,
    Proximity Proximity
) : EddystoneFrame(PeripheralId, Rssi, Timestamp)
{
    /// <inheritdoc />
    public override EddystoneFrameType FrameType => EddystoneFrameType.Url;
}


/// <summary>
/// An Eddystone-TLM frame, carrying the beacon's own health telemetry.
/// </summary>
/// <param name="Version">The TLM version byte. 0x00 is unencrypted, 0x01 is encrypted.</param>
/// <param name="BatteryVolts">
/// Battery voltage in volts, or null when the beacon runs from mains power and reports zero.
/// Only present on unencrypted frames.
/// </param>
/// <param name="TemperatureCelsius">
/// Beacon temperature in degrees celsius, or null when the beacon does not have a sensor.
/// Only present on unencrypted frames.
/// </param>
/// <param name="AdvertisementCount">
/// How many advertisements the beacon has sent since power-on. Only present on unencrypted frames.
/// </param>
/// <param name="Uptime">
/// How long the beacon has been powered on, to a resolution of 100 milliseconds.
/// Only present on unencrypted frames.
/// </param>
/// <param name="EncryptedPayload">
/// The raw payload of an encrypted (version 0x01) frame, for a caller that holds the beacon's
/// identity key. Null on unencrypted frames.
/// </param>
public sealed record EddystoneTlmFrame(
    string PeripheralId,
    int Rssi,
    DateTimeOffset Timestamp,
    byte Version,
    double? BatteryVolts,
    double? TemperatureCelsius,
    uint AdvertisementCount,
    TimeSpan Uptime,
    byte[]? EncryptedPayload
) : EddystoneFrame(PeripheralId, Rssi, Timestamp)
{
    /// <inheritdoc />
    public override EddystoneFrameType FrameType => EddystoneFrameType.Tlm;

    /// <summary>Whether this frame's contents are encrypted and therefore not decoded.</summary>
    public bool IsEncrypted => this.Version == 0x01;
}


/// <summary>
/// An Eddystone-EID frame, carrying a rotating 8-byte ephemeral identifier.
/// </summary>
/// <param name="EphemeralId">The raw 8-byte identifier.</param>
/// <param name="TxPower">The beacon's calibrated power at zero metres, as advertised.</param>
/// <param name="Distance">The estimated distance in metres.</param>
/// <param name="Proximity">The distance bucket derived from <paramref name="Distance"/>.</param>
/// <remarks>
/// The identifier is surfaced raw. Resolving it back to a registered beacon needs the deployment's
/// identity key and the Curve25519/AES-EAX derivation from the Eddystone-EID specification, which
/// this library does not implement.
/// </remarks>
public sealed record EddystoneEidFrame(
    string PeripheralId,
    int Rssi,
    DateTimeOffset Timestamp,
    byte[] EphemeralId,
    sbyte TxPower,
    double Distance,
    Proximity Proximity
) : EddystoneFrame(PeripheralId, Rssi, Timestamp)
{
    /// <inheritdoc />
    public override EddystoneFrameType FrameType => EddystoneFrameType.Eid;

    /// <summary>The ephemeral identifier as 16 uppercase hex characters.</summary>
    public string EphemeralIdHex => Convert.ToHexString(this.EphemeralId);
}
