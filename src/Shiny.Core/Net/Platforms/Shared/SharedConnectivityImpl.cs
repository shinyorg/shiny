using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace Shiny.Net
{
    public class SharedConnectivityImpl : NotifyPropertyChanged, IConnectivity
    {
        NetworkReach networkReach = NetworkReach.None;
        public virtual NetworkReach Reach
        {
            get => this.networkReach;
            protected set => this.Set(ref this.networkReach, value);
        }


        NetworkAccess networkAccess = NetworkAccess.None;
        public virtual NetworkAccess Access
        {
            get => this.networkAccess;
            protected set => this.Set(ref this.networkAccess, value);
        }


        public string? CellularCarrier => null;


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
            {
                NetworkChange.NetworkAddressChanged += this.OnNetworkAddressChanged;
                NetworkChange.NetworkAvailabilityChanged += this.OnNetworkAvailabilityChanged;
            }
            else
            {
                NetworkChange.NetworkAddressChanged -= this.OnNetworkAddressChanged;
                NetworkChange.NetworkAvailabilityChanged -= this.OnNetworkAvailabilityChanged;
            }
        }


        protected virtual void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs args) => this.DoDetection();
        protected virtual void OnNetworkAddressChanged(object sender, EventArgs args) => this.DoDetection();


        protected virtual void DoDetection()
        {
            var nis = NetworkInterface.GetAllNetworkInterfaces().Where(x =>
                x.OperationalStatus == OperationalStatus.Up &&
                x.NetworkInterfaceType != NetworkInterfaceType.Loopback
            );
            foreach (var ni in nis)
            {
                //ni.NetworkInterfaceType == NetworkInterfaceType.
                var addies = ni.GetIPProperties()?.UnicastAddresses;
                var ipv4 = addies?.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();
                var ipv6 = addies?.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetworkV6)?.Address.ToString();
            }
        }
    }
}
