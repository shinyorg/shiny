using System;
using System.Reactive.Linq;
using Android.Content;
using Android.Net;

namespace Shiny.Net;

public class ConnectivityImpl : IConnectivity
{
    readonly AndroidPlatform platform;
    public ConnectivityImpl(AndroidPlatform platform) => this.platform = platform;


    public IObservable<IConnectivity> WhenChanged() => Observable
        .Create<IConnectivity>(ob =>
        {
            var request = new NetworkRequest.Builder().Build();
            var callback = new ShinyNetworkCallback(() => ob.OnNext(this));
            this.Native.RegisterNetworkCallback(request, callback);     

            return () => this.Native.UnregisterNetworkCallback(callback);
        })
        .Publish()
        .RefCount();

    public ConnectionTypes ConnectionTypes
    {
        get
        {
            var n = this.Native;
            if (n.ActiveNetwork == null)
                return ConnectionTypes.None;
            
            var caps = n.GetNetworkCapabilities(n.ActiveNetwork!);
            if (caps == null)
                return ConnectionTypes.None;
            
            ConnectionTypes types = 0;
            if (caps.HasTransport(TransportType.Wifi))
                types |= ConnectionTypes.Wifi;

            if (caps.HasTransport(TransportType.Cellular))
                types |= ConnectionTypes.Cellular;
            
            if (caps.HasTransport(TransportType.Ethernet))
                types |= ConnectionTypes.Wired;

            if (caps.HasTransport(TransportType.Bluetooth))
                types |= ConnectionTypes.Bluetooth;
            
            return types;
        }
    }

    
    public NetworkAccess Access
    {
        get
        {
            var info = this.Native.ActiveNetworkInfo;
            if (info == null || !info.IsAvailable || !info.IsConnected)
                return NetworkAccess.None;

            var access = info.IsRoaming ? NetworkAccess.ConstrainedInternet : NetworkAccess.Internet;
            return access;
        }
    }


    ConnectivityManager Native => this.platform.GetSystemService<ConnectivityManager>(Context.ConnectivityService);
}


class ShinyNetworkCallback : ConnectivityManager.NetworkCallback
{
    readonly Action onChange;
    public ShinyNetworkCallback(Action onChange) => this.onChange = onChange;
    
    public override void OnAvailable(Network network) => this.onChange.Invoke();
    public override void OnLost(Network network) => this.onChange.Invoke();
    public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities) => this.onChange.Invoke();
    public override void OnUnavailable() => this.onChange.Invoke();
    public override void OnLinkPropertiesChanged(Network network, LinkProperties linkProperties) => this.onChange.Invoke();
    public override void OnLosing(Network network, int maxMsToLive) => this.onChange.Invoke();
}