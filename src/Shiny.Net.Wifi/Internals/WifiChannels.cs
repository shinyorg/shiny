namespace Shiny.Net.Wifi.Internals;


/// <summary>
/// Maps a centre frequency to its band and channel number.
/// </summary>
/// <remarks>
/// Every platform reports the frequency in MHz but only some report the channel, so the channel is
/// derived here rather than being carried through five different native shapes. The arithmetic is
/// straight out of 802.11: channels are 5 MHz apart from a per-band base, with two historical
/// exceptions (2.4 GHz channel 14 and 6 GHz channel 2) that do not fit the formula.
/// </remarks>
internal static class WifiChannels
{
    const int Channel14Frequency = 2484;
    const int SixGhzChannel2Frequency = 5935;

    public static WifiBand ToBand(int? frequencyMhz) => frequencyMhz switch
    {
        null => WifiBand.Unknown,
        >= 2401 and <= 2495 => WifiBand.TwoPointFourGhz,
        >= 5150 and <= 5895 => WifiBand.FiveGhz,
        >= 5925 and <= 7125 => WifiBand.SixGhz,
        _ => WifiBand.Unknown
    };


    public static int? ToChannel(int? frequencyMhz)
    {
        if (frequencyMhz == null)
            return null;

        var freq = frequencyMhz.Value;

        // the two frequencies the 5 MHz spacing rule does not cover
        if (freq == Channel14Frequency)
            return 14;

        if (freq == SixGhzChannel2Frequency)
            return 2;

        return ToBand(freq) switch
        {
            WifiBand.TwoPointFourGhz => (freq - 2407) / 5,
            WifiBand.FiveGhz => (freq - 5000) / 5,
            WifiBand.SixGhz => (freq - 5950) / 5,
            _ => null
        };
    }


    /// <summary>
    /// The inverse of <see cref="ToChannel"/>, for the platforms that report a band and channel
    /// but no frequency - CoreWLAN being the one that matters.
    /// </summary>
    public static int? ToFrequency(WifiBand band, int channel)
    {
        if (band == WifiBand.TwoPointFourGhz && channel == 14)
            return Channel14Frequency;

        if (band == WifiBand.SixGhz && channel == 2)
            return SixGhzChannel2Frequency;

        return band switch
        {
            WifiBand.TwoPointFourGhz => 2407 + (channel * 5),
            WifiBand.FiveGhz => 5000 + (channel * 5),
            WifiBand.SixGhz => 5950 + (channel * 5),
            _ => null
        };
    }


    /// <summary>
    /// Converts an RSSI in dBm to a 0-100 bar-style percentage.
    /// </summary>
    /// <remarks>
    /// Android exposes <c>WifiManager.CalculateSignalLevel</c> and WinRT exposes SignalBars, but
    /// neither CoreWLAN nor NetworkManager offers anything comparable, so the same clamp is applied
    /// everywhere for a consistent number. -100 dBm is unusable, -50 dBm is excellent; the range
    /// between is linear, which is not physically true but is what every Wi-Fi picker shows.
    /// </remarks>
    public static int ToPercent(int rssiDbm) => rssiDbm switch
    {
        <= -100 => 0,
        >= -50 => 100,
        _ => 2 * (rssiDbm + 100)
    };
}
