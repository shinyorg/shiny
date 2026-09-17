using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.BluetoothLE;
using Xunit;

namespace Shiny.BluetoothLE.Common.Tests;


/// <summary>
/// The hello and accept frames are parsed from bytes an unauthenticated peer wrote, so every way of getting
/// them wrong is pinned here.
/// </summary>
public class L2CapTicketsTests
{
    [Fact]
    public void HelloRoundTrips()
    {
        var token = L2CapTickets.CreateToken();
        var frame = L2CapTickets.CreateHello(token);

        Assert.Equal(L2CapTickets.HelloLength, frame.Length);
        Assert.Equal(L2CapTicketStatus.Accepted, L2CapTickets.ReadHello(frame, out var parsed));
        Assert.Equal(token, parsed);
    }


    [Fact]
    public void AcceptRoundTripsEveryStatus()
    {
        foreach (var status in Enum.GetValues<L2CapTicketStatus>())
        {
            var frame = L2CapTickets.CreateAccept(status);
            Assert.Equal(L2CapTickets.AcceptLength, frame.Length);
            Assert.Equal(status, L2CapTickets.ReadAccept(frame));
        }
    }


    [Fact]
    public void TokensAreDistinctAndTheRightLength()
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 1000; i++)
        {
            var token = L2CapTickets.CreateToken();
            Assert.Equal(L2CapTickets.TokenLength, token.Length);
            Assert.True(tokens.Add(token), "CreateToken produced a duplicate");
        }
    }


    [Fact]
    public void AShortFrameIsMalformed()
    {
        var frame = L2CapTickets.CreateHello(L2CapTickets.CreateToken());
        Assert.Equal(L2CapTicketStatus.Malformed, L2CapTickets.ReadHello(frame.AsSpan(0, frame.Length - 1), out _));
    }


    [Fact]
    public void WrongMagicIsMalformed()
    {
        var frame = L2CapTickets.CreateHello(L2CapTickets.CreateToken());
        frame[0] = (byte)'X';
        Assert.Equal(L2CapTicketStatus.Malformed, L2CapTickets.ReadHello(frame, out _));
    }


    [Fact]
    public void AWrongVersionIsReportedAsItself()
    {
        // not folded into Malformed: it is the one failure a user can act on, by updating
        var frame = L2CapTickets.CreateHello(L2CapTickets.CreateToken());
        frame[4] = 0xFE;

        Assert.Equal(L2CapTicketStatus.VersionMismatch, L2CapTickets.ReadHello(frame, out _));
        Assert.Equal(L2CapTicketStatus.VersionMismatch, L2CapTickets.ReadAccept(frame));
    }


    [Theory]
    [InlineData(0x00)]
    [InlineData(0x0A)]
    [InlineData(0x20)]
    [InlineData(0x7F)]
    [InlineData(0xFF)]
    public void NonPrintableTokenBytesAreRejected(byte value)
    {
        var frame = L2CapTickets.CreateHello(L2CapTickets.CreateToken());
        frame[10] = value;
        Assert.Equal(L2CapTicketStatus.Malformed, L2CapTickets.ReadHello(frame, out _));
    }


    [Fact]
    public void AnUnknownAcceptStatusIsMalformed()
    {
        var frame = L2CapTickets.CreateAccept(L2CapTicketStatus.Accepted);
        frame[5] = 0x7E;
        Assert.Equal(L2CapTicketStatus.Malformed, L2CapTickets.ReadAccept(frame));
    }


    [Fact]
    public void HelloRejectsAWrongLengthToken()
        => Assert.Throws<ArgumentException>(() => L2CapTickets.CreateHello("tooshort"));
}


public class L2CapChannelStreamTests
{
#pragma warning disable CA2022

    static (L2CapChannel Channel, Subject<byte[]> Incoming, List<byte[]> Written) Create()
    {
        var incoming = new Subject<byte[]>();
        var written = new List<byte[]>();
        var channel = new L2CapChannel(0x80, "peer", data => { written.Add(data); return Observable.Return(Unit.Default); }, incoming);
        return (channel, incoming, written);
    }


    [Fact]
    public async Task ReadsAcrossTheBuffersTheLinkDelivered()
    {
        var (channel, incoming, _) = Create();
        await using var stream = channel.AsStream();

        incoming.OnNext(new byte[] { 1, 2, 3 });
        incoming.OnNext(new byte[] { 4, 5 });
        incoming.OnNext(new byte[] { 6 });
        incoming.OnCompleted();

        var buffer = new byte[6];
        await stream.ReadExactlyAsync(buffer);

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, buffer);
        Assert.Equal(0, await stream.ReadAsync(new byte[1]));
        Assert.Equal(6, stream.BytesRead);
    }


    [Fact]
    public async Task BufferedBytesAreDeliveredBeforeAFaultSurfaces()
    {
        var (channel, incoming, _) = Create();
        await using var stream = channel.AsStream();

        incoming.OnNext(new byte[] { 9, 9 });
        incoming.OnError(new IOException("the link dropped"));

        var buffer = new byte[2];
        await stream.ReadExactlyAsync(buffer);
        Assert.Equal(new byte[] { 9, 9 }, buffer);

        var error = await Assert.ThrowsAsync<IOException>(async () => await stream.ReadAsync(new byte[1]));
        Assert.Equal("the link dropped", error.Message);
    }


    [Fact]
    public async Task WritesAreSplitToTheMaximumWriteSize_WhichCanChange()
    {
        var (channel, _, written) = Create();
        await using var stream = channel.AsStream(maxWriteSize: 64);

        await stream.WriteAsync(new byte[150]);
        Assert.Equal(new[] { 64, 64, 22 }, written.Select(x => x.Length));

        written.Clear();
        stream.MaxWriteSize = 100;
        await stream.WriteAsync(new byte[250]);
        Assert.Equal(new[] { 100, 100, 50 }, written.Select(x => x.Length));
        Assert.Equal(400, stream.BytesWritten);
    }


    [Fact]
    public void SynchronousReadsAndWritesAreRefused()
    {
        var (channel, _, _) = Create();
        using var stream = channel.AsStream();

        Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.False(stream.CanSeek);
    }


    [Fact]
    public void DisposingClosesTheChannelOnce_UnlessLeftOpen()
    {
        var closes = 0;
        var channel = new L2CapChannel(0x80, "peer", _ => Observable.Return(Unit.Default), Observable.Never<byte[]>(), () => closes++);

        var stream = channel.AsStream();
        stream.Dispose();
        stream.Dispose();
        Assert.Equal(1, closes);

        var other = new L2CapChannel(0x80, "peer", _ => Observable.Return(Unit.Default), Observable.Never<byte[]>(), () => closes++);
        other.AsStream(leaveOpen: true).Dispose();
        Assert.Equal(1, closes);
    }

#pragma warning restore CA2022
}
