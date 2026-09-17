using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shiny.BluetoothLE;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE.Hosting;
using Xunit;

namespace Shiny.BluetoothLE.Common.Tests;


/// <summary>
/// The ticket broker and the central's claim, driven end to end over in-memory channels.
/// </summary>
public class L2CapTicketBrokerTests : IDisposable
{
    static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    readonly ListeningHostingManager hosting = new();
    readonly L2CapTicketBroker broker;
    readonly List<ClosingLoopback> links = new();


    public L2CapTicketBrokerTests()
        => this.broker = new L2CapTicketBroker(this.hosting, new L2CapTicketBrokerOptions { HandshakeTimeout = TimeSpan.FromSeconds(5) });


    public void Dispose()
    {
        this.broker.Dispose();
        foreach (var link in this.links)
            link.Dispose();
    }


    ClosingLoopback Connect(int? fragmentSize = null)
    {
        var link = new ClosingLoopback(fragmentSize);
        this.links.Add(link);
        this.hosting.Connect(link.Responder);
        return link;
    }


    [Fact]
    public async Task AClaimedTicketHandsBothEndsAStream()
    {
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ticket = await this.broker.Reserve("echo", TimeSpan.FromMinutes(1), async (stream, ct) =>
        {
            var buffer = new byte[5];
            await stream.ReadExactlyAsync(buffer, ct);
            received.TrySetResult(buffer);
            await stream.WriteAsync("world"u8.ToArray(), ct);
        });

        Assert.Equal(ListeningHostingManager.AssignedPsm, ticket.Psm);
        Assert.Equal(L2CapTickets.TokenLength, ticket.Token.Length);
        Assert.True(this.hosting.Secure);

        var link = this.Connect(fragmentSize: 7);
        await using var client = await link.Initiator.ClaimTicket(ticket.Token);

        await client.WriteAsync("hello"u8.ToArray());
        Assert.Equal("hello"u8.ToArray(), await received.Task.WaitAsync(Wait));

        var reply = new byte[5];
        await client.ReadExactlyAsync(reply).AsTask().WaitAsync(Wait);
        Assert.Equal("world"u8.ToArray(), reply);

        // the handler returning closes the channel, which is how the central learns the transfer ended
        Assert.Equal(0, await client.ReadAsync(new byte[1]).AsTask().WaitAsync(Wait));
    }


    [Fact]
    public async Task AnUnknownTokenIsRefused()
    {
        await this.broker.Reserve("something", TimeSpan.FromMinutes(1), (_, _) => Task.CompletedTask);

        var link = this.Connect();
        var ex = await Assert.ThrowsAsync<L2CapTicketException>(() => link.Initiator.ClaimTicket(L2CapTickets.CreateToken()));
        Assert.Equal(L2CapTicketStatus.UnknownTicket, ex.Status);
    }


    [Fact]
    public async Task ATicketIsSingleUse()
    {
        var release = new TaskCompletionSource();
        var ticket = await this.broker.Reserve("held", TimeSpan.FromMinutes(1), (_, _) => release.Task);

        await using var first = await this.Connect().Initiator.ClaimTicket(ticket.Token);

        var ex = await Assert.ThrowsAsync<L2CapTicketException>(() => this.Connect().Initiator.ClaimTicket(ticket.Token));
        Assert.Equal(L2CapTicketStatus.AlreadyClaimed, ex.Status);

        release.SetResult();
    }


    [Fact]
    public async Task AnExpiredTicketIsRefused()
    {
        var ran = false;
        var ticket = await this.broker.Reserve("stale", TimeSpan.FromMilliseconds(50), (_, _) => { ran = true; return Task.CompletedTask; });
        await Task.Delay(150);

        var ex = await Assert.ThrowsAsync<L2CapTicketException>(() => this.Connect().Initiator.ClaimTicket(ticket.Token));
        Assert.Equal(L2CapTicketStatus.UnknownTicket, ex.Status);
        Assert.False(ran);
        Assert.Equal(0, this.broker.PendingTickets);
    }


    [Fact]
    public async Task GarbageIsAnsweredAsMalformedAndNeverReachesAHandler()
    {
        var ran = false;
        await this.broker.Reserve("guarded", TimeSpan.FromMinutes(1), (_, _) => { ran = true; return Task.CompletedTask; });

        var link = this.Connect();
        await using var raw = link.Initiator.AsStream();
        await raw.WriteAsync(new byte[L2CapTickets.HelloLength]);

        var accept = new byte[L2CapTickets.AcceptLength];
        await raw.ReadExactlyAsync(accept).AsTask().WaitAsync(Wait);

        Assert.Equal(L2CapTicketStatus.Malformed, L2CapTickets.ReadAccept(accept));
        Assert.Equal(0, await raw.ReadAsync(new byte[1]).AsTask().WaitAsync(Wait));
        Assert.False(ran);
    }


    [Fact]
    public async Task ReleasingAClaimedTicketCancelsItsHandler()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var ticket = await this.broker.Reserve("long", TimeSpan.FromMinutes(1), async (_, ct) =>
        {
            started.SetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                cancelled.SetResult();
                throw;
            }
        });

        await using var client = await this.Connect().Initiator.ClaimTicket(ticket.Token);
        await started.Task.WaitAsync(Wait);

        this.broker.Release(ticket.Token);

        await cancelled.Task.WaitAsync(Wait);
        Assert.Equal(0, this.broker.PendingTickets);
    }


    [Fact]
    public async Task EveryReservationSharesOneListener()
    {
        var a = await this.broker.Reserve("a", TimeSpan.FromMinutes(1), (_, _) => Task.CompletedTask);
        var b = await this.broker.Reserve("b", TimeSpan.FromMinutes(1), (_, _) => Task.CompletedTask);

        Assert.Equal(1, this.hosting.OpenCount);
        Assert.Equal(a.Psm, b.Psm);
        Assert.NotEqual(a.Token, b.Token);
        Assert.Equal(2, this.broker.PendingTickets);
    }


    [Fact]
    public async Task TheReservationsWriteSizeAppliesToItsStream()
    {
        var sizes = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ticket = await this.broker.Reserve("sized", TimeSpan.FromMinutes(1), (stream, _) =>
        {
            sizes.SetResult(stream.MaxWriteSize);
            return Task.CompletedTask;
        }, maxWriteSize: 512);

        await using var client = await this.Connect().Initiator.ClaimTicket(ticket.Token);
        Assert.Equal(512, await sizes.Task.WaitAsync(Wait));
        Assert.Equal(512, ticket.MaxWriteSize);
    }


    [Fact]
    public async Task DisposingTheBrokerClosesTheListener()
    {
        await this.broker.Reserve("x", TimeSpan.FromMinutes(1), (_, _) => Task.CompletedTask);
        this.broker.Dispose();

        Assert.True(this.hosting.IsClosed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => this.broker.Reserve("y", TimeSpan.FromMinutes(1), (_, _) => Task.CompletedTask));
    }


    /// <summary>
    /// Two channel ends wired together in memory, where closing one end ends the other's reads - as closing a real
    /// L2CAP socket does, and as the broker relies on to tell a central its transfer finished.
    /// </summary>
    sealed class ClosingLoopback : IDisposable
    {
        readonly ReplaySubject<byte[]> toInitiator = new();
        readonly ReplaySubject<byte[]> toResponder = new();

        public ClosingLoopback(int? fragmentSize)
        {
            this.Initiator = new L2CapChannel(0x81, "responder", d => Send(this.toResponder, d, fragmentSize), this.toInitiator, () => this.toResponder.OnCompleted());
            this.Responder = new L2CapChannel(0x81, "initiator", d => Send(this.toInitiator, d, fragmentSize), this.toResponder, () => this.toInitiator.OnCompleted());
        }

        public L2CapChannel Initiator { get; }
        public L2CapChannel Responder { get; }

        static IObservable<Unit> Send(ISubject<byte[]> target, byte[] data, int? fragmentSize) => Observable.Defer(() =>
        {
            var size = fragmentSize ?? data.Length;
            for (var i = 0; i < data.Length; i += Math.Max(1, size))
                target.OnNext(data.AsSpan(i, Math.Min(size, data.Length - i)).ToArray());

            return Observable.Return(Unit.Default);
        });

        public void Dispose()
        {
            this.toInitiator.Dispose();
            this.toResponder.Dispose();
        }
    }


    /// <summary>Hands the broker each channel a test connects, as the stack does when a central opens one.</summary>
    sealed class ListeningHostingManager : IBleHostingManager
    {
        public const ushort AssignedPsm = 0x0081;
        Action<L2CapChannel>? onOpen;

        public int OpenCount { get; private set; }
        public bool Secure { get; private set; }
        public bool IsClosed { get; private set; }

        public void Connect(L2CapChannel channel)
            => (this.onOpen ?? throw new InvalidOperationException("No listener is open")).Invoke(channel);

        public Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
        {
            this.OpenCount++;
            this.Secure = secure;
            this.onOpen = onOpen;
            return Task.FromResult(new L2CapInstance(AssignedPsm, () => this.IsClosed = true));
        }

        public Task<AccessState> RequestAccess(bool advertise = true, bool connect = true) => Task.FromResult(AccessState.Available);
        public AccessState AdvertisingAccessStatus => AccessState.Available;
        public AccessState GattAccessStatus => AccessState.Available;
        public bool IsAdvertising => false;
        public IReadOnlyList<IGattService> Services => Array.Empty<IGattService>();
        public Task StartAdvertising(AdvertisementOptions? options = null) => throw new NotSupportedException();
        public void StopAdvertising() => throw new NotSupportedException();
        public Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null) => throw new NotSupportedException();
        public Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder) => throw new NotSupportedException();
        public void RemoveService(string serviceUuid) => throw new NotSupportedException();
        public void ClearServices() => throw new NotSupportedException();
    }
}
