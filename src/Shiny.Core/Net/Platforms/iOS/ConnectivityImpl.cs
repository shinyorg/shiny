using System;


namespace Shiny.Net
{
    public class ConnectivityImpl : NotifyPropertyChanged, IConnectivity
    {
        public NetworkReach Reach
        {
            get
            {
                var s = Reachability.InternetConnectionStatus();
                if (s != NetworkStatus.NotReachable)
                    return NetworkReach.Internet;

                s = Reachability.LocalWifiConnectionStatus();
                if (s != NetworkStatus.NotReachable)
                    return NetworkReach.Local;

                return NetworkReach.None;
            }
        }


        public NetworkAccess Access
        {
            get
            {
                var s = Reachability.InternetConnectionStatus();
                if (s != NetworkStatus.NotReachable)
                {
                    switch (s)
                    {
                        case NetworkStatus.ReachableViaCarrierDataNetwork:
                            return NetworkAccess.Cellular;

                        case NetworkStatus.ReachableViaWiFiNetwork:
                            return NetworkAccess.WiFi;
                    }
                }

                s = Reachability.LocalWifiConnectionStatus();
                if (s != NetworkStatus.NotReachable)
                {
                    switch (s)
                    {
                        case NetworkStatus.ReachableViaCarrierDataNetwork:
                            return NetworkAccess.Cellular;

                        case NetworkStatus.ReachableViaWiFiNetwork:
                            return NetworkAccess.WiFi;
                    }
                }
                return NetworkAccess.None;
            }
        }


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
                Reachability.ReachabilityChanged += this.OnReachChanged;
            else
                Reachability.ReachabilityChanged -= this.OnReachChanged;
        }


        void OnReachChanged(object sender, EventArgs args)
        {
            this.RaisePropertyChanged(nameof(this.Reach));
            this.RaisePropertyChanged(nameof(this.Access));
        }
    }
}
