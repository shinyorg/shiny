// Modified versions of MAUI Essentials Connectivity
using System;
using System.Reactive.Linq;
using Android.Content;
using Android.Net;

namespace Shiny.Net;


public class ConnectivityImpl : IConnectivity, IShinyStartupTask
{
    readonly AndroidPlatform platform;
    readonly ConnectivityManager connectivityManager;


    public ConnectivityImpl(AndroidPlatform platform)
    {
        this.platform = platform;
        this.connectivityManager = this.platform.GetSystemService<ConnectivityManager>(Context.ConnectivityService)!;
    }


    public void Start()
    {
        if (!this.platform.IsInManifest(Android.Manifest.Permission.AccessNetworkState))
            throw new ArgumentException("ACCESS_NETWORK_STATE has not been granted");
    }



    public ConnectionTypes ConnectionTypes
    {
        get
        {
            ConnectionTypes types = 0;
            var networks = this.connectivityManager.GetAllNetworks();
            foreach (var network in networks)
            {
                var info = this.connectivityManager.GetNetworkInfo(network);
                if (info != null && (info.IsAvailable || info.IsConnectedOrConnecting))
                {
                    var result = info.Type switch
                    {
                        ConnectivityType.Ethernet => ConnectionTypes.Wired,
                        ConnectivityType.Wifi => ConnectionTypes.Wifi,
                        ConnectivityType.Bluetooth => ConnectionTypes.Bluetooth,
                        ConnectivityType.Wimax => ConnectionTypes.Cellular,
                        ConnectivityType.Mobile => ConnectionTypes.Cellular,
                        ConnectivityType.MobileDun => ConnectionTypes.Cellular,
                        ConnectivityType.MobileHipri => ConnectionTypes.Cellular,
                        ConnectivityType.MobileMms => ConnectionTypes.Cellular,
                        ConnectivityType.Dummy => ConnectionTypes.Unknown,
                        _ => FromTypeName(info.TypeName)
                    };
                    types |= result;
                }
            }
            return types;
        }
    }


    public NetworkAccess Access
    {
        get
        {
            var currentAccess = NetworkAccess.None;
            var networks = this.connectivityManager.GetAllNetworks();

            foreach (var network in networks)
            {
                var capabilities = this.connectivityManager.GetNetworkCapabilities(network);
                if (capabilities != null)
                {
                    var info = this.connectivityManager.GetNetworkInfo(network);
                    if ((info?.IsAvailable ?? false) == true)
                    {
                        // Check to see if it has the internet capability
                        if (!capabilities.HasCapability(NetCapability.Internet))
                        {
                            // Doesn't have internet, but local is possible
                            currentAccess = IsBetterAccess(currentAccess, NetworkAccess.Local);
                        }
                        else
                        {
                            if (info.IsConnected)
                            {
                                currentAccess = IsBetterAccess(currentAccess, NetworkAccess.Internet);
                            }
                            else if (info.IsConnectedOrConnecting)
                            {
                                currentAccess = IsBetterAccess(currentAccess, NetworkAccess.ConstrainedInternet);
                            }
                        }
                    }
                }
            }
            return currentAccess;
        }
    }


    public IObservable<IConnectivity> WhenChanged() => Observable.Create<IConnectivity>(ob =>
    {
        var request = new NetworkRequest.Builder().Build()!;
        var callback = new ConnectivityNetworkCallback(() => ob.OnNext(this));
        this.connectivityManager.RegisterNetworkCallback(request, callback);

        return () => this.connectivityManager.UnregisterNetworkCallback(callback);
    });


    static NetworkAccess IsBetterAccess(NetworkAccess currentAccess, NetworkAccess newAccess) =>
        newAccess > currentAccess ? newAccess : currentAccess;


    static ConnectionTypes FromTypeName(string? typeName)
    {
        if (typeName.IsEmpty())
            return ConnectionTypes.Unknown;

        if (typeName!.Contains("mobile", StringComparison.OrdinalIgnoreCase))
            return ConnectionTypes.Cellular;

        if (typeName.Contains("wimax", StringComparison.OrdinalIgnoreCase))
            return ConnectionTypes.Cellular;

        if (typeName.Contains("wifi", StringComparison.OrdinalIgnoreCase))
            return ConnectionTypes.Wifi;

        if (typeName.Contains("ethernet", StringComparison.OrdinalIgnoreCase))
            return ConnectionTypes.Wired;

        if (typeName.Contains("bluetooth", StringComparison.OrdinalIgnoreCase))
            return ConnectionTypes.Bluetooth;

        return ConnectionTypes.Unknown;
    }
}