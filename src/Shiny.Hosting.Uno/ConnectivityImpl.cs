using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Shiny.Net;
using Windows.Networking.Connectivity;
using ShinyConn = Shiny.Net.IConnectivity;

namespace Shiny;


public class ConnectivityImpl : ShinyConn
{
    public ConnectionTypes ConnectionTypes
    {
        get
        {
            //var networkInterfaceList = NetworkInformation.GetConnectionProfiles();
            //foreach (var interfaceInfo in networkInterfaceList.Where(nii => nii.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.None))
            //{
            //    var type = ConnectionProfile.Unknown;

            //    try
            //    {
            //        var adapter = interfaceInfo.NetworkAdapter;
            //        if (adapter == null)
            //            continue;

            //        // http://www.iana.org/assignments/ianaiftype-mib/ianaiftype-mib
            //        switch (adapter.IanaInterfaceType)
            //        {
            //            case 6:
            //                type = ConnectionProfile.Ethernet;
            //                break;
            //            case 71:
            //                type = ConnectionProfile.WiFi;
            //                break;
            //            case 243:
            //            case 244:
            //                type = ConnectionProfile.Cellular;
            //                break;

            //            // xbox wireless, can skip
            //            case 281:
            //                continue;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        // TODO Add Logging?
            //        Debug.WriteLine($"Unable to get Network Adapter, returning Unknown: {ex.Message}");
            //    }

            //    yield return type;
            return ConnectionTypes.None;
        }
    }

    public NetworkAccess Access
    {
        get
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile == null)
                return NetworkAccess.Unknown;

            var level = profile.GetNetworkConnectivityLevel();
            return level switch
            {
                NetworkConnectivityLevel.LocalAccess => NetworkAccess.Local,
                NetworkConnectivityLevel.InternetAccess => NetworkAccess.Internet,
                NetworkConnectivityLevel.ConstrainedInternetAccess => NetworkAccess.ConstrainedInternet,
                _ => NetworkAccess.None,
            };
        }
    }


    public IObservable<ShinyConn> WhenChanged() => Observable.Create<ShinyConn>(ob =>
    {
        var handler = new NetworkStatusChangedEventHandler(_ => ob.OnNext(this));
        NetworkInformation.NetworkStatusChanged += handler;
        return () => NetworkInformation.NetworkStatusChanged -= handler;
    });
}