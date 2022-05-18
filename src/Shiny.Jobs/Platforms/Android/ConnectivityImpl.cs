using System;
using System.Linq;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Net;
using Android.Telephony;
using Microsoft.Extensions.Logging;

[assembly: UsesPermission(Android.Manifest.Permission.AccessNetworkState)]

namespace Shiny.Jobs.Net;


public class ConnectivityImpl : NotifyPropertyChanged, IConnectivity
{
    readonly AndroidPlatform platform;
    readonly ILogger logger;
    IDisposable? netmon;


    public ConnectivityImpl(AndroidPlatform platform, ILogger<IConnectivity> logger)
    {
        this.platform = platform;
        this.logger = logger;
    }


    public string? CellularCarrier => this.Telephone.NetworkOperatorName;


    NetworkReach reach;
    public NetworkReach Reach
    {
        get
        {
            this.DoDetection();
            return this.reach;
        }
        private set => this.Set(ref this.reach, value);
    }


    NetworkAccess access;
    public NetworkAccess Access
    {
        get
        {
            this.DoDetection();
            return this.access;
        }
        private set => this.Set(ref this.access, value);
    }


    void DoDetection()
    {
        try
        {
            if (!this.platform.IsInManifest(Android.Manifest.Permission.AccessNetworkState))
                throw new ArgumentException("ACCESS_NETWORK_STATE has not been granted");

            // API 26
            //new ConnectivityManager.NetworkCallback();
            //this.Connectivity.RegisterNetworkCallback(new NetworkRequest
            var networks = this.Connectivity
                .GetAllNetworks()
                .Select(x => (
                    Network: x,
                    Info: this.Connectivity.GetNetworkInfo(x),
                    Caps: this.Connectivity.GetNetworkCapabilities(x)
                ))
                .Where(x =>
                    (x.Info?.IsAvailable ?? false) &&
                    (x.Info?.IsConnectedOrConnecting ?? false)
                );

            var access = NetworkAccess.None;
            var reach = NetworkReach.None;

            foreach (var network in networks)
            {
                access = ToAccess(network.Info.Type);

                if (!network.Caps.HasCapability(NetCapability.Internet))
                { 
                    reach = NetworkReach.Local;
                }
                else
                {
                    reach = NetworkReach.Internet;
                    break;
                }
            }
            this.Reach = reach;
            this.Access = access;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning("Connectivity implementation error - " + ex);
        }
    }


    protected override void OnNpcHookChanged(bool hasSubscribers)
    {
        if (hasSubscribers)
        {
            this.netmon = this.platform
                .WhenIntentReceived(ConnectivityManager.ConnectivityAction)
                .Subscribe(_ => this.DoDetection());
        }
        else
        {
            this.netmon?.Dispose();
        }
    }


    static NetworkAccess ToAccess(ConnectivityType type) => type switch
    {
        ConnectivityType.Ethernet    => NetworkAccess.Ethernet,
        ConnectivityType.Wifi        => NetworkAccess.WiFi,
        ConnectivityType.Bluetooth   => NetworkAccess.Bluetooth,
        ConnectivityType.Wimax       => NetworkAccess.Cellular,
        ConnectivityType.Mobile      => NetworkAccess.Cellular,
        ConnectivityType.MobileDun   => NetworkAccess.Cellular,
        ConnectivityType.MobileHipri => NetworkAccess.Cellular,
        ConnectivityType.MobileMms   => NetworkAccess.Cellular,
        _ => NetworkAccess.Unknown
    };


    ConnectivityManager? connectivityMgr;
    ConnectivityManager Connectivity
    {
        get
        {
            if ((this.connectivityMgr?.Handle ?? IntPtr.Zero) == IntPtr.Zero)
                this.connectivityMgr = this.platform.GetSystemService<ConnectivityManager>(Context.ConnectivityService);

            return this.connectivityMgr;
        }
    }


    TelephonyManager? telManager;
    TelephonyManager Telephone
    {
        get
        {
            if ((this.telManager?.Handle ?? IntPtr.Zero) == IntPtr.Zero)
                this.telManager = this.platform.GetSystemService<TelephonyManager>(Context.TelephonyService);

            return this.telManager;
        }
    }
}