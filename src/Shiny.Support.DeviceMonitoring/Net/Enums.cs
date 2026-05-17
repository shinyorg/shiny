using System;

namespace Shiny.Net;


/// <summary>
/// Specifies the kind of internet connectivity required by an operation.
/// </summary>
public enum InternetAccess
{
    /// <summary>No internet connectivity is required.</summary>
    None = 0,
    /// <summary>Any internet connection is acceptable, metered or unmetered.</summary>
    Any = 1,
    /// <summary>Only an unmetered (typically Wi-Fi) connection is acceptable.</summary>
    Unmetered = 2
}


/// <summary>
/// Bit flags describing the active physical network connection types.
/// </summary>
[Flags]
public enum ConnectionTypes
{
    /// <summary>No connection.</summary>
    None = 0,
    /// <summary>Connection type could not be determined.</summary>
    Unknown = 1,
    /// <summary>Bluetooth tethering or PAN.</summary>
    Bluetooth = 2,
    /// <summary>Wired (Ethernet) connection.</summary>
    Wired = 4,
    /// <summary>Wi-Fi connection.</summary>
    Wifi = 8,
    /// <summary>Cellular data connection.</summary>
    Cellular = 16
}


/// <summary>
/// Describes the level of network access currently available.
/// </summary>
public enum NetworkAccess
{
    /// <summary>Network access could not be determined.</summary>
    Unknown,
    /// <summary>No network is available.</summary>
    None,
    /// <summary>Only a local network is reachable.</summary>
    Local,
    /// <summary>The internet is reachable but with constraints (e.g. Low Data Mode).</summary>
    ConstrainedInternet,
    /// <summary>The full internet is reachable.</summary>
    Internet
}
