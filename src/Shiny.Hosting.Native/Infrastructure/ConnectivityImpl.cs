// TODO: what about pure native NET7?
#if XAMARINIOS || MONOANDROID
using System;
using System.Reactive.Linq;
using Xamarin.Essentials;
using MsftNet = Xamarin.Essentials;

namespace Shiny.Net;

             
public class ConnectivityImpl : IConnectivity
{

    public ConnectionTypes ConnectionTypes
    {
        get
        {
            ConnectionTypes types = 0;
            foreach (var type in Connectivity.ConnectionProfiles)
            {
                types |= type switch
                {
                    MsftNet.ConnectionProfile.Bluetooth => ConnectionTypes.Bluetooth,
                    MsftNet.ConnectionProfile.Cellular => ConnectionTypes.Cellular,
                    MsftNet.ConnectionProfile.Ethernet => ConnectionTypes.Wired,
                    MsftNet.ConnectionProfile.WiFi => ConnectionTypes.Wifi,
                    _ => ConnectionTypes.Unknown
                };
            }
            return types;
        }
    }


    public NetworkAccess Access => Connectivity.NetworkAccess switch
    {
        MsftNet.NetworkAccess.ConstrainedInternet => NetworkAccess.ConstrainedInternet,
        MsftNet.NetworkAccess.Internet => NetworkAccess.Internet,
        MsftNet.NetworkAccess.Local => NetworkAccess.Local,
        MsftNet.NetworkAccess.None => NetworkAccess.None,
        _ => NetworkAccess.Unknown 
    };


    public IObservable<IConnectivity> WhenChanged() => Observable.Create<IConnectivity>(ob =>
    {
        var handler = new EventHandler<MsftNet.ConnectivityChangedEventArgs>((_, __) => ob.OnNext(this));
        Connectivity.ConnectivityChanged += handler;
        return () => Connectivity.ConnectivityChanged -= handler;
    });    
}
#endif