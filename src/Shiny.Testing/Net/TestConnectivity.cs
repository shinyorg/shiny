using System;
using Shiny.Net;


namespace Shiny.Testing.Net
{
    public class TestConnectivity : NotifyPropertyChanged, IConnectivity
    {
        NetworkReach reach = NetworkReach.Internet;
        public NetworkReach Reach
        {
            get => this.reach;
            set => this.Set(ref this.reach, value);
        }


        NetworkAccess access = NetworkAccess.WiFi;
        public NetworkAccess Access
        {
            get => this.access;
            set => this.Set(ref this.access, value);
        }
    }
}
