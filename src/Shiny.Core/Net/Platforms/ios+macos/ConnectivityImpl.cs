using System;
using CoreTelephony;

namespace Shiny.Net
{
    public class ConnectivityImpl : NotifyPropertyChanged, IConnectivity
    {
        readonly CTTelephonyNetworkInfo cellular = new CTTelephonyNetworkInfo();


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


        public string? CellularCarrier => this.cellular.SubscriberCellularProvider?.CarrierName;
        //{
        //    get
        //    {
        //        var tel = new CTTelephonyNetworkInfo();
        //        //tel.CellularProviderUpdatedEventHandler
        //        //tel.CurrentRadioAccessTechnology
        //        //tel.ServiceSubscriberCellularProvidersDidUpdateNotifier
        //        //tel.ServiceCurrentRadioAccessTechnology
        //        return tel.SubscriberCellularProvider.CarrierName;
        //    }
        //}

        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
            {
                Reachability.ReachabilityChanged += this.OnReachChanged;
                this.cellular.CellularProviderUpdatedEventHandler = carrier =>
                    this.RaisePropertyChanged(nameof(this.CellularCarrier));
            }
            else
            {
                Reachability.ReachabilityChanged -= this.OnReachChanged;
                this.cellular.CellularProviderUpdatedEventHandler = null;
            }
        }


        void OnReachChanged(object sender, EventArgs args)
        {
            this.RaisePropertyChanged(nameof(this.Reach));
            this.RaisePropertyChanged(nameof(this.Access));
        }
    }
}
