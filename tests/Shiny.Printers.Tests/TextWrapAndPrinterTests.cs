using System.Linq;
using Shiny.Printers;
using Shiny.Printers.Document;
using Shiny.Printers.Protocols.EscPos;

namespace Shiny.Printers.Tests;


public class TextWrapTests
{
    [Fact]
    public void Wraps_On_Word_Boundaries()
        => Assert.Equal(["hello", "world"], TextWrap.Wrap("hello world", 5).ToArray());

    [Fact]
    public void Keeps_Short_Text_On_One_Line()
        => Assert.Equal(["a b c"], TextWrap.Wrap("a b c", 10).ToArray());

    [Fact]
    public void Hard_Splits_Words_Longer_Than_Width()
        => Assert.Equal(["abc", "def", "gh"], TextWrap.Wrap("abcdefgh", 3).ToArray());

    [Fact]
    public void Honours_Existing_Newlines()
        => Assert.Equal(["a", "b"], TextWrap.Wrap("a\nb", 10).ToArray());

    [Fact]
    public void Respects_CharactersPerLine_Capability()
    {
        var caps = PrinterCapabilities.Paper58mm; // 32 cols
        var lines = TextWrap.Wrap(new string('x', 70), caps.CharactersPerLine).ToArray();
        Assert.All(lines, l => Assert.True(l.Length <= caps.CharactersPerLine));
        Assert.Equal(70, lines.Sum(l => l.Length));
    }
}


public class PrinterRoundTripTests
{
    [Fact]
    public async Task Print_Sends_Encoded_Bytes_To_Connection()
    {
        var connection = new FakePrinterConnection();
        var protocol = new EscPosProtocol();
        var caps = PrinterCapabilities.Paper80mm;
        var printer = new Printer(connection, protocol, caps);

        var doc = new PrintDocument()
            .AlignCenter()
            .Bold()
            .Line("RECEIPT")
            .Cut();

        await printer.Print(doc);

        Assert.Equal(protocol.Encode(doc, caps), connection.Written);
        Assert.Same(caps, printer.Capabilities);
    }

    [Fact]
    public async Task PrintRaw_Forwards_Bytes()
    {
        var connection = new FakePrinterConnection();
        var printer = new Printer(connection, new EscPosProtocol(), PrinterCapabilities.Paper58mm);

        await printer.PrintRaw(new byte[] { 1, 2, 3 });

        Assert.Equal([1, 2, 3], connection.Written);
    }

    [Fact]
    public void Dispose_Disposes_Disposable_Connection()
    {
        var connection = new DisposableConnection();
        var printer = new Printer(connection, new EscPosProtocol(), PrinterCapabilities.Paper58mm);

        Assert.Same(connection, printer.Connection);
        printer.Dispose();

        Assert.True(connection.Disposed);
    }

    [Fact]
    public void Dispose_Tolerates_NonDisposable_Connection()
    {
        var printer = new Printer(new FakePrinterConnection(), new EscPosProtocol(), PrinterCapabilities.Paper58mm);
        printer.Dispose();
    }

    sealed class DisposableConnection : IPrinterConnection, IDisposable
    {
        public bool Disposed { get; private set; }
        public bool IsConnected => !this.Disposed;
        public IObservable<PrinterConnectionState> WhenStatusChanged() => System.Reactive.Linq.Observable.Never<PrinterConnectionState>();
        public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public void Dispose() => this.Disposed = true;
    }
}
