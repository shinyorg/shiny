using System;
using System.Reactive.Linq;
using Android.Content;
using Android.Net;

namespace Shiny.Net;

public class ConnectivityImpl : IConnectivity
{
    readonly AndroidPlatform platform;
    public ConnectivityImpl(AndroidPlatform platform) => this.platform = platform;


    public IObservable<IConnectivity> WhenChanged() => Observable.Create<IConnectivity>(ob =>
    {
        var request = new NetworkRequest.Builder().Build();
        var callback = new ShinyNetworkCallback(() => ob.OnNext(this));
        this.Native.RegisterNetworkCallback(request, callback);     

        // ConnectivityManager.IsNetworkTypeValid(ConnectivityType.Bluetooth)
        return () => this.Native.UnregisterNetworkCallback(callback);
    });

    public ConnectionTypes ConnectionTypes
    {
        get
        {
            return ConnectionTypes.None;
        }
    }


    public NetworkAccess Access
    {
        get
        {
            return NetworkAccess.None;
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

//        static NetworkAccess IsBetterAccess(NetworkAccess currentAccess, NetworkAccess newAccess) =>
//            newAccess > currentAccess ? newAccess : currentAccess;

//        public NetworkAccess NetworkAccess
//        {
//            get
//            {
//                Permissions.EnsureDeclared<Permissions.NetworkState>();

//                try
//                {
//                    var currentAccess = NetworkAccess.None;
//                    var manager = ConnectivityManager;

//#pragma warning disable CS0618 // Type or member is obsolete
//#pragma warning disable CA1416 // Validate platform compatibility
//#pragma warning disable CA1422 // Validate platform compatibility
//                    var networks = manager.GetAllNetworks();
//#pragma warning restore CA1422 // Validate platform compatibility
//#pragma warning restore CA1416 // Validate platform compatibility
//#pragma warning restore CS0618 // Type or member is obsolete



//
// foreach (var network in networks)
//                    {
//                        try
//                        {
//                            var capabilities = manager.GetNetworkCapabilities(network);

//                            if (capabilities == null)
//                                continue;

//#pragma warning disable CS0618 // Type or member is obsolete
//#pragma warning disable CA1416 // Validate platform compatibility
//#pragma warning disable CA1422 // Validate platform compatibility
//                            var info = manager.GetNetworkInfo(network);

//                            if (info == null || !info.IsAvailable)
//                                continue;
//#pragma warning restore CS0618 // Type or member is obsolete

//                            // Check to see if it has the internet capability
//                            if (!capabilities.HasCapability(NetCapability.Internet))
//                            {
//                                // Doesn't have internet, but local is possible
//                                currentAccess = IsBetterAccess(currentAccess, NetworkAccess.Local);
//                                continue;
//                            }

//                            ProcessNetworkInfo(info);
//                        }
//                        catch
//                        {
//                            // there is a possibility, but don't worry
//                        }
//                    }

//                    void ProcessAllNetworkInfo()
//                    {
//#pragma warning disable CS0618 // Type or member is obsolete
//                        foreach (var info in manager.GetAllNetworkInfo())
//#pragma warning restore CS0618 // Type or member is obsolete
//                        {
//                            ProcessNetworkInfo(info);
//                        }
//                    }

//#pragma warning disable CS0618 // Type or member is obsolete
//                    void ProcessNetworkInfo(NetworkInfo info)
//                    {
//                        if (info == null || !info.IsAvailable)
//                            return;

//                        if (info.IsConnected)
//                            currentAccess = IsBetterAccess(currentAccess, NetworkAccess.Internet);
//                        else if (info.IsConnectedOrConnecting)
//                            currentAccess = IsBetterAccess(currentAccess, NetworkAccess.ConstrainedInternet);
//#pragma warning restore CA1422 // Validate platform compatibility
//#pragma warning restore CA1416 // Validate platform compatibility
//#pragma warning restore CS0618 // Type or member is obsolete
//                    }

//                    return currentAccess;
//                }
//                catch (Exception e)
//                {
//                    // TODO add Logging here
//                    Debug.WriteLine("Unable to get connected state - do you have ACCESS_NETWORK_STATE permission? - error: {0}", e);
//                    return NetworkAccess.Unknown;
//                }
//            }
//        }

//        public IEnumerable<ConnectionProfile> ConnectionProfiles
//        {
//            get
//            {
//                Permissions.EnsureDeclared<Permissions.NetworkState>();

//                var manager = ConnectivityManager;
//#pragma warning disable CS0618 // Type or member is obsolete
//#pragma warning disable CA1416 // Validate platform compatibility
//#pragma warning disable CA1422 // Validate platform compatibility
//                var networks = manager.GetAllNetworks();
//#pragma warning restore CS0618 // Type or member is obsolete
//                foreach (var network in networks)
//                {
//#pragma warning disable CS0618 // Type or member is obsolete
//                    NetworkInfo info = null;
//                    try
//                    {
//                        info = manager.GetNetworkInfo(network);
//                    }
//                    catch
//                    {
//                        // there is a possibility, but don't worry about it
//                    }
//#pragma warning restore CS0618 // Type or member is obsolete

//                    var p = ProcessNetworkInfo(info);
//                    if (p.HasValue)
//                        yield return p.Value;
//                }

//#pragma warning disable CS0618 // Type or member is obsolete
//                static ConnectionProfile? ProcessNetworkInfo(NetworkInfo info)
//                {

//                    if (info == null || !info.IsAvailable || !info.IsConnectedOrConnecting)
//                        return null;


//                    return GetConnectionType(info.Type, info.TypeName);
//                }
//#pragma warning restore CA1422 // Validate platform compatibility
//#pragma warning restore CA1416 // Validate platform compatibility
//#pragma warning restore CS0618 // Type or member is obsolete
//            }
//        }

//        internal static ConnectionProfile GetConnectionType(ConnectivityType connectivityType, string typeName)
//        {
//            switch (connectivityType)
//            {
//                case ConnectivityType.Ethernet:
//                    return ConnectionProfile.Ethernet;
//                case ConnectivityType.Wifi:
//                    return ConnectionProfile.WiFi;
//                case ConnectivityType.Bluetooth:
//                    return ConnectionProfile.Bluetooth;
//                case ConnectivityType.Wimax:
//                case ConnectivityType.Mobile:
//                case ConnectivityType.MobileDun:
//                case ConnectivityType.MobileHipri:
//                case ConnectivityType.MobileMms:
//                    return ConnectionProfile.Cellular;
//                case ConnectivityType.Dummy:
//                    return ConnectionProfile.Unknown;
//                default:
//                    if (string.IsNullOrWhiteSpace(typeName))
//                        return ConnectionProfile.Unknown;

//                    if (typeName.Contains("mobile", StringComparison.OrdinalIgnoreCase))
//                        return ConnectionProfile.Cellular;

//                    if (typeName.Contains("wimax", StringComparison.OrdinalIgnoreCase))
//                        return ConnectionProfile.Cellular;

//                    if (typeName.Contains("wifi", StringComparison.OrdinalIgnoreCase))
//                        return ConnectionProfile.WiFi;

//                    if (typeName.Contains("ethernet", StringComparison.OrdinalIgnoreCase))
//                        return ConnectionProfile.Ethernet;

//                    if (typeName.Contains("bluetooth", StringComparison.OrdinalIgnoreCase))
//                        return ConnectionProfile.Bluetooth;

//                    return ConnectionProfile.Unknown;
//            }
//        }
//    }

//    [BroadcastReceiver(Enabled = true, Exported = false, Label = "Essentials Connectivity Broadcast Receiver")]
//    class ConnectivityBroadcastReceiver : BroadcastReceiver
//    {
//        Action onChanged;

//        /// <summary>
//        /// Initializes a new instance of the <see cref="ConnectivityBroadcastReceiver"/> class.
//        /// </summary>
//        public ConnectivityBroadcastReceiver()
//        {
//        }

//        /// <summary>
//        /// Initializes a new instance of the <see cref="ConnectivityBroadcastReceiver"/> class.
//        /// </summary>
//        /// <param name="onChanged">The action that is triggered whenever the connectivity changes.</param>
//        public ConnectivityBroadcastReceiver(Action onChanged) =>
//            this.onChanged = onChanged;

//        public override async void OnReceive(Context context, Intent intent)
//        {
//#pragma warning disable CS0618 // Type or member is obsolete
//            if (intent.Action != ConnectivityManager.ConnectivityAction && intent.Action != ConnectivityImplementation.ConnectivityChangedAction)
//#pragma warning restore CS0618 // Type or member is obsolete
//                return;

//            // await 1500ms to ensure that the the connection manager updates
//            await Task.Delay(1500);
//            onChanged?.Invoke();
//        }
//    }
//}