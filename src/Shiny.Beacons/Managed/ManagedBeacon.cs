using System;

namespace Shiny.Beacons.Managed;


/// <summary>
/// A beacon in a <see cref="ManagedBeaconScan"/>'s list, updated in place as new observations arrive.
/// </summary>
/// <remarks>
/// The identity is fixed for the life of the instance; only the signal measurements change. That is
/// what lets a UI bind a row to one beacon and watch it move rather than seeing rows replaced.
/// </remarks>
public class ManagedBeacon : NotifyPropertyChanged
{
    /// <summary>
    /// Creates a managed beacon from its first observation.
    /// </summary>
    /// <param name="beacon">The observation.</param>
    /// <param name="regionIdentifier">The region the scan is watching.</param>
    public ManagedBeacon(Beacon beacon, string regionIdentifier)
    {
        this.Identity = beacon.Identity;
        this.RegionIdentifier = regionIdentifier;
        this.Update(beacon);
    }


    /// <summary>The beacon's UUID, major and minor.</summary>
    public BeaconIdentity Identity { get; }

    /// <summary>The identifier of the region being scanned.</summary>
    public string RegionIdentifier { get; }

    /// <summary>The beacon's UUID.</summary>
    public Guid Uuid => this.Identity.Uuid;

    /// <summary>The beacon's major value.</summary>
    public ushort Major => this.Identity.Major;

    /// <summary>The beacon's minor value.</summary>
    public ushort Minor => this.Identity.Minor;


    Proximity proximity;
    /// <summary>The most recent proximity bucket.</summary>
    public Proximity Proximity
    {
        get => this.proximity;
        private set => this.Set(ref this.proximity, value);
    }


    double distance;
    /// <summary>The most recent estimated distance in metres.</summary>
    public double Distance
    {
        get => this.distance;
        private set => this.Set(ref this.distance, value);
    }


    int rssi;
    /// <summary>The most recent raw signal strength in dBm.</summary>
    public int Rssi
    {
        get => this.rssi;
        private set => this.Set(ref this.rssi, value);
    }


    DateTimeOffset lastSeen;
    /// <summary>When this beacon was last observed.</summary>
    public DateTimeOffset LastSeen
    {
        get => this.lastSeen;
        private set => this.Set(ref this.lastSeen, value);
    }


    /// <summary>
    /// Applies a new observation.
    /// </summary>
    /// <param name="beacon">The observation.</param>
    public void Update(Beacon beacon)
    {
        this.Proximity = beacon.Proximity;
        this.Distance = beacon.Distance;
        this.Rssi = beacon.Rssi;
        this.LastSeen = beacon.Timestamp;
    }
}
