using System;
using System.Linq;
using System.Reactive.Linq;
using Android.Content;
using Android.Net;
using Android.Telephony;


namespace Shiny.Net
{
    public class ConnectivityImpl : NotifyPropertyChanged, IConnectivity
    {
        readonly AndroidContext context;
        IDisposable? netmon;


        public ConnectivityImpl(AndroidContext context)
        {
            this.context = context;
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
                Logging.Log.Write("Connectivity", ex.ToString());
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
                if (this.connectivityMgr.IsNull())
                    this.connectivityMgr = this.context.GetSystemService<ConnectivityManager>(Context.ConnectivityService);

                return this.connectivityMgr;
            }
        }


        TelephonyManager? telManager;
        TelephonyManager Telephone
        {
            get
            {
                if (this.telManager.IsNull())
                    this.telManager = this.context.GetSystemService<TelephonyManager>(Context.TelephonyService);

                return this.telManager;
            }
        }
    }
}