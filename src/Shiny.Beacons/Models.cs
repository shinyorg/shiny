using System;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Beacons;


/// <summary>
/// A coarse distance bucket derived from the estimated distance to a beacon.
/// </summary>
public enum Proximity
{
    /// <summary>The distance could not be estimated - usually a missing or zero RSSI.</summary>
    Unknown = 0,

    /// <summary>Within roughly half a metre.</summary>
    Immediate = 1,

    /// <summary>Between roughly half a metre and three metres.</summary>
    Near = 2,

    /// <summary>Beyond roughly three metres.</summary>
    Far = 3
}


/// <summary>
/// Whether the device is currently inside or outside a monitored beacon region.
/// </summary>
public enum BeaconRegionState
{
    /// <summary>The region has not been evaluated yet.</summary>
    Unknown,

    /// <summary>At least one beacon matching the region is in range.</summary>
    Entered,

    /// <summary>No beacon matching the region has been seen within the exit timeout.</summary>
    Exited
}


/// <summary>
/// A single observation of an iBeacon.
/// </summary>
/// <param name="Uuid">The beacon proximity UUID.</param>
/// <param name="Major">The major value.</param>
/// <param name="Minor">The minor value.</param>
/// <param name="Rssi">The received signal strength in dBm for this observation.</param>
/// <param name="Proximity">The distance bucket derived from <paramref name="Distance"/>.</param>
/// <param name="Distance">The estimated distance to the beacon in metres.</param>
/// <param name="TxPower">
/// The calibrated power the beacon reports at one metre. Null on Apple platforms - CoreLocation
/// does not surface the raw advertisement, only its own filtered distance.
/// </param>
/// <param name="Timestamp">When the observation was taken.</param>
public record Beacon(
    Guid Uuid,
    ushort Major,
    ushort Minor,
    int Rssi,
    Proximity Proximity,
    double Distance,
    sbyte? TxPower,
    DateTimeOffset Timestamp
)
{
    /// <summary>
    /// The identity of this beacon without any of the signal measurements - use this to compare
    /// observations of the same physical beacon across time.
    /// </summary>
    public BeaconIdentity Identity => new(this.Uuid, this.Major, this.Minor);

    public override string ToString() => $"[Beacon: Uuid={this.Uuid}, Major={this.Major}, Minor={this.Minor}]";
}


/// <summary>
/// The three values that identify a physical iBeacon.
/// </summary>
/// <param name="Uuid">The beacon proximity UUID.</param>
/// <param name="Major">The major value.</param>
/// <param name="Minor">The minor value.</param>
public readonly record struct BeaconIdentity(Guid Uuid, ushort Major, ushort Minor)
{
    public override string ToString() => $"{this.Uuid}:{this.Major}:{this.Minor}";
}


/// <summary>
/// A set of iBeacons to watch for, identified by UUID and optionally narrowed by major and minor.
/// </summary>
/// <param name="Identifier">A unique identifier for the region.</param>
/// <param name="Uuid">The beacon proximity UUID that beacons must match.</param>
/// <param name="Major">When set, only beacons with this major value belong to the region.</param>
/// <param name="Minor">When set, only beacons with this major and minor value belong to the region.</param>
/// <param name="NotifyOnEntry">Whether the delegate is invoked when the region is entered.</param>
/// <param name="NotifyOnExit">Whether the delegate is invoked when the region is exited.</param>
public record BeaconRegion(
    string Identifier,
    Guid Uuid,
    ushort? Major = null,
    ushort? Minor = null,
    bool NotifyOnEntry = true,
    bool NotifyOnExit = true
) : IRepositoryEntity
{
    // forces validation to run after the primary constructor
    readonly bool valid = Validate(Identifier, Uuid, Major, Minor);

    static bool Validate(string identifier, Guid uuid, ushort? major, ushort? minor)
    {
        if (identifier.IsEmpty())
            throw new ArgumentException("Identifier is not set", nameof(identifier));

        if (uuid == Guid.Empty)
            throw new ArgumentException("A beacon region requires a UUID", nameof(uuid));

        // NOTE: zero is a perfectly legal major/minor value - the pre-5.0 module rejected it
        if (minor != null && major == null)
            throw new ArgumentException("You must provide a major value if you are setting minor", nameof(minor));

        return true;
    }


    /// <summary>
    /// Whether the supplied beacon belongs to this region.
    /// </summary>
    /// <param name="beacon">The observed beacon.</param>
    public bool IsBeaconInRegion(Beacon beacon)
        => this.IsBeaconInRegion(beacon.Uuid, beacon.Major, beacon.Minor);


    /// <summary>
    /// Whether a beacon with the supplied identity belongs to this region.
    /// </summary>
    /// <param name="uuid">The beacon proximity UUID.</param>
    /// <param name="major">The beacon major value.</param>
    /// <param name="minor">The beacon minor value.</param>
    public bool IsBeaconInRegion(Guid uuid, ushort major, ushort minor)
    {
        if (!this.Uuid.Equals(uuid))
            return false;

        if (this.Major == null)
            return true;

        if (this.Major.Value != major)
            return false;

        return this.Minor == null || this.Minor.Value == minor;
    }
}
