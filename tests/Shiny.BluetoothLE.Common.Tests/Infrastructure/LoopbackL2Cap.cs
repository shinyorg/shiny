using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE;

namespace Shiny.BluetoothLE.Common.Tests.Infrastructure;


/// <summary>
/// A pair of <see cref="L2CapChannel"/> objects wired back to back in memory, so the transfer
/// protocol can be exercised end to end without a radio.
/// </summary>
public sealed class LoopbackL2Cap : IDisposable
{
    readonly ReplaySubject<byte[]> toInitiator = new();
    readonly ReplaySubject<byte[]> toResponder = new();
    readonly int? fragmentSize;


    /// <param name="fragmentSize">
    /// When set, every write is delivered to the peer in chunks of this size - the real link
    /// fragments at the MTU, so the reader has to reassemble across chunk boundaries.
    /// </param>
    public LoopbackL2Cap(int? fragmentSize = null)
    {
        this.fragmentSize = fragmentSize;

        this.Initiator = new L2CapChannel(
            0x0080,
            "responder-peer",
            data => this.Send(this.toResponder, data),
            this.toInitiator
        );

        this.Responder = new L2CapChannel(
            0x0080,
            "initiator-peer",
            data => this.Send(this.toInitiator, data),
            this.toResponder
        );
    }


    /// <summary>The side that drives transfers (the central, in BLE terms).</summary>
    public L2CapChannel Initiator { get; }

    /// <summary>The side that serves transfers (the peripheral, in BLE terms).</summary>
    public L2CapChannel Responder { get; }

    /// <summary>Total bytes pushed across the link in both directions.</summary>
    public long BytesOnTheWire { get; private set; }


    /// <summary>Simulates the peer dropping the connection.</summary>
    public void CloseResponder() => this.toInitiator.OnCompleted();

    /// <summary>Simulates the initiator dropping the connection.</summary>
    public void CloseInitiator() => this.toResponder.OnCompleted();


    IObservable<Unit> Send(ISubject<byte[]> target, byte[] data) => Observable.Defer(() =>
    {
        this.BytesOnTheWire += data.Length;

        if (this.fragmentSize == null || data.Length <= this.fragmentSize.Value)
        {
            target.OnNext(data);
        }
        else
        {
            for (var i = 0; i < data.Length; i += this.fragmentSize.Value)
            {
                var len = Math.Min(this.fragmentSize.Value, data.Length - i);
                target.OnNext(data.AsSpan(i, len).ToArray());
            }
        }
        return Observable.Return(Unit.Default);
    });


    public void Dispose()
    {
        this.toInitiator.Dispose();
        this.toResponder.Dispose();
    }
}
