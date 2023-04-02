using Shiny.Stores;
using Shiny.Support.Repositories;

namespace Shiny.Locations;


public record GeofenceRegion(
    string Identifier,
    Position Center,
    Distance Radius
) : IRepositoryEntity
{
    /// <summary>
    /// Determines if this region is single use.
    /// </summary>
    public bool SingleUse { get; set; }

    /// <summary>
    /// Determines if the region should notify when entered.
    /// </summary>
    public bool NotifyOnEntry { get; set; } = true;

    /// <summary>
    /// Determines if the region should notify when exited.
    /// </summary>
    public bool NotifyOnExit { get; set; } = true;

    public override string ToString() => $"[Identifier: {this.Identifier}]";
}
