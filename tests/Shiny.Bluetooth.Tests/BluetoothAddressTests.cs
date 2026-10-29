using Xunit;
namespace Shiny.Bluetooth.Tests;


public class BluetoothAddressTests
{
    [Theory]
    [InlineData("AA:BB:CC:DD:EE:FF")]
    [InlineData("aa:bb:cc:dd:ee:ff")]
    [InlineData("aa-bb-cc-dd-ee-ff")]
    [InlineData("AABBCCDDEEFF")]
    [InlineData("(aa:bb:cc:dd:ee:ff)")]
    public void Normalize_AcceptsEveryPlatformForm(string input)
        => Assert.Equal("AA:BB:CC:DD:EE:FF", BluetoothAddress.Normalize(input));


    [Theory]
    [InlineData("")]
    [InlineData("AA:BB:CC:DD:EE")]
    [InlineData("AA:BB:CC:DD:EE:FF:00")]
    [InlineData("GG:BB:CC:DD:EE:FF")]
    [InlineData("AA.BB.CC.DD.EE.FF")]
    public void Normalize_RejectsInvalid(string input)
        => Assert.Throws<ArgumentException>(() => BluetoothAddress.Normalize(input));


    [Fact]
    public void UInt64_RoundTrips()
    {
        var value = BluetoothAddress.ToUInt64("00:1A:7D:DA:71:13");
        Assert.Equal(0x001A7DDA7113UL, value);
        Assert.Equal("00:1A:7D:DA:71:13", BluetoothAddress.FromUInt64(value));
    }


    [Fact]
    public void ToDashed_IsIOBluetoothForm()
        => Assert.Equal("00-1a-7d-da-71-13", BluetoothAddress.ToDashed("00:1A:7D:DA:71:13"));
}
