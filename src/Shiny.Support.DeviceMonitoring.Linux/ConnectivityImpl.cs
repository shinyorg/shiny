using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Shiny.Net;


namespace Shiny.Net;


/// <summary>
/// Linux connectivity implementation backed by System.Net.NetworkInformation.
/// Uses NetworkChange.NetworkAddressChanged for live updates (delivered via netlink on Linux).
/// </summary>
public class ConnectivityImpl : IConnectivity
{
    NetworkAddressChangedEventHandler? addrHandler;
    NetworkAvailabilityChangedEventHandler? availHandler;
    int subscriberCount;


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
        this.addrHandler = (_, _) => this.changed?.Invoke(this, EventArgs.Empty);
        this.availHandler = (_, _) => this.changed?.Invoke(this, EventArgs.Empty);
        NetworkChange.NetworkAddressChanged += this.addrHandler;
        NetworkChange.NetworkAvailabilityChanged += this.availHandler;
    }


    void StopListening()
    {
        if (this.addrHandler != null)
        {
            NetworkChange.NetworkAddressChanged -= this.addrHandler;
            this.addrHandler = null;
        }
        if (this.availHandler != null)
        {
            NetworkChange.NetworkAvailabilityChanged -= this.availHandler;
            this.availHandler = null;
        }
    }


    public ConnectionTypes ConnectionTypes
    {
        get
        {
            var types = ConnectionTypes.None;
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                types |= nic.NetworkInterfaceType switch
                {
                    NetworkInterfaceType.Wireless80211 => ConnectionTypes.Wifi,
                    NetworkInterfaceType.Ethernet => ConnectionTypes.Wired,
                    NetworkInterfaceType.GigabitEthernet => ConnectionTypes.Wired,
                    NetworkInterfaceType.FastEthernetT => ConnectionTypes.Wired,
                    NetworkInterfaceType.FastEthernetFx => ConnectionTypes.Wired,
                    NetworkInterfaceType.Wman => ConnectionTypes.Cellular,
                    NetworkInterfaceType.Wwanpp => ConnectionTypes.Cellular,
                    NetworkInterfaceType.Wwanpp2 => ConnectionTypes.Cellular,
                    _ => ConnectionTypes.Unknown
                };
            }
            return types == ConnectionTypes.None ? ConnectionTypes.None : types;
        }
    }


    public NetworkAccess Access
    {
        get
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return NetworkAccess.None;

            var anyUp = NetworkInterface
                .GetAllNetworkInterfaces()
                .Any(n =>
                    n.OperationalStatus == OperationalStatus.Up &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            return anyUp ? NetworkAccess.Internet : NetworkAccess.Local;
        }
    }
}
