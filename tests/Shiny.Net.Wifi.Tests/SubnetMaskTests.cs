using System.Net;
using Xunit;

namespace Shiny.Net.Wifi.Tests;


/// <summary>
/// NetworkManager and Android both report a prefix length where callers expect a dotted mask.
/// </summary>
public class SubnetMaskTests
{
    [Theory]
    [InlineData(24, "255.255.255.0")]
    [InlineData(16, "255.255.0.0")]
    [InlineData(8, "255.0.0.0")]
    [InlineData(23, "255.255.254.0")]
    [InlineData(32, "255.255.255.255")]
    [InlineData(0, "0.0.0.0")]
    public void PrefixLengthsBecomeMasks(int prefix, string expected)
        => Assert.Equal(IPAddress.Parse(expected), LinuxWifiManager.ToMask(prefix));
}
