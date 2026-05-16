using System;

namespace Shiny.Net;


/// <summary>
/// Monitors network connectivity state and changes
/// </summary>
public interface IConnectivity
{
    /// <summary>
    /// Fires when connectivity state changes
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Gets the current connection types (WiFi, Cellular, etc.)
    /// </summary>
    ConnectionTypes ConnectionTypes { get; }

    /// <summary>
    /// Gets the current network access level
    /// </summary>
    NetworkAccess Access { get; }
}
