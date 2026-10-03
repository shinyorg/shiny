using System.Net;
using System.Net.Sockets;
using Shiny.Printers;
using Shiny.Printers.Network;
using Shiny.Printers.Protocols.EscPos;

namespace Shiny.Printers.Tests;


public class TcpPrinterConnectionTests
{
    [Fact]
    public async Task Streams_Bytes_Verbatim_Over_Tcp()
    {
        using var listener = StartListener(out var port);
        var serverReceived = ReadAllFromFirstClient(listener);

        using var conn = new TcpPrinterConnection("127.0.0.1", port);
        await conn.Connect();
        Assert.True(conn.IsConnected);

        var payload = new byte[] { 0x1B, 0x40, (byte)'H', (byte)'i', 0x0A, 0x0A };
        await conn.SendAsync(payload);
        conn.Disconnect();

        Assert.Equal(payload, await serverReceived);
        Assert.False(conn.IsConnected);
    }


    [Fact]
    public async Task Print_Document_End_To_End_Over_Tcp()
    {
        using var listener = StartListener(out var port);
        var serverReceived = ReadAllFromFirstClient(listener);

        using var conn = new TcpPrinterConnection("127.0.0.1", port);
        await conn.Connect();
        var printer = new Printer(conn, new EscPosProtocol(), PrinterCapabilities.Paper80mm);

        await printer.Print(new Document.PrintDocument().Line("HELLO NET"));
        conn.Disconnect();

        var bytes = await serverReceived;
        Assert.NotEmpty(bytes);
        // ESC/POS initialises each document with ESC @ (0x1B 0x40).
        Assert.Equal(0x1B, bytes[0]);
        Assert.Equal(0x40, bytes[1]);
    }


    [Fact]
    public async Task Manager_Connects_And_Reports_Connected()
    {
        using var listener = StartListener(out var port);
        var accept = listener.AcceptTcpClientAsync();

        var manager = new NetworkPrinterManager();
        var printer = await manager.Connect("127.0.0.1", port, PrinterCapabilities.Paper80mm);

        Assert.True(printer.IsConnected);
        Assert.Equal(80, printer.Capabilities.PaperWidthMm);

        using var serverSide = await accept;
    }


    [Fact]
    public async Task SendAsync_Throws_When_Not_Connected()
    {
        using var conn = new TcpPrinterConnection("127.0.0.1", 9100);
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await conn.SendAsync(new byte[] { 1, 2, 3 })
        );
    }


    [Fact]
    public async Task Connect_Times_Out_To_A_Dead_Host()
    {
        // 203.0.113.0/24 (TEST-NET-3) is reserved and unroutable - the connect never completes.
        using var conn = new TcpPrinterConnection("203.0.113.1", 9100, TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAsync<TimeoutException>(async () => await conn.Connect());
        Assert.False(conn.IsConnected);
    }


    static TcpListener StartListener(out int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return listener;
    }


    static async Task<byte[]> ReadAllFromFirstClient(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms); // completes when the printer side shuts the socket down
        return ms.ToArray();
    }
}
