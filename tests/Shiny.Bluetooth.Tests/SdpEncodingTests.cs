using System.Text;
using Xunit;

namespace Shiny.Bluetooth.Tests;


public class SdpEncodingTests
{
    [Fact]
    public void Text_IsTextElementWithByteLength()
        => Assert.Equal(new byte[] { 0x25, 0x03, (byte)'S', (byte)'P', (byte)'P' }, SdpEncoding.Text("SPP"));


    [Fact]
    public void Text_LengthCountsUtf8Bytes()
    {
        var encoded = SdpEncoding.Text("é");
        Assert.Equal(2, encoded[1]);
        Assert.Equal("é", Encoding.UTF8.GetString(encoded, 2, encoded[1]));
    }


    [Fact]
    public void Text_TruncatesOnACharacterBoundary()
    {
        // 254 ASCII bytes then a 2-byte character straddling the 255-byte limit
        var encoded = SdpEncoding.Text(new string('a', 254) + "é");
        Assert.Equal(254, encoded[1]);
        Assert.Equal(256, encoded.Length);
    }
}
