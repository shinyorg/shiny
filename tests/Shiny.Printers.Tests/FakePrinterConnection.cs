using System.Collections.Generic;
using System.Reactive.Linq;
using Shiny.Printers;

namespace Shiny.Printers.Tests;


/// <summary>Captures everything written so a full document can be round-tripped without hardware.</summary>
public sealed class FakePrinterConnection : IPrinterConnection
{
    readonly List<byte> captured = new();

    public bool IsConnected { get; set; } = true;

    public byte[] Written => this.captured.ToArray();

    public IObservable<PrinterConnectionState> WhenStatusChanged()
        => Observable.Return(PrinterConnectionState.Connected);

    public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        this.captured.AddRange(data.ToArray());
        return ValueTask.CompletedTask;
    }
}
