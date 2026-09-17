using System.Text;
using Shiny.BluetoothLE;
using Xunit;

namespace Shiny.BluetoothLE.Common.Tests;

/// <summary>
/// Tests for <see cref="BleMessageFraming"/> and <see cref="BleMessageReassembler"/>.
/// </summary>
/// <remarks>
/// The format only runs for real across a GATT link, so its correctness has to come from here. A framing bug
/// shows up in the field as a request that never gets an answer and no error anywhere.
/// </remarks>
public class BleMessageFramingTests
{
    // IPeripheral.Mtu on the smallest link: a 23-byte ATT MTU less its 3-byte header
    const int MinimumMtu = BleMessageFraming.MinimumPayload;

    [Fact]
    public void Encode_SmallPayload_ProducesOneFragmentMarkedStartAndEnd()
    {
        var payload = Encoding.UTF8.GetBytes("hi");

        var fragments = BleMessageFraming.Encode(payload, mtu: 247);

        Assert.Single(fragments);
        Assert.Equal(0xC0, fragments[0][0] & 0xC0); // START | END
        Assert.Equal(payload, fragments[0][1..]);
    }

    [Fact]
    public void Encode_EmptyPayload_StillProducesOneCompleteFragment()
    {
        // A zero-length message is legal - a command with no arguments - and the receiver must still
        // see a complete message rather than nothing at all.
        var fragments = BleMessageFraming.Encode([], mtu: 247);

        Assert.Single(fragments);
        Assert.Single(fragments[0]);
    }

    [Fact]
    public void RoundTrip_LargePayload_AtMinimumMtu()
    {
        var payload = Encoding.UTF8.GetBytes(new string('x', 4096));

        var fragments = BleMessageFraming.Encode(payload, MinimumMtu);
        Assert.True(fragments.Count > 1, "a 4 KB payload must fragment at the minimum MTU");

        var reassembler = new BleMessageReassembler();
        byte[]? message = null;

        for (var i = 0; i < fragments.Count; i++)
        {
            var result = reassembler.Push(fragments[i], out message);
            var expected = i == fragments.Count - 1 ? BleMessageFrameResult.Complete : BleMessageFrameResult.Partial;
            Assert.Equal(expected, result);
        }

        Assert.Equal(payload, message);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(61)]
    [InlineData(182)]
    [InlineData(244)]
    [InlineData(514)]
    public void RoundTrip_AcrossRealisticMtus(int mtu)
    {
        // The negotiated MTU varies by central: 23 is the BLE floor, 185 is what iOS
        // typically lands on, 247 is common on Android, 517 is the ATT maximum.
        var payload = new byte[1500];
        Random.Shared.NextBytes(payload);

        var fragments = BleMessageFraming.Encode(payload, mtu);

        // Every fragment must fit inside the MTU minus ATT's own 3-byte overhead, or the
        // controller silently truncates it.
        Assert.All(fragments, f => Assert.True(
            f.Length <= mtu,
            $"fragment of {f.Length} bytes exceeds the usable payload for MTU {mtu}"
        ));

        var reassembler = new BleMessageReassembler();
        byte[]? message = null;
        foreach (var fragment in fragments)
            reassembler.Push(fragment, out message);

        Assert.Equal(payload, message);
    }

    [Fact]
    public void RoundTrip_PayloadLongerThanTheSequenceSpace()
    {
        // The sequence counter is 6 bits, so it wraps every 64 fragments. At the minimum MTU
        // that is roughly 1.2 KB - well inside the size of a WiFi scan result, which is
        // the kind of message most likely to hit this.
        var payload = new byte[8000];
        Random.Shared.NextBytes(payload);

        var fragments = BleMessageFraming.Encode(payload, MinimumMtu);
        Assert.True(fragments.Count > 64, "this test needs the sequence counter to wrap");

        var reassembler = new BleMessageReassembler();
        byte[]? message = null;
        foreach (var fragment in fragments)
            reassembler.Push(fragment, out message);

        Assert.Equal(payload, message);
    }

    [Fact]
    public void Push_DroppedFragment_IsDetectedRatherThanReassembledIntoGarbage()
    {
        var payload = new byte[512];
        Random.Shared.NextBytes(payload);

        var fragments = BleMessageFraming.Encode(payload, MinimumMtu);
        var reassembler = new BleMessageReassembler();

        Assert.Equal(BleMessageFrameResult.Partial, reassembler.Push(fragments[0], out _));

        // Skip fragment 1 - the case a flaky link actually produces.
        var result = reassembler.Push(fragments[2], out var message);

        Assert.Equal(BleMessageFrameResult.OutOfSequence, result);
        Assert.Null(message);
    }

    [Fact]
    public void Push_ContinuationWithoutStart_IsRejected()
    {
        var reassembler = new BleMessageReassembler();

        // Sequence 1 with neither START nor END - what a sender that reconnected mid-message would send.
        var result = reassembler.Push([0x01, 0xAA], out var message);

        Assert.Equal(BleMessageFrameResult.OutOfSequence, result);
        Assert.Null(message);
    }

    [Fact]
    public void Push_EmptyFragment_IsRejectedAsMalformed()
    {
        var reassembler = new BleMessageReassembler();

        Assert.Equal(BleMessageFrameResult.Malformed, reassembler.Push([], out var message));
        Assert.Null(message);
    }

    [Fact]
    public void Push_RestartedMessage_DiscardsThePartialAndSucceeds()
    {
        var first = BleMessageFraming.Encode(new byte[300], MinimumMtu);
        var second = BleMessageFraming.Encode(Encoding.UTF8.GetBytes("retry"), MinimumMtu);

        var reassembler = new BleMessageReassembler();
        reassembler.Push(first[0], out _);
        reassembler.Push(first[1], out _);

        // A fresh START mid-message means the sender gave up and started over; the partial
        // must be discarded rather than prefixed onto the new message.
        var result = reassembler.Push(second[0], out var message);

        Assert.Equal(BleMessageFrameResult.Complete, result);
        Assert.Equal("retry", Encoding.UTF8.GetString(message!));
    }

    [Fact]
    public void Push_MessageExceedingTheCap_IsRejected()
    {
        // The cap is what stops an unauthenticated central from streaming fragments forever to
        // exhaust the host's memory.
        var reassembler = new BleMessageReassembler(maxMessageBytes: 128);
        var fragments = BleMessageFraming.Encode(new byte[4096], MinimumMtu);

        var sawTooLarge = false;
        foreach (var fragment in fragments)
        {
            if (reassembler.Push(fragment, out _) == BleMessageFrameResult.TooLarge)
            {
                sawTooLarge = true;
                break;
            }
        }

        Assert.True(sawTooLarge, "the reassembler must refuse a message larger than its cap");
    }

    [Theory]
    [InlineData(20, 19)]
    [InlineData(182, 181)]
    [InlineData(10, 19)]
    public void PayloadPerFragment_SubtractsOnlyTheHeaderAndNeverDropsBelowTheMinimum(int mtu, int expected)
        => Assert.Equal(expected, BleMessageFraming.PayloadPerFragment(mtu));

    [Fact]
    public void Reassembler_IsPerLink_SoTwoCentralsDoNotInterleave()
    {
        // Hosts hold one reassembler per central. This asserts the property that makes that
        // necessary: two independent streams reassemble correctly in parallel.
        var alpha = Encoding.UTF8.GetBytes(new string('a', 200));
        var beta = Encoding.UTF8.GetBytes(new string('b', 200));

        var alphaFragments = BleMessageFraming.Encode(alpha, MinimumMtu);
        var betaFragments = BleMessageFraming.Encode(beta, MinimumMtu);

        var alphaReassembler = new BleMessageReassembler();
        var betaReassembler = new BleMessageReassembler();

        byte[]? alphaMessage = null;
        byte[]? betaMessage = null;

        for (var i = 0; i < Math.Max(alphaFragments.Count, betaFragments.Count); i++)
        {
            if (i < alphaFragments.Count)
                alphaReassembler.Push(alphaFragments[i], out alphaMessage);
            if (i < betaFragments.Count)
                betaReassembler.Push(betaFragments[i], out betaMessage);
        }

        Assert.Equal(alpha, alphaMessage);
        Assert.Equal(beta, betaMessage);
    }
}
