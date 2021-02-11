using System;
using System.Linq;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Net;
using Android.Telephony;
using Microsoft.Extensions.Logging;

[assembly: UsesPermission(Android.Manifest.Permission.AccessNetworkState)]


namespace Shiny.Net
{
    public class ConnectivityImpl : NotifyPropertyChanged, IConnectivity
    {
        readonly IAndroidContext context;
        readonly ILogger logger;
        IDisposable? netmon;


        public ConnectivityImpl(IAndroidContext context, ILogger<IConnectivity> logger)
        {
            this.context = context;
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
                if (!this.context.IsInManifest(Android.Manifest.Permission.AccessNetworkState))
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
                        reach = NetworkReach.Local;
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
                this.netmon = this.context
                    .WhenIntentReceived(ConnectivityManager.ConnectivityAction)
                    .Subscribe(_ => this.DoDetection());
            }
            else
            {
                this.netmon?.Dispose();
            }
        }


        static NetworkAccess ToAccess(ConnectivityType type)
        {
            switch (type)
            {
                case ConnectivityType.Ethernet    : return NetworkAccess.Ethernet;
                case ConnectivityType.Wifi        : return NetworkAccess.WiFi;
                case ConnectivityType.Bluetooth   : return NetworkAccess.Bluetooth;
                case ConnectivityType.Wimax       :
                case ConnectivityType.Mobile      :
                case ConnectivityType.MobileDun   :
                case ConnectivityType.MobileHipri :
                case ConnectivityType.MobileMms   : return NetworkAccess.Cellular;
                default                           : return NetworkAccess.Unknown;
            }
        }


        ConnectivityManager? connectivityMgr;
        ConnectivityManager Connectivity
        {
            get
            {
                if ((this.connectivityMgr?.Handle ?? IntPtr.Zero) == IntPtr.Zero)
                    this.connectivityMgr = this.context.GetSystemService<ConnectivityManager>(Context.ConnectivityService);

                return this.connectivityMgr;
            }
        }


        TelephonyManager? telManager;
        TelephonyManager Telephone
        {
            get
            {
                if ((this.telManager?.Handle ?? IntPtr.Zero) == IntPtr.Zero)
                    this.telManager = this.context.GetSystemService<TelephonyManager>(Context.TelephonyService);

                return this.telManager;
            }
        }
    }
}