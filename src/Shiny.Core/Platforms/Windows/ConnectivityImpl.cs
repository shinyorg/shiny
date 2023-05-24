using System;
using System.Linq;
using System.Reactive.Linq;
using Windows.Networking.Connectivity;

namespace Shiny.Net;


public class ConnectivityImpl : IConnectivity
{
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


    public IObservable<IConnectivity> WhenChanged() => Observable.Create<IConnectivity>(ob =>
    {
        var handler = new NetworkStatusChangedEventHandler(_ => ob.OnNext(this));
        NetworkInformation.NetworkStatusChanged +=  handler;

        return () => NetworkInformation.NetworkStatusChanged -= handler;
    });
}