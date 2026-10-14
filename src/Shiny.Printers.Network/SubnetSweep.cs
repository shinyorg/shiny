using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Shiny.Printers.Network;


static class SubnetSweep
{
    // a /24 is 254 hosts - anything wider would take minutes and flood a corporate LAN with connects
    public const int NarrowestPrefix = 24;


    /// <summary>The IPv4 subnets of this device's active interfaces, with this device's own address on each.</summary>
    public static IReadOnlyList<(IPNetwork Network, IPAddress Self)> LocalNetworks()
    {
        var list = new List<(IPNetwork, IPAddress)>();
        var interfaces = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(x =>
                x.OperationalStatus == OperationalStatus.Up &&
                x.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                x.NetworkInterfaceType != NetworkInterfaceType.Tunnel
            );

        foreach (var ni in interfaces)
        {
            var addresses = ni
                .GetIPProperties()
                .UnicastAddresses
                .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork && !IsLinkLocal(x.Address));

            foreach (var ua in addresses)
            {
                var network = ToSweepNetwork(ua.Address, PrefixLength(ua));
                if (!list.Contains((network, ua.Address)))
                    list.Add((network, ua.Address));
            }
        }
        return list;
    }


    /// <summary>The subnet to sweep for an interface address - its own subnet, or the /24 around it when wider.</summary>
    public static IPNetwork ToSweepNetwork(IPAddress address, int prefixLength)
    {
        var prefix = Math.Clamp(prefixLength, NarrowestPrefix, 32);
        var mask = uint.MaxValue << (32 - prefix);
        var value = ToUInt32(address) & mask;
        return new IPNetwork(FromUInt32(value), prefix);
    }


    /// <summary>The host addresses in an IPv4 network - without the network and broadcast addresses below a /31.</summary>
    public static IEnumerable<IPAddress> Hosts(IPNetwork network)
    {
        if (network.BaseAddress.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException($"Only IPv4 networks can be swept - {network} is not one.", nameof(network));

        var size = 1UL << (32 - network.PrefixLength);
        var first = (ulong)ToUInt32(network.BaseAddress);
        ulong start = 0, end = size;
        if (size > 2)
        {
            start = 1;
            end = size - 1;
        }

        for (var i = start; i < end; i++)
            yield return FromUInt32((uint)(first + i));
    }


    /// <summary>True if a TCP connect to <paramref name="address"/>:<paramref name="port"/> succeeds within <paramref name="timeout"/>.</summary>
    public static async Task<bool> Probe(IPAddress address, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout);
        try
        {
            await socket.ConnectAsync(address, port, linked.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }


    static int PrefixLength(UnicastIPAddressInformation address)
    {
        try
        {
            return address.PrefixLength;
        }
        catch (PlatformNotSupportedException)
        {
            // not every platform reports it - a /24 is what nearly every home and shop LAN is
            return NarrowestPrefix;
        }
    }


    static bool IsLinkLocal(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }


    static uint ToUInt32(IPAddress address)
    {
        var b = address.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }


    static IPAddress FromUInt32(uint value) => new([
        (byte)(value >> 24),
        (byte)(value >> 16),
        (byte)(value >> 8),
        (byte)value
    ]);
}
