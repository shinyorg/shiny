using Microsoft.Extensions.Logging.Abstractions;
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
}
