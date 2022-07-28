using System;
using Android.Net;
using static Android.Net.ConnectivityManager;

namespace Shiny.Net;


public class ConnectivityNetworkCallback : NetworkCallback
{
    readonly Action onChange;
    public ConnectivityNetworkCallback(Action onChange) => this.onChange = onChange;


    public override void OnAvailable(Network network) => this.onChange.Invoke();
    public override void OnLost(Network network) => this.onChange.Invoke();
    public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities) => this.onChange.Invoke();
    public override void OnUnavailable() => this.onChange.Invoke();
    public override void OnLinkPropertiesChanged(Network network, LinkProperties linkProperties) => this.onChange.Invoke();
    public override void OnLosing(Network network, int maxMsToLive) => this.onChange.Invoke();
}
