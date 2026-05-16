using System;
using System.Linq;
using System.Threading;
using Shiny.Net;
using Windows.Networking.Connectivity;

namespace Shiny.Net;


public class ConnectivityImpl : IConnectivity
{
    NetworkStatusChangedEventHandler? handler;
    int subscriberCount;


    event EventHandler? changed;
    public event EventHandler? Changed
    {
        add
        {
            this.changed += value;
            if (Interlocked.Increment(ref this.subscriberCount) == 1)
            {
                this.handler = _ => this.changed?.Invoke(this, EventArgs.Empty);
                NetworkInformation.NetworkStatusChanged += this.handler;
            }
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0 && this.handler != null)
            {
                NetworkInformation.NetworkStatusChanged -= this.handler;
                this.handler = null;
            }
        }
    }


    public ConnectionTypes ConnectionTypes
    {
        get
        {
            var access = ConnectionTypes.None;
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
                        access |= ConnectionTypes.Wired;
                        break;
                    case 71:
                        access |= ConnectionTypes.Wifi;
                        break;
                    case 243:
                    case 244:
                        access |= ConnectionTypes.Cellular;
                        break;
                }
            }
            return access;
        }
    }


    public NetworkAccess Access
    {
        get
        {
            var level = NetworkInformation
                .GetInternetConnectionProfile()?
                .GetNetworkConnectivityLevel();

            return level switch
            {
                NetworkConnectivityLevel.ConstrainedInternetAccess => NetworkAccess.ConstrainedInternet,
                NetworkConnectivityLevel.LocalAccess => NetworkAccess.Local,
                NetworkConnectivityLevel.InternetAccess => NetworkAccess.Internet,
                _ => NetworkAccess.None
            };
        }
    }
}
