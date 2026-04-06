using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive.Linq;

namespace Shiny.Net;


/// <summary>
/// Linux connectivity implementation backed by System.Net.NetworkInformation.
/// Uses NetworkChange.NetworkAddressChanged for live updates (delivered via netlink on Linux).
/// </summary>
public class ConnectivityImpl : IConnectivity
{
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

            // Anything routable beyond loopback counts as Local at minimum. We can't cheaply
            // verify Internet reachability without making a request, so we report Internet
            // when at least one interface is up and non-loopback (matches typical desktop UX).
            var anyUp = NetworkInterface
                .GetAllNetworkInterfaces()
                .Any(n =>
                    n.OperationalStatus == OperationalStatus.Up &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            return anyUp ? NetworkAccess.Internet : NetworkAccess.Local;
        }
    }


    public IObservable<IConnectivity> WhenChanged() => Observable.Create<IConnectivity>(ob =>
    {
        NetworkAddressChangedEventHandler addrHandler = (_, _) => ob.OnNext(this);
        NetworkAvailabilityChangedEventHandler availHandler = (_, _) => ob.OnNext(this);

        NetworkChange.NetworkAddressChanged += addrHandler;
        NetworkChange.NetworkAvailabilityChanged += availHandler;

        return () =>
        {
            NetworkChange.NetworkAddressChanged -= addrHandler;
            NetworkChange.NetworkAvailabilityChanged -= availHandler;
        };
    });
}
