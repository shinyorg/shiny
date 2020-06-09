using System;


namespace Shiny.Vpn
{
    public class VpnConnectionOptions
    {
        public VpnConnectionOptions(string serverAddress)
        {
            this.ServerAddress = serverAddress;
        }


        // TODO: VPN type?
        // TODO: default username/password for auto-managing the request?
        // TODO: add to settings in iOS?  Does this also work on UWP?
        //public bool LoadFromPreferences { get; set; }
        //public bool SaveToPreferences { get; set; }


        public string ServerAddress { get; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
