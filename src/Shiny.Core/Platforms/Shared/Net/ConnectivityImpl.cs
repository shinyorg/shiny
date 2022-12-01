#if APPLE || ANDROID
using System;
using System.Reactive.Linq;
using MsftNet = Microsoft.Maui.Networking;

namespace Shiny.Net;


public class ConnectivityImpl : IConnectivity
{
    readonly MsftNet.IConnectivity conn = MsftNet.Connectivity.Current;


    public ConnectionTypes ConnectionTypes
    {
        get
        {
            ConnectionTypes types = 0;
            foreach (var type in this.conn.ConnectionProfiles)
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


    public NetworkAccess Access => this.conn.NetworkAccess switch
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
        this.conn.ConnectivityChanged += handler;
        return () => this.conn.ConnectivityChanged -= handler;
    });    
}
#endif