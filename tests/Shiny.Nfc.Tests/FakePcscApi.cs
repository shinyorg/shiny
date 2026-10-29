using Shiny.Nfc.Infrastructure.Tags;
using Shiny.Nfc.Pcsc;

namespace Shiny.Nfc.Tests;


/// <summary>
/// PC/SC with readers the test plugs in and tags it places on them. <c>SCardGetStatusChange</c>
/// blocks until a reader's state differs from what the caller passed, the timeout passes, or
/// <c>SCardCancel</c> - the same contract the scan loop relies on against the real service.
/// </summary>
class FakePcscApi : IPcscApi
{
    readonly object sync = new();
    readonly Dictionary<string, (IApduChannel Tag, byte[] Atr)?> readers = new();
    readonly Dictionary<nint, string> cards = new();
    SemaphoreSlim changed = new(0);
    nint nextCard = 100;

    public int EstablishFailures { get; set; }
    public int Cancels { get; private set; }
    public int Releases { get; private set; }
    public List<nint> Disconnects { get; } = new();


    public void PlugIn(string reader)
    {
        lock (this.sync)
            this.readers[reader] = null;
        this.changed.Release();
    }


    public void Unplug(string reader)
    {
        lock (this.sync)
            this.readers.Remove(reader);
        this.changed.Release();
    }


    public void Place(string reader, IApduChannel tag, byte[] atr)
    {
        lock (this.sync)
            this.readers[reader] = (tag, atr);
        this.changed.Release();
    }


    public void Remove(string reader)
    {
        lock (this.sync)
            this.readers[reader] = null;
        this.changed.Release();
    }


    public nint EstablishContext()
    {
        if (this.EstablishFailures > 0)
        {
            this.EstablishFailures--;
            throw new PcscException("SCardEstablishContext", PcscError.NoService);
        }
        return 1;
    }

    public void ReleaseContext(nint context) => this.Releases++;

    public void Cancel(nint context)
    {
        this.Cancels++;
        this.changed.Release();
    }


    public string[] ListReaders(nint context)
    {
        lock (this.sync)
            return this.readers.Keys.ToArray();
    }


    public uint GetStatusChange(nint context, ReaderState[] states, uint timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            lock (this.sync)
            {
                var any = false;
                foreach (var state in states)
                {
                    if (!this.readers.TryGetValue(state.Reader, out var slot))
                        return PcscError.UnknownReader;

                    var actual = slot == null ? PcscState.Empty : PcscState.Present;
                    if (actual != state.CurrentState)
                    {
                        state.EventState = actual | PcscState.Changed;
                        state.Atr = slot?.Atr ?? [];
                        any = true;
                    }
                    else
                    {
                        state.EventState = actual;
                    }
                }
                if (any)
                    return PcscError.Success;
            }

            var left = deadline - DateTime.UtcNow;
            if (left <= TimeSpan.Zero || !this.changed.Wait(left))
                return PcscError.Timeout;

            if (this.Cancels > 0)
                return PcscError.Cancelled;
        }
    }


    public (nint Card, uint Protocol) Connect(nint context, string reader)
    {
        lock (this.sync)
        {
            if (this.readers.GetValueOrDefault(reader) == null)
                throw new PcscException("SCardConnect", PcscError.NoSmartcard);

            var card = this.nextCard++;
            this.cards[card] = reader;
            return (card, PcscConstants.ProtocolT1);
        }
    }


    public void Disconnect(nint card)
    {
        lock (this.sync)
        {
            this.cards.Remove(card);
            this.Disconnects.Add(card);
        }
    }


    public byte[] Transmit(nint card, uint protocol, ReadOnlySpan<byte> command)
    {
        IApduChannel tag;
        lock (this.sync)
        {
            if (!this.cards.TryGetValue(card, out var reader))
                throw new PcscException("SCardTransmit", PcscError.InvalidHandle);

            if (this.readers.GetValueOrDefault(reader) is not { } slot)
                throw new PcscException("SCardTransmit", PcscError.RemovedCard);

            tag = slot.Tag;
        }

        // the simulated tags answer synchronously
        var response = tag.Transmit(command.ToArray(), CancellationToken.None);
        return response.IsCompletedSuccessfully ? response.Result : throw new InvalidOperationException("Simulated tags answer synchronously");
    }
}
