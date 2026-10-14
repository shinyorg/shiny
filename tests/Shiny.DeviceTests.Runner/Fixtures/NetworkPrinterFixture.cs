using System.Net;
using System.Net.Sockets;

namespace Shiny.DeviceTests.Runner.Fixtures;


/// <summary>
/// A fake RAW / JetDirect receipt printer on TCP 9100. It records every byte it is sent and answers ESC/POS
/// real-time status queries (DLE EOT n) with a configurable reply, so the NetworkPrinters suite can assert on
/// exactly what Shiny.Printers.Network put on the wire.
///
///   GET    /env/printer                         PrinterCapture - connection count + base64 of every byte received
///   DELETE /env/printer                         clears the capture and restores the "all clear" status reply
///   POST   /env/printer/status?reply=12121272   the four DLE EOT reply bytes (n = 1..4) as hex
/// </summary>
public static class NetworkPrinterFixture
{
    public static void MapNetworkPrinter(this WebApplication app, RunnerState state)
    {
        var printer = new FakePrinter(DeviceTestConstants.PrinterPort);
        app.Lifetime.ApplicationStarted.Register(printer.Start);
        app.Lifetime.ApplicationStopping.Register(printer.Stop);

        app.MapGet("/env/printer", () => printer.IsListening
            ? Results.Json(printer.GetCapture(), DeviceTestJsonContext.Default.PrinterCapture)
            : Results.Problem($"The fake printer could not listen on {DeviceTestConstants.PrinterPort}", statusCode: 503)
        );

        app.MapDelete("/env/printer", () =>
        {
            printer.Reset();
            return Results.Ok();
        });

        app.MapPost("/env/printer/status", (string reply) =>
        {
            if (reply.Length != 8 || !reply.All(Char.IsAsciiHexDigit))
                return Results.BadRequest("reply must be 4 hex bytes, e.g. 12121272");

            printer.StatusReply = Convert.FromHexString(reply);
            return Results.Ok();
        });
    }


    sealed class FakePrinter(int port)
    {
        // DLE EOT n replies for n = 1..4: bits 1 and 4 set is the "nothing to report" ESC/POS status byte
        static readonly byte[] AllClear = [0x12, 0x12, 0x12, 0x12];

        readonly object sync = new();
        readonly List<byte> received = [];
        readonly CancellationTokenSource cts = new();
        TcpListener? listener;
        int connections;

        public byte[] StatusReply { get; set; } = AllClear;
        public bool IsListening => this.listener != null;


        public void Start()
        {
            try
            {
                var l = new TcpListener(IPAddress.Any, port);
                l.Start();
                this.listener = l;
                _ = this.AcceptLoop(l, this.cts.Token);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"  ! fake printer could not listen on {port}: {ex.Message} - NetworkPrinters tests will fail");
            }
        }


        public void Stop()
        {
            this.cts.Cancel();
            this.listener?.Stop();
        }


        public PrinterCapture GetCapture()
        {
            lock (this.sync)
                return new PrinterCapture(this.connections, Convert.ToBase64String(this.received.ToArray()));
        }


        public void Reset()
        {
            lock (this.sync)
            {
                this.received.Clear();
                this.connections = 0;
            }
            this.StatusReply = AllClear;
        }


        async Task AcceptLoop(TcpListener l, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await l.AcceptTcpClientAsync(ct);
                    lock (this.sync)
                        this.connections++;

                    _ = this.Receive(client, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
                {
                    return;
                }
            }
        }


        async Task Receive(TcpClient client, CancellationToken ct)
        {
            using var _ = client;
            var stream = client.GetStream();
            var buffer = new byte[4096];

            // the last two bytes seen, so a DLE EOT n split across reads is still recognised
            byte prev2 = 0, prev1 = 0;
            try
            {
                var read = await stream.ReadAsync(buffer, ct);
                while (read > 0)
                {
                    List<byte> replies = [];
                    lock (this.sync)
                    {
                        for (var i = 0; i < read; i++)
                        {
                            var b = buffer[i];
                            this.received.Add(b);
                            if (prev2 == 0x10 && prev1 == 0x04 && b is >= 1 and <= 4)
                                replies.Add(this.StatusReply[b - 1]);

                            prev2 = prev1;
                            prev1 = b;
                        }
                    }
                    if (replies.Count > 0)
                        await stream.WriteAsync(replies.ToArray(), ct);

                    read = await stream.ReadAsync(buffer, ct);
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException)
            {
                // the app hung up mid-job or the runner is stopping - nothing to report
            }
        }
    }
}
