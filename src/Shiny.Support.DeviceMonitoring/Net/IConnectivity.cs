using System;

namespace Shiny.Net;


/// <summary>
/// Monitors network connectivity state and changes
/// </summary>
public interface IConnectivity
{
    /// <summary>
    /// Returns an observable that emits whenever <see cref="ConnectionTypes"/> or
    /// <see cref="Access"/> changes. The same instance is emitted, with updated property values.
    /// </summary>
    IObservable<IConnectivity> WhenChanged();

    /// <summary>
    /// Gets the current connection types (WiFi, Cellular, etc.)
    /// </summary>
    ConnectionTypes ConnectionTypes { get; }

    /// <summary>
    /// Gets the current network access level
    /// </summary>
    NetworkAccess Access { get; }
}