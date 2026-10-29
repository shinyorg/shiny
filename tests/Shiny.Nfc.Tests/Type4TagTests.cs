using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Nfc.Infrastructure.Tags;
using Xunit;

namespace Shiny.Nfc.Tests;


public class Type4TagTests
{
    static Task<ApduNfcTag> Connect(SimulatedType4Tag sim)
        => ApduNfcTag.Connect(sim, SimulatedType4Tag.IsoDepAtr, NullLogger.Instance, CancellationToken.None);


    [Fact]
    public async Task EmptyTag_HasNoMessage()
    {
        var tag = await Connect(new SimulatedType4Tag());

        Assert.Null(await tag.ReadNdef());
        Assert.True(tag.IsWritable);
        Assert.Equal(0x800 - 2, tag.MaxSize);
        Assert.Null(tag.Identifier);
    }


    [Fact]
    public async Task ExistingMessage_IsRead()
    {
        var sim = new SimulatedType4Tag { AnswersGetUid = true };
        var message = new NdefMessage(NdefRecord.CreateUri("https://shinylib.net"));
        sim.Store(message);

        var tag = await Connect(sim);
        Assert.Equal(message, await tag.ReadNdef());
        Assert.Equal(new byte[] { 0x08, 0x12, 0x34, 0x56 }, tag.Identifier);
    }


    [Fact]
    public async Task WriteAndRead_ChunkToTheTagsLimits()
    {
        var sim = new SimulatedType4Tag { MaxLe = 0x10, MaxLc = 0x0D };
        var message = new NdefMessage(NdefRecord.CreateMime("application/octet-stream", Enumerable.Range(0, 400).Select(x => (byte)x).ToArray()));

        await (await Connect(sim)).WriteNdef(message);

        Assert.All(sim.Updates, x => Assert.True(x.Data.Length <= 0x0D));
        Assert.Equal(message.ToByteArray(), sim.StoredMessage);
        Assert.Equal(message, await (await Connect(sim)).ReadNdef());
    }


    [Fact]
    public async Task Write_SetsLengthLast()
    {
        var sim = new SimulatedType4Tag();
        var message = new NdefMessage(NdefRecord.CreateText("hello"));

        await (await Connect(sim)).WriteNdef(message);

        Assert.Equal(0, sim.Updates[0].Offset);
        Assert.Equal(new byte[] { 0x00, 0x00 }, sim.Updates[0].Data);
        Assert.Equal(0, sim.Updates[^1].Offset);
        Assert.Equal(new byte[] { 0x00, (byte)message.Length }, sim.Updates[^1].Data);
    }


    [Fact]
    public async Task WriteProtectedFile_IsReadOnly()
    {
        var sim = new SimulatedType4Tag { WriteAccess = 0xFF };
        var tag = await Connect(sim);

        Assert.False(tag.IsWritable);
        await Assert.ThrowsAsync<NfcTagReadOnlyException>(() => tag.WriteNdef(NdefMessage.Empty));
        Assert.Empty(sim.Updates);
    }


    [Fact]
    public async Task Version1OnlyTag_IsStillRead()
    {
        var sim = new SimulatedType4Tag { AnswersV2 = false };
        var message = new NdefMessage(NdefRecord.CreateText("legacy"));
        sim.Store(message);

        Assert.Equal(message, await (await Connect(sim)).ReadNdef());
    }


    [Fact]
    public async Task NoNdefApplication_IsNotNdef()
    {
        var tag = await Connect(new SimulatedType4Tag { HasNdefApplication = false });

        Assert.Null(await tag.ReadNdef());
        Assert.False(tag.IsWritable);
        Assert.Null(tag.MaxSize);
        await Assert.ThrowsAsync<NfcNotSupportedException>(() => tag.WriteNdef(NdefMessage.Empty));
    }


    [Fact]
    public async Task TooLarge_ThrowsAndWritesNothing()
    {
        var sim = new SimulatedType4Tag { NdefFile = new byte[64] };
        var tag = await Connect(sim);

        var ex = await Assert.ThrowsAsync<NdefMessageTooLargeException>(
            () => tag.WriteNdef(new NdefMessage(NdefRecord.CreateText(new string('x', 100))))
        );
        Assert.Equal(62, ex.MaxSize);
        Assert.Empty(sim.Updates);
    }


    [Fact]
    public async Task MakeReadOnly_IsNotSupported()
    {
        var tag = await Connect(new SimulatedType4Tag());

        var ex = await Assert.ThrowsAsync<NfcNotSupportedException>(() => tag.MakeReadOnly());
        Assert.Equal(NfcCapabilities.MakeReadOnly, ex.Capability);
    }


    [Theory]
    [InlineData("3B8F8001804F0CA0000003060300030000000068", "Type2")]
    [InlineData("3B8F8001804F0CA00000030603003A0000000068", "Type2")]
    [InlineData("3B8F8001804F0CA0000003060300010000000068", "Unsupported")]
    [InlineData("3B8F8001804F0CA0000003060300020000000068", "Unsupported")]
    [InlineData("3B8180018080", "IsoDep")]
    [InlineData("3B8C80018057434F534E4C4F4D4E00", "IsoDep")]
    public void Atr_IsClassified(string atr, string expected)
        => Assert.Equal(expected, Atr.Classify(Convert.FromHexString(atr)).Family.ToString());
}
