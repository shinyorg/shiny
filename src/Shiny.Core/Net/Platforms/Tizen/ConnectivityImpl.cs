using System;


namespace Shiny.Net
{
    public class ConnectivityImpl : NotifyPropertyChanged, IConnectivity
    {
        public NetworkReach Reach => throw new NotImplementedException();

        public NetworkAccess Access => throw new NotImplementedException();


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            //if (hasSubscribers)
            //    Reachability.ReachabilityChanged += this.OnReachChanged;
            //else
            //    Reachability.ReachabilityChanged -= this.OnReachChanged;
        }
    }
}