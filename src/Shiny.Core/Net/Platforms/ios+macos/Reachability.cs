using System;
using System.Net;
using SystemConfiguration;

using CoreFoundation;


namespace Shiny
{
    public enum NetworkStatus
    {
        NotReachable,
        ReachableViaCarrierDataNetwork,
        ReachableViaWiFiNetwork
    }


    public static class Reachability
    {
        public static string HostName = "www.google.com";


        public static bool IsReachableWithoutRequiringConnection(NetworkReachabilityFlags flags)
        {
            // Is it reachable with the current network configuration?
            var isReachable = (flags & NetworkReachabilityFlags.Reachable) != 0;

            // Do we need a connection to reach it?
#if __MACOS__
            var noConnectionRequired = (flags & NetworkReachabilityFlags.ConnectionRequired) == 0;
#else
            var noConnectionRequired = (flags & NetworkReachabilityFlags.ConnectionRequired) == 0
                                       || (flags & NetworkReachabilityFlags.IsWWAN) != 0;
#endif
            return isReachable && noConnectionRequired;
        }


        // Is the host reachable with the current network configuration
        public static bool IsHostReachable(string host)
        {
            if (string.IsNullOrEmpty(host))
                return false;

            using (var r = new NetworkReachability(host))
            {
                NetworkReachabilityFlags flags;

                if (r.TryGetFlags(out flags))
                    return IsReachableWithoutRequiringConnection(flags);
            }

            return false;
        }


        //
        // Raised every time there is an interesting reachable event,
        // we do not even pass the info as to what changed, and
        // we lump all three status we probe into one
        //
        public static event EventHandler? ReachabilityChanged;

        static void OnChange(NetworkReachabilityFlags flags) => ReachabilityChanged?.Invoke(null, EventArgs.Empty);

        //
        // Returns true if it is possible to reach the AdHoc WiFi network
        // and optionally provides extra network reachability flags as the
        // out parameter
        //
        static NetworkReachability? adHocWiFiNetworkReachability;
        public static bool IsAdHocWiFiNetworkAvailable(out NetworkReachabilityFlags flags)
        {
            if (adHocWiFiNetworkReachability == null)
            {
                adHocWiFiNetworkReachability = new NetworkReachability(new IPAddress(new byte[] {169, 254, 0, 0}));
                adHocWiFiNetworkReachability.SetNotification(OnChange);
                adHocWiFiNetworkReachability.Schedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
            }

            return adHocWiFiNetworkReachability.TryGetFlags(out flags) && IsReachableWithoutRequiringConnection(flags);
        }


        static NetworkReachability? defaultRouteReachability;
        static bool IsNetworkAvailable(out NetworkReachabilityFlags flags)
        {
            if (defaultRouteReachability == null)
            {
                defaultRouteReachability = new NetworkReachability(new IPAddress(0));
                defaultRouteReachability.SetNotification(OnChange);
                defaultRouteReachability.Schedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
            }

            return defaultRouteReachability.TryGetFlags(out flags) && IsReachableWithoutRequiringConnection(flags);
        }


        static NetworkReachability? remoteHostReachability;
        public static NetworkStatus RemoteHostStatus()
        {
            NetworkReachabilityFlags flags;
            bool reachable;

            if (remoteHostReachability == null)
            {
                remoteHostReachability = new NetworkReachability(HostName);

                // Need to probe before we queue, or we wont get any meaningful values
                // this only happens when you create NetworkReachability from a hostname
                reachable = remoteHostReachability.TryGetFlags(out flags);

                remoteHostReachability.SetNotification(OnChange);
                remoteHostReachability.Schedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
            }
            else
            {
                reachable = remoteHostReachability.TryGetFlags(out flags);
            }

            if (!reachable)
                return NetworkStatus.NotReachable;

            if (!IsReachableWithoutRequiringConnection(flags))
                return NetworkStatus.NotReachable;

#if __MACOS__
             return NetworkStatus.ReachableViaWiFiNetwork;
#else
            return (flags & NetworkReachabilityFlags.IsWWAN) != 0
                ? NetworkStatus.ReachableViaCarrierDataNetwork
                : NetworkStatus.ReachableViaWiFiNetwork;
#endif
        }


        public static NetworkStatus InternetConnectionStatus()
        {
            NetworkReachabilityFlags flags;
            var defaultNetworkAvailable = IsNetworkAvailable(out flags);

            if (defaultNetworkAvailable && ((flags & NetworkReachabilityFlags.IsDirect) != 0))
                return NetworkStatus.NotReachable;
#if !__MACOS__
            if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
                return NetworkStatus.ReachableViaCarrierDataNetwork;
#endif
            if (flags == 0)
                return NetworkStatus.NotReachable;

            return NetworkStatus.ReachableViaWiFiNetwork;
        }


        public static NetworkStatus LocalWifiConnectionStatus()
        {
            NetworkReachabilityFlags flags;
            if (IsAdHocWiFiNetworkAvailable(out flags))
                if ((flags & NetworkReachabilityFlags.IsDirect) != 0)
                    return NetworkStatus.ReachableViaWiFiNetwork;

            return NetworkStatus.NotReachable;
        }
    }
}