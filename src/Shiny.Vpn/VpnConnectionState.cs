using System;


namespace Shiny.Vpn
{
    public enum VpnConnectionState
    {
        Connecting,
        Connected,
        Disconnecting,
        Disconnected,
        Invalid,
        Reasserting
    }
}
