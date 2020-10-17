using System;
using System.Linq;
using Windows.Networking.Connectivity;


namespace Shiny.Net
{
    public class ConnectivityImpl : NotifyPropertyChanged, IConnectivity
    {
        public string? CellularCarrier => null;


        public NetworkReach Reach
        {
            get
            {
                var level = NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel();
                switch (level)
                {
                    case NetworkConnectivityLevel.None:
                        return NetworkReach.None;

                    case NetworkConnectivityLevel.LocalAccess:
                        return NetworkReach.Local;

                    case NetworkConnectivityLevel.InternetAccess:
                        return NetworkReach.Internet;

                    case NetworkConnectivityLevel.ConstrainedInternetAccess:
                        return NetworkReach.ConstrainedInternet;

                    default:
                        return NetworkReach.None;
                }
            }
        }


        public NetworkAccess Access
        {
            get
            {
                var access = NetworkAccess.None;
                var list = NetworkInformation
                    .GetConnectionProfiles()
                    .Where(x =>
                        x.NetworkAdapter != null &&
                        x.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.None
                    )
                    .Select(x => x.NetworkAdapter.IanaInterfaceType);

                foreach (var item in list)
                {
                    switch (item)
                    {
                        case 6:
                            access |= NetworkAccess.Ethernet;
                            break;

                        case 71:
                            access |= NetworkAccess.WiFi;
                            break;

                        case 243:
                        case 244:
                            access |= NetworkAccess.Cellular;
                            break;
                    }
                }
                return access;
            }
        }


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
                NetworkInformation.NetworkStatusChanged += this.OnNetworkStatusChanged;
            else
                NetworkInformation.NetworkStatusChanged -= this.OnNetworkStatusChanged;
        }


        void OnNetworkStatusChanged(object sender)
        {
            this.RaisePropertyChanged(nameof(this.Reach));
            this.RaisePropertyChanged(nameof(this.Access));
        }
    }
}