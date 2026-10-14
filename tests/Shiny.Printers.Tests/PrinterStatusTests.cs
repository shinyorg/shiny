using System.Net;
using System.Net.Sockets;
using Shiny.Printers.Network;
using Shiny.Printers.Protocols.EscPos;

namespace Shiny.Printers.Tests;


public class PrinterStatusTests
{
    // bits 1 and 4 are always set on an ESC/POS status byte
    const byte Idle = 0x12;


    [Fact]
    public void Parse_Idle_Printer_Can_Print()
    {
        var status = EscPosStatus.Parse([Idle, Idle, Idle, Idle]);

        Assert.True(status.IsOnline);
        Assert.True(status.CanPrint);
        Assert.False(status.HasError);
        Assert.False(status.IsDrawerSensorHigh);
    }


    [Fact]
    public void Parse_Cover_Open_And_Paper_Out()
    {
        var status = EscPosStatus.Parse([
            Idle | 0x08,          // offline
            Idle | 0x04 | 0x20,   // cover open, stopped for paper end
            Idle,
            Idle | 0x0C | 0x60    // near end + end sensors
        ]);

        Assert.False(status.IsOnline);
        Assert.True(status.IsCoverOpen);
        Assert.True(status.IsPaperOut);
        Assert.True(status.IsPaperNearEnd);
        Assert.False(status.CanPrint);
    }


    [Fact]
    public void Parse_Errors_And_Drawer()
    {
        var status = EscPosStatus.Parse([Idle | 0x04, Idle, Idle | 0x08 | 0x20 | 0x40, Idle]);

        Assert.True(status.IsDrawerSensorHigh);
        Assert.True(status.IsCutterError);
        Assert.True(status.HasUnrecoverableError);
        Assert.True(status.HasAutoRecoverableError);
        Assert.True(status.HasError);
        Assert.False(status.CanPrint);
    }


    [Fact]
    public void Parse_Rejects_Non_Status_Bytes()
    {
        Assert.Throws<InvalidDataException>(() => EscPosStatus.Parse([(byte)'O', (byte)'K', Idle, Idle]));
        Assert.Throws<InvalidDataException>(() => EscPosStatus.Parse([Idle, Idle]));
    }


    [Fact]
    public async Task GetStatus_Over_Tcp()
    {
        using var listener = StartListener(out var port);
        var server = ServeStatus(listener, [Idle, Idle | 0x04, Idle, Idle | 0x0C], staleBytes: [0xFF]);

        var printer = await new NetworkPrinterManager().Connect("127.0.0.1", port, PrinterCapabilities.Paper80mm);
        Assert.True(printer.SupportsStatus);

        // let the stale byte arrive so the query has to discard it
        await Task.Delay(100);
        var status = await printer.GetStatus();

        Assert.True(status.IsCoverOpen);
        Assert.True(status.IsPaperNearEnd);
        Assert.False(status.CanPrint);
        Assert.Equal(EscPosStatus.Request, await server);
        ((IDisposable)printer).Dispose();
    }


    [Fact]
    public async Task GetStatus_Times_Out_When_Printer_Is_Silent()
    {
        using var listener = StartListener(out var port);
        var accept = listener.AcceptTcpClientAsync();

        using var conn = new TcpPrinterConnection("127.0.0.1", port);
        await conn.Connect();
        var printer = new Printer(conn, new EscPosProtocol(), PrinterCapabilities.Paper80mm)
        {
            StatusTimeout = TimeSpan.FromMilliseconds(200)
        };

        await Assert.ThrowsAsync<TimeoutException>(() => printer.GetStatus());
        using var serverSide = await accept;
    }


    [Fact]
    public async Task GetStatus_Not_Supported_On_Write_Only_Transport()
    {
        IPrinter printer = new Printer(new FakePrinterConnection(), new EscPosProtocol(), PrinterCapabilities.Paper58mm);

        Assert.False(printer.SupportsStatus);
        await Assert.ThrowsAsync<NotSupportedException>(() => printer.GetStatus());
    }


    static TcpListener StartListener(out int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return listener;
    }


    // a fake ESC/POS printer: optionally sends junk first, then answers one 12-byte status request
    static async Task<byte[]> ServeStatus(TcpListener listener, byte[] reply, byte[] staleBytes)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        await stream.WriteAsync(staleBytes);

        var request = new byte[EscPosStatus.Request.Length];
        await stream.ReadExactlyAsync(request);
        await stream.WriteAsync(reply);
        return request;
    }
}
