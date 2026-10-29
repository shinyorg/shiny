using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Nfc.Infrastructure.Tags;
using Xunit;

namespace Shiny.Nfc.Tests;


public class Type2TagTests
{
    static Task<ApduNfcTag> Connect(SimulatedType2Tag sim)
        => ApduNfcTag.Connect(sim, SimulatedType2Tag.UltralightAtr, NullLogger.Instance, CancellationToken.None);


    [Fact]
    public async Task BlankNtag213_HasNoMessage_AndReportsCapacity()
    {
        var tag = await Connect(SimulatedType2Tag.Ntag213());

        Assert.Null(await tag.ReadNdef());
        Assert.True(tag.IsWritable);
        Assert.Equal(SimulatedType2Tag.Uid, tag.Identifier);

        // 144-byte data area, NDEF TLV after the 5-byte lock control TLV, 2-byte TLV header
        Assert.Equal(137, tag.MaxSize);
    }


    [Fact]
    public async Task Write_ThenReadFromAFreshConnection()
    {
        var sim = SimulatedType2Tag.Ntag213();
        var message = new NdefMessage(NdefRecord.CreateUri("https://shinylib.net"), NdefRecord.CreateText("hello"));

        await (await Connect(sim)).WriteNdef(message);
        var read = await (await Connect(sim)).ReadNdef();

        Assert.Equal(message, read);

        // lock control TLV untouched, NDEF TLV follows it, terminator after the message
        var encoded = message.ToByteArray();
        var data = sim.DataArea(5 + 2 + encoded.Length + 1);
        Assert.Equal(new byte[] { 0x01, 0x03, 0xA0, 0x0C, 0x34, 0x03, (byte)encoded.Length }, data[..7]);
        Assert.Equal(encoded, data[7..^1]);
        Assert.Equal(0xFE, data[^1]);
    }


    [Fact]
    public async Task Write_SetsLengthLast_SoATornWriteReadsAsEmpty()
    {
        var sim = SimulatedType2Tag.Ntag213();
        var message = new NdefMessage(NdefRecord.CreateUri("https://shinylib.net/a/long/enough/path"));

        await (await Connect(sim)).WriteNdef(message);

        // the NDEF TLV header sits at data offset 5-6, which is page 5
        var first = sim.Writes.First();
        var last = sim.Writes.Last();
        Assert.Equal(5, first.Page);
        Assert.Equal(new byte[] { 0x34, 0x03, 0x00 }, first.Data[..3]);
        Assert.Equal(5, last.Page);
        Assert.Equal(new byte[] { 0x34, 0x03, (byte)message.Length }, last.Data[..3]);
    }


    [Fact]
    public async Task LargeMessage_UsesThreeByteTlvLength()
    {
        var sim = SimulatedType2Tag.Ntag215();
        var tag = await Connect(sim);
        Assert.Equal(492, tag.MaxSize);

        var message = new NdefMessage(NdefRecord.CreateMime("application/octet-stream", new byte[300]));
        await tag.WriteNdef(message);

        Assert.Equal(new byte[] { 0x03, 0xFF, 0x00, 0x00 }, sim.Writes.First().Data);
        Assert.Equal(new byte[] { 0x03, 0xFF, (byte)(message.Length >> 8), (byte)message.Length }, sim.Page(4));
        Assert.Equal(message, await (await Connect(sim)).ReadNdef());
    }


    [Fact]
    public async Task Rewrite_ShorterMessageReplacesLonger()
    {
        var sim = SimulatedType2Tag.Ntag213();
        var tag = await Connect(sim);

        await tag.WriteNdef(new NdefMessage(NdefRecord.CreateText(new string('x', 100))));
        var shorter = new NdefMessage(NdefRecord.CreateText("y"));
        await tag.WriteNdef(shorter);

        Assert.Equal(shorter, await tag.ReadNdef());
        Assert.Equal(shorter, await (await Connect(sim)).ReadNdef());
    }


    [Fact]
    public async Task TooLarge_ThrowsAndWritesNothing()
    {
        var sim = SimulatedType2Tag.Ntag213();
        var tag = await Connect(sim);

        var ex = await Assert.ThrowsAsync<NdefMessageTooLargeException>(
            () => tag.WriteNdef(new NdefMessage(NdefRecord.CreateText(new string('x', 200))))
        );
        Assert.Equal(137, ex.MaxSize);
        Assert.Empty(sim.Writes);
    }


    [Fact]
    public async Task ReadOnlyCapabilityContainer_RefusesWrites()
    {
        var sim = SimulatedType2Tag.Custom([0xE1, 0x10, 0x12, 0x0F], [0x03, 0x00, 0xFE]);
        var tag = await Connect(sim);

        Assert.False(tag.IsWritable);
        await Assert.ThrowsAsync<NfcTagReadOnlyException>(() => tag.WriteNdef(NdefMessage.Empty));
        Assert.Empty(sim.Writes);
    }


    [Fact]
    public async Task NotNdefFormatted_HasNoMessageAndCannotBeWritten()
    {
        var sim = SimulatedType2Tag.Custom([0x00, 0x00, 0x00, 0x00], []);
        var tag = await Connect(sim);

        Assert.Null(await tag.ReadNdef());
        Assert.Null(tag.MaxSize);
        Assert.False(tag.IsWritable);
        await Assert.ThrowsAsync<NfcException>(() => tag.WriteNdef(NdefMessage.Empty));
    }


    [Fact]
    public async Task UnsupportedMappingVersion_IsTreatedAsUnformatted()
    {
        var tag = await Connect(SimulatedType2Tag.Custom([0xE1, 0x20, 0x12, 0x00], [0x03, 0x00, 0xFE]));
        Assert.Null(tag.MaxSize);
    }


    [Fact]
    public async Task NullTlvsBeforeTheMessage_AreSkipped()
    {
        var message = new NdefMessage(NdefRecord.CreateText("hi"));
        var encoded = message.ToByteArray();
        var sim = SimulatedType2Tag.Custom([0xE1, 0x10, 0x12, 0x00], [0x00, 0x00, 0x03, (byte)encoded.Length, .. encoded, 0xFE]);

        Assert.Equal(message, await (await Connect(sim)).ReadNdef());
    }


    [Fact]
    public async Task TlvLengthPastTheDataArea_IsAFormatError()
    {
        var sim = SimulatedType2Tag.Custom([0xE1, 0x10, 0x02, 0x00], [0x03, 0x40, 0xD1]);
        var tag = await Connect(sim);

        await Assert.ThrowsAsync<NdefFormatException>(() => tag.ReadNdef());
    }


    [Fact]
    public async Task MakeReadOnly_SetsCapabilityContainerAndEveryLockBit()
    {
        var sim = SimulatedType2Tag.Ntag213();
        var tag = await Connect(sim);
        await tag.WriteNdef(new NdefMessage(NdefRecord.CreateUri("https://shinylib.net")));

        await tag.MakeReadOnly();

        Assert.Equal(0x0F, sim.Page(3)[3]);
        Assert.Equal(new byte[] { 0xFF, 0xFF }, sim.Page(2)[2..]);
        // lock control TLV declares 12 dynamic lock bits at page 40
        Assert.Equal(new byte[] { 0xFF, 0x0F }, sim.Page(40)[..2]);

        var again = await Connect(sim);
        Assert.False(again.IsWritable);
        Assert.Equal("https://shinylib.net", (await again.ReadNdef())!.Records[0].TryGetUri(out var uri) ? uri : null);

        // the hardware now refuses writes everywhere in the data area
        var before = sim.Writes.Count;
        foreach (var page in new[] { 4, 15, 16, 39 })
        {
            var response = await sim.Transmit([0xFF, 0xD6, 0x00, (byte)page, 0x04, 1, 2, 3, 4], default);
            Assert.Equal(new byte[] { 0x63, 0x00 }, response);
        }
        Assert.Equal(before, sim.Writes.Count);
    }


    [Fact]
    public async Task MakeReadOnly_WithoutLockControlTlv_StillLocksStaticallyAndMarksReadOnly()
    {
        var sim = SimulatedType2Tag.Ntag215();
        var tag = await Connect(sim);

        await tag.MakeReadOnly();

        Assert.Equal(0x0F, sim.Page(3)[3]);
        Assert.Equal(new byte[] { 0xFF, 0xFF }, sim.Page(2)[2..]);
        Assert.False((await Connect(sim)).IsWritable);
    }


    [Fact]
    public async Task TagRemoved_SurfacesAsTagLost()
    {
        var sim = SimulatedType2Tag.Ntag213();
        var tag = await Connect(sim);
        sim.TagRemoved = true;

        await Assert.ThrowsAsync<NfcTagLostException>(() => tag.WriteNdef(NdefMessage.Empty));
    }
}
