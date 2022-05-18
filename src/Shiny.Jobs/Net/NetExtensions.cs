using System;
using System.Reactive.Linq;

namespace Shiny.Net;


public static class NetExtensions
{
    public static bool IsDirectConnect(this IConnectivity connectivity)
        => connectivity.Access.HasFlag(NetworkAccess.WiFi) || connectivity.Access.HasFlag(NetworkAccess.Ethernet);


    public static bool IsInternetAvailable(this IConnectivity connectivity)
        => connectivity.Reach.HasFlag(NetworkReach.Internet) || connectivity.Reach.HasFlag(NetworkReach.ConstrainedInternet);


    public static IObservable<bool> WhenInternetStatusChanged(this IConnectivity connectivity) => connectivity
        .WhenAnyProperty(x => x.Reach)
        .Select(_ => connectivity.IsInternetAvailable())
        .StartWith(connectivity.IsInternetAvailable());
}
