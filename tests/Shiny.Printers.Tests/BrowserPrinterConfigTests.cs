using Shiny.Printers;
using Shiny.Printers.Blazor;
using Shiny.Printers.Protocols.Tspl;

namespace Shiny.Printers.Tests;


/// <summary>
/// The browser transports do their real work in JavaScript; the only C# logic is projecting the public
/// configs onto the flat DTOs handed across the interop boundary. These lock that projection down.
/// </summary>
public class BrowserPrinterConfigTests
{
    [Fact]
    public void Bluetooth_Defaults_MatchKnownProfiles()
    {
        var js = new WebBluetoothPrinterConfig().ToJs();

        Assert.Equal(KnownWebPrinterProfiles.All.Count, js.Profiles.Count);
        Assert.Equal(KnownWebPrinterProfiles.All[0].Name, js.Profiles[0].Name);
        Assert.Equal(KnownWebPrinterProfiles.All[0].ServiceUuid, js.Profiles[0].Service);
        Assert.Equal(KnownWebPrinterProfiles.All[0].WriteCharacteristicUuid, js.Profiles[0].Write);
        Assert.True(js.WriteWithoutResponse);
        Assert.False(js.AcceptAllDevices);
        Assert.Equal(180, js.ChunkSize);
        Assert.Equal(20, js.InterChunkDelayMs);
    }


    [Fact]
    public void Bluetooth_ProjectsProfilesInOrder_AndAllowsNullWriteCharacteristic()
    {
        var config = new WebBluetoothPrinterConfig
        {
            Profiles = [new("First", "aaaa"), new("Second", "bbbb", "cccc")],
            AcceptAllDevices = true,
            WriteWithoutResponse = false,
            ChunkSize = 20
        };

        var js = config.ToJs();

        Assert.Collection(
            js.Profiles,
            p => { Assert.Equal("First", p.Name); Assert.Equal("aaaa", p.Service); Assert.Null(p.Write); },
            p => { Assert.Equal("Second", p.Name); Assert.Equal("bbbb", p.Service); Assert.Equal("cccc", p.Write); }
        );
        Assert.True(js.AcceptAllDevices);
        Assert.False(js.WriteWithoutResponse);
        Assert.Equal(20, js.ChunkSize);
    }


    [Fact]
    public void Bluetooth_WithNoProfiles_Throws()
        => Assert.Throws<ArgumentException>(() => new WebBluetoothPrinterConfig { Profiles = [] }.ToJs());


    [Fact]
    public void Serial_Defaults_Are9600_8N1()
    {
        var js = new WebSerialPrinterConfig().ToJs();

        Assert.Equal(9600, js.BaudRate);
        Assert.Equal(8, js.DataBits);
        Assert.Equal(1, js.StopBits);
        Assert.Equal("none", js.Parity);
        Assert.Equal("none", js.FlowControl);
        Assert.Equal(0, js.BufferSize);
        Assert.Empty(js.UsbVendorIds);
    }


    [Theory]
    [InlineData(WebSerialParity.None, "none")]
    [InlineData(WebSerialParity.Even, "even")]
    [InlineData(WebSerialParity.Odd, "odd")]
    public void Serial_ParityMapsToWebSerialValue(WebSerialParity parity, string expected)
        => Assert.Equal(expected, new WebSerialPrinterConfig { Parity = parity }.ToJs().Parity);


    [Theory]
    [InlineData(WebSerialFlowControl.None, "none")]
    [InlineData(WebSerialFlowControl.Hardware, "hardware")]
    public void Serial_FlowControlMapsToWebSerialValue(WebSerialFlowControl flow, string expected)
        => Assert.Equal(expected, new WebSerialPrinterConfig { FlowControl = flow }.ToJs().FlowControl);


    [Fact]
    public void Usb_DefaultsToPrinterClassFilter()
    {
        var js = new WebUsbPrinterConfig().ToJs();

        var filter = Assert.Single(js.Filters);
        Assert.Equal(7, filter.ClassCode);
        Assert.Null(filter.VendorId);
        Assert.Null(filter.ProductId);
        Assert.Equal(4096, js.ChunkSize);
    }


    [Fact]
    public void Usb_ProjectsVendorAndProductFilters()
    {
        var js = new WebUsbPrinterConfig { Filters = [new(VendorId: 0x0416, ProductId: 0x5011)] }.ToJs();

        var filter = Assert.Single(js.Filters);
        Assert.Equal(0x0416, filter.VendorId);
        Assert.Equal(0x5011, filter.ProductId);
        Assert.Null(filter.ClassCode);
    }


    [Fact]
    public void InterChunkDelay_ConvertsToWholeMilliseconds_AndNeverGoesNegative()
    {
        Assert.Equal(250, new WebUsbPrinterConfig { InterChunkDelay = TimeSpan.FromMilliseconds(250) }.ToJs().InterChunkDelayMs);
        Assert.Equal(0, new WebUsbPrinterConfig { InterChunkDelay = TimeSpan.Zero }.ToJs().InterChunkDelayMs);
        Assert.Equal(0, new WebUsbPrinterConfig { InterChunkDelay = TimeSpan.FromMilliseconds(-5) }.ToJs().InterChunkDelayMs);
    }


    [Fact]
    public void Config_CarriesCapabilitiesAndProtocol_WhichNeverCrossTheInteropBoundary()
    {
        // Capabilities default to 58mm and the protocol to ESC/POS, matching the BLE + TCP transports.
        var defaults = new WebSerialPrinterConfig();
        Assert.Same(PrinterCapabilities.Paper58mm, defaults.Capabilities);
        Assert.IsType<Protocols.EscPos.EscPosProtocol>(defaults.ProtocolFactory());

        // ...and both are overridable, e.g. an 80mm label printer speaking TSPL.
        var custom = new WebSerialPrinterConfig
        {
            Capabilities = PrinterCapabilities.Paper80mm,
            ProtocolFactory = static () => new TsplProtocol()
        };
        Assert.Equal(48, custom.Capabilities.CharactersPerLine);
        Assert.IsType<TsplProtocol>(custom.ProtocolFactory());
    }
}
