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
        this.connectivityManager = this.platform.GetSystemService<ConnectivityManager>(Context.ConnectivityService);
    }


    public void Start()
    {
        if (!this.platform.IsInManifest(Android.Manifest.Permission.AccessNetworkState))
            throw new ArgumentException("ACCESS_NETWORK_STATE has not been granted");
    }



    public ConnectionTypes ConnectionTypes => throw new NotImplementedException();

    public NetworkAccess Access
    {
        get
        {


            return NetworkAccess.None;
        }
    }


    public IObservable<IConnectivity> WhenChanged() => Observable.Create<IConnectivity>(ob =>
    {
        var request = new NetworkRequest.Builder().Build();

        var native = this.platform.GetSystemService<ConnectivityManager>(Context.ConnectivityService);
        // TODO: need callback impl
        //native.RegisterNetworkCallback(request, null);

        return () =>
        {
            //native.UnregisterNetworkCallback(null);
        };
    });
}

