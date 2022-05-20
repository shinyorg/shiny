using System;
using Shiny.Stores;

namespace Shiny.Beacons;


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
    ushort? Minor = null
) : IStoreEntity
{
    public bool NotifyOnEntry { get; set; } = true;
    public bool NotifyOnExit { get; set; } = true;


    public void Validate()
    {
        if (this.Major != null && this.Major < 1)
            throw new ArgumentException("Invalid Major Value");

        if (this.Minor != null)
        {
            ArgumentNullException.ThrowIfNull(this.Major, "You must provide a major value if you are setting minor");

            if (this.Minor < 1)
                throw new InvalidOperationException("Invalid Minor Value");
        }
    }


    public override string ToString() => $"[Identifier: {this.Identifier} - UUID: {this.Uuid} - Major: {this.Major ?? 0} - Minor: {this.Minor ?? 0}]";
}