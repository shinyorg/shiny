using System;
using Shiny.Net;


namespace Shiny.Testing.Net
{
    public class TestConnectivity : NotifyPropertyChanged, IConnectivity
    {
        NetworkReach reach;
        public NetworkReach Reach
        {
            get => this.reach;
            set => this.Set(ref this.reach, value);
        }


        NetworkAccess access;
        public NetworkAccess Access
        {
            get => this.access;
            set => this.Set(ref this.access, value);
        }
    }
}
