using System;

namespace Shiny.Net;


public enum InternetAccess
{
    None = 0,
    Any = 1,
    Unmetered = 2
}


[Flags]
public enum ConnectionTypes
{
    None = 0,
    Unknown = 1,
    Bluetooth = 2,
    Ethernet = 4,
    WiFi = 8,
    Cellular = 16
}


public enum NetworkAccess
{
    Unknown,
    None,
    Local,
    ConstrainedInternet,
    Internet
}