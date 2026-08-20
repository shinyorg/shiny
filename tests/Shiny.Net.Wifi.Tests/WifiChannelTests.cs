using Shiny.Net.Wifi.Internals;
using Xunit;

namespace Shiny.Net.Wifi.Tests;


public class WifiChannelTests
{
    [Theory]
    [InlineData(2412, WifiBand.TwoPointFourGhz)]
    [InlineData(2437, WifiBand.TwoPointFourGhz)]
    [InlineData(2484, WifiBand.TwoPointFourGhz)]
    [InlineData(5180, WifiBand.FiveGhz)]
    [InlineData(5825, WifiBand.FiveGhz)]
    [InlineData(5955, WifiBand.SixGhz)]
    [InlineData(7115, WifiBand.SixGhz)]
    [InlineData(900, WifiBand.Unknown)]
    [InlineData(5910, WifiBand.Unknown)] // the gap between the 5 and 6 GHz allocations
    public void BandsAreClassified(int frequency, WifiBand expected)
        => Assert.Equal(expected, WifiChannels.ToBand(frequency));


    [Fact]
    public void UnreportedFrequencyHasNoBand()
        => Assert.Equal(WifiBand.Unknown, WifiChannels.ToBand(null));


    [Theory]
    [InlineData(2412, 1)]
    [InlineData(2437, 6)]
    [InlineData(2462, 11)]
    [InlineData(2484, 14)]   // the Japanese channel the 5 MHz rule does not cover
    [InlineData(5180, 36)]
    [InlineData(5745, 149)]
    [InlineData(5955, 1)]
    [InlineData(5935, 2)]    // 6 GHz channel 2 sits below channel 1
    public void ChannelsAreDerived(int frequency, int expected)
        => Assert.Equal(expected, WifiChannels.ToChannel(frequency));


    [Fact]
    public void UnknownFrequencyHasNoChannel()
    {
        Assert.Null(WifiChannels.ToChannel(null));
        Assert.Null(WifiChannels.ToChannel(900));
    }


    [Theory]
    [InlineData(WifiBand.TwoPointFourGhz, 1, 2412)]
    [InlineData(WifiBand.TwoPointFourGhz, 14, 2484)]
    [InlineData(WifiBand.FiveGhz, 36, 5180)]
    [InlineData(WifiBand.SixGhz, 1, 5955)]
    [InlineData(WifiBand.SixGhz, 2, 5935)]
    public void FrequencyRoundTripsFromChannel(WifiBand band, int channel, int expected)
    {
        Assert.Equal(expected, WifiChannels.ToFrequency(band, channel));
        Assert.Equal(channel, WifiChannels.ToChannel(expected));
    }


    [Fact]
    public void UnknownBandHasNoFrequency()
        => Assert.Null(WifiChannels.ToFrequency(WifiBand.Unknown, 6));


    [Theory]
    [InlineData(-30, 100)]
    [InlineData(-50, 100)]
    [InlineData(-75, 50)]
    [InlineData(-100, 0)]
    [InlineData(-120, 0)]
    public void SignalStrengthIsClamped(int dbm, int expected)
        => Assert.Equal(expected, WifiChannels.ToPercent(dbm));
}
