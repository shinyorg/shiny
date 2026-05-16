using System;
using System.Threading;
using Android.Content;
using Android.Net;
using Shiny.Net;

namespace Shiny.Net;


public class ConnectivityImpl : IConnectivity, IDisposable
{
    readonly AndroidPlatform platform;
    ShinyNetworkCallback? callback;
    int subscriberCount;

    public ConnectivityImpl(AndroidPlatform platform) => this.platform = platform;


    event EventHandler? changed;
    public event EventHandler? Changed
    {
        add
        {
            this.changed += value;
            if (Interlocked.Increment(ref this.subscriberCount) == 1)
                this.StartListening();
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0)
                this.StopListening();
        }
    }


    void StartListening()
    {
        var request = new NetworkRequest.Builder().Build();
        this.callback = new ShinyNetworkCallback(() => this.changed?.Invoke(this, EventArgs.Empty));
        this.Native.RegisterNetworkCallback(request, this.callback);
    }


    void StopListening()
    {
        if (this.callback != null)
        {
            this.Native.UnregisterNetworkCallback(this.callback);
            this.callback = null;
        }
    }


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

            return info.IsRoaming ? NetworkAccess.ConstrainedInternet : NetworkAccess.Internet;
        }
    }


    public void Dispose() => this.StopListening();


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
