using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Nfc.Infrastructure;
using Xunit;

namespace Shiny.Nfc.Tests;


/// <summary>
/// The PC/SC scan loop over <see cref="FakePcscApi"/>: readers coming and going, tags placed and
/// lifted, the service not being up yet, and the session ending cleanly. The tag protocols
/// themselves are covered by the Type 2 / Type 4 tests.
/// </summary>
public class PcscManagerTests
{
    const string Reader = "ACS ACR122U PICC Interface";
    static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);


    static PcscNfcManager Create(FakePcscApi api) => new(api, NullLogger.Instance);


    [Fact]
    public async Task A_tag_placed_on_the_reader_is_scanned_and_can_be_written()
    {
        var api = new FakePcscApi();
        api.PlugIn(Reader);
        var sim = SimulatedType2Tag.Ntag213();
        api.Place(Reader, sim, SimulatedType2Tag.UltralightAtr);

        var message = new NdefMessage(NdefRecord.CreateUri("https://shinylib.net"));
        using var cts = new CancellationTokenSource(Wait);
        await foreach (var tag in Create(api).Scan(ct: cts.Token))
        {
            Assert.Equal(SimulatedType2Tag.Uid, tag.Identifier);
            Assert.Null(await tag.ReadNdef(cts.Token));

            await tag.WriteNdef(message, cts.Token);
            Assert.Equal(message, await tag.ReadNdef(cts.Token));
            break;
        }

        Assert.Equal(1, api.Cancels);
        Assert.Equal(1, api.Releases);
        Assert.NotEmpty(api.Disconnects);
    }


    [Fact]
    public async Task A_lifted_tag_is_lost_and_putting_it_back_scans_it_again()
    {
        var api = new FakePcscApi();
        api.PlugIn(Reader);
        var sim = SimulatedType2Tag.Ntag213();
        api.Place(Reader, sim, SimulatedType2Tag.UltralightAtr);

        using var cts = new CancellationTokenSource(Wait);
        var seen = 0;
        await foreach (var tag in Create(api).Scan(ct: cts.Token))
        {
            seen++;
            if (seen == 2)
                break;

            // a blank tag's read is answered from what was read at connect, so lose it with I/O
            api.Remove(Reader);
            await Assert.ThrowsAsync<NfcTagLostException>(() => tag.WriteNdef(new NdefMessage(NdefRecord.CreateText("gone")), cts.Token));

            // let the scan see the removal before the tag goes back, or it never saw it leave
            await WaitFor(() => api.OpenCards(Reader) == 0, cts.Token);
            api.Place(Reader, sim, SimulatedType2Tag.UltralightAtr);
        }

        Assert.Equal(2, seen);
    }


    [Fact]
    public async Task A_reader_plugged_in_mid_scan_is_picked_up()
    {
        var api = new FakePcscApi();
        using var cts = new CancellationTokenSource(Wait);

        var scan = Create(api).Scan(ct: cts.Token).GetAsyncEnumerator(cts.Token);
        var next = scan.MoveNextAsync();

        await Task.Delay(200, cts.Token);
        api.PlugIn(Reader);
        api.Place(Reader, SimulatedType2Tag.Ntag215(), SimulatedType2Tag.UltralightAtr);

        Assert.True(await next);
        Assert.Equal(SimulatedType2Tag.Uid, scan.Current.Identifier);
        await scan.DisposeAsync();
    }


    [Fact]
    public async Task Waits_for_the_pcsc_service_instead_of_failing()
    {
        var api = new FakePcscApi { EstablishFailures = 2 };
        api.PlugIn(Reader);
        api.Place(Reader, SimulatedType2Tag.Ntag213(), SimulatedType2Tag.UltralightAtr);

        using var cts = new CancellationTokenSource(Wait);
        var result = await Create(api).ReadOne(ct: cts.Token);

        Assert.NotNull(result);
        Assert.Equal(SimulatedType2Tag.Uid, result.Identifier);
        Assert.Equal(0, api.EstablishFailures);
    }


    [Fact]
    public async Task An_iso_dep_tag_is_opened_as_type_4()
    {
        var api = new FakePcscApi();
        api.PlugIn(Reader);
        var sim = new SimulatedType4Tag();
        api.Place(Reader, sim, SimulatedType4Tag.IsoDepAtr);

        var message = new NdefMessage(NdefRecord.CreateText("type four"));
        using var cts = new CancellationTokenSource(Wait);
        Assert.True(await Create(api).WriteOne(message, ct: cts.Token));

        var read = await Create(api).ReadOne(ct: cts.Token);
        Assert.Equal(message, read?.Message);
    }


    [Fact]
    public async Task Access_reflects_whether_a_reader_is_connected()
    {
        var api = new FakePcscApi();
        Assert.Equal(AccessState.NotSetup, await Create(api).RequestAccess());

        api.PlugIn(Reader);
        Assert.Equal(AccessState.Available, await Create(api).RequestAccess());

        Assert.Equal(AccessState.NotSetup, await Create(new FakePcscApi { EstablishFailures = 1 }).RequestAccess());

        var none = new PcscNfcManager(null, NullLogger.Instance);
        Assert.Equal(AccessState.NotSupported, await none.RequestAccess());
        Assert.Equal(NfcCapabilities.None, none.Capabilities);
    }


    const string ReaderA = "Reader A";
    const string ReaderB = "Reader B";


    // an NTAG213 already holding a text record, so a test can tell which reader a tag came from
    static SimulatedType2Tag Labelled(string label)
    {
        var ndef = new NdefMessage(NdefRecord.CreateText(label)).ToByteArray();
        return SimulatedType2Tag.Custom([0xE1, 0x10, 0x12, 0x00], [0x03, (byte)ndef.Length, .. ndef, 0xFE]);
    }


    static async Task<string?> Label(INfcTag tag, CancellationToken ct)
    {
        var message = await tag.ReadNdef(ct);
        return message != null && message.Records[0].TryGetText(out var text) ? text.Text : null;
    }


    static async Task WaitFor(Func<bool> condition, CancellationToken ct)
    {
        while (!condition())
            await Task.Delay(20, ct);
    }


    // A is read and held by a slow consumer, B's tag waits in the slot, then A is re-presented and
    // its new tag pushes B's out of the slot while B's card stays on the reader
    static async Task<IAsyncEnumerator<INfcTag>> DropReaderBsTag(FakePcscApi api, CancellationToken ct)
    {
        api.PlugIn(ReaderA);
        api.PlugIn(ReaderB);

        var scan = Create(api).Scan(ct: ct).GetAsyncEnumerator(ct);
        var next = scan.MoveNextAsync();
        api.Place(ReaderA, Labelled("A"), SimulatedType2Tag.UltralightAtr);
        Assert.True(await next);
        Assert.Equal("A", await Label(scan.Current, ct));

        api.Place(ReaderB, Labelled("B"), SimulatedType2Tag.UltralightAtr);
        await WaitFor(() => api.ConnectCount(ReaderB) == 1, ct);

        api.Remove(ReaderA);
        await WaitFor(() => api.OpenCards(ReaderA) == 0, ct);
        api.Place(ReaderA, Labelled("A"), SimulatedType2Tag.UltralightAtr);

        // B's card handle is closed when its tag is dropped
        await WaitFor(() => api.ConnectCount(ReaderA) == 2 && api.OpenCards(ReaderB) == 0, ct);
        return scan;
    }


    [Fact]
    public async Task A_dropped_tag_waits_for_room_and_is_delivered_again_without_evicting()
    {
        var api = new FakePcscApi();
        using var cts = new CancellationTokenSource(Wait);
        var scan = await DropReaderBsTag(api, cts.Token);

        // the slot is full with A's tag: B must not reconnect, and must not push A's tag out
        await Task.Delay(1500, cts.Token);
        Assert.Equal(1, api.ConnectCount(ReaderB));
        Assert.Equal(2, api.ConnectCount(ReaderA));

        Assert.True(await scan.MoveNextAsync());
        Assert.Equal("A", await Label(scan.Current, cts.Token));

        // room now - B's tag, still on the reader, comes back
        Assert.True(await scan.MoveNextAsync());
        Assert.Equal("B", await Label(scan.Current, cts.Token));
        Assert.Equal(2, api.ConnectCount(ReaderB));

        // a slow consumer holding B's tag causes no further reconnects on either reader
        await Task.Delay(1000, cts.Token);
        Assert.Equal(2, api.ConnectCount(ReaderB));
        Assert.Equal(2, api.ConnectCount(ReaderA));

        await scan.DisposeAsync();
    }


    [Fact]
    public async Task A_dropped_tag_removed_from_its_reader_goes_back_to_normal()
    {
        var api = new FakePcscApi();
        using var cts = new CancellationTokenSource(Wait);
        var scan = await DropReaderBsTag(api, cts.Token);

        api.Remove(ReaderB);
        await Task.Delay(500, cts.Token);

        Assert.True(await scan.MoveNextAsync());
        Assert.Equal("A", await Label(scan.Current, cts.Token));

        // the card left while dropped, so there is nothing to offer again
        await Task.Delay(1000, cts.Token);
        Assert.Equal(1, api.ConnectCount(ReaderB));

        // and putting it back is an ordinary tap
        var next = scan.MoveNextAsync();
        api.Place(ReaderB, Labelled("B2"), SimulatedType2Tag.UltralightAtr);
        Assert.True(await next);
        Assert.Equal("B2", await Label(scan.Current, cts.Token));
        Assert.Equal(2, api.ConnectCount(ReaderB));

        await scan.DisposeAsync();
    }


    [Fact]
    public async Task A_single_readers_dropped_tag_is_delivered_again()
    {
        var api = new FakePcscApi();
        api.PlugIn(Reader);
        api.Place(Reader, Labelled("only"), SimulatedType2Tag.UltralightAtr);

        using var cts = new CancellationTokenSource(Wait);
        var scan = Create(api).Scan(ct: cts.Token).GetAsyncEnumerator(cts.Token);
        Assert.True(await scan.MoveNextAsync());
        var first = scan.Current;

        // what the scan queue does to a tag it drops
        ((IDroppableNfcTag)first).OnDropped();
        Assert.Equal(0, api.OpenCards(Reader));
        await Assert.ThrowsAsync<NfcTagLostException>(() => first.WriteNdef(new NdefMessage(NdefRecord.CreateText("x")), cts.Token));

        Assert.True(await scan.MoveNextAsync());
        Assert.NotSame(first, scan.Current);
        Assert.Equal("only", await Label(scan.Current, cts.Token));
        Assert.Equal(2, api.ConnectCount(Reader));

        await scan.DisposeAsync();
    }
}
