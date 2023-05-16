using System;
using Shiny.Support.Repositories;

namespace Shiny.Beacons;

public enum Proximity
{
    Unknown = 0,
    Immediate = 1,
    Near = 2,
    Far = 3
}


public record Beacon(
    Guid Uuid,
    ushort Major,
    ushort Minor,
    Proximity Proximity,
    int Rssi,
    double Accuracy
)
{
    public override string ToString() => $"[Beacon: Uuid={this.Uuid}, Major={this.Major}, Minor={this.Minor}]";
}


public enum BeaconRegionState
{
    Unknown,
    Entered,
    Exited
}


public record BeaconRegion(
    string Identifier,
    Guid Uuid,
    ushort? Major = null,
    ushort? Minor = null,
    bool NotifyOnEntry = true,
    bool NotifyOnExit = true
) : IRepositoryEntity
{
    // forces validation to run after primary ctor
    readonly bool valid = Check.Assert(Identifier, Major, Minor);

    internal static class Check
    {
        internal static bool Assert(string identifier, double? major, double? minor)
        {
            if (identifier.IsEmpty())
                throw new ArgumentException("Identifier is not set");

            if (major != null && major < 1)
                throw new ArgumentException("Invalid Major Value");

            if (minor != null)
            {
                if (major == null)
                    throw new InvalidOperationException("You must provide a major value if you are setting minor");

                if (minor < 1)
                    throw new InvalidOperationException("Invalid Minor Value");
            }
            return true;
        }
    }
}