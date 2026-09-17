using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// One L2CAP listener shared by every bulk transfer a device offers, with each channel claimed by a single-use
/// ticket.
/// </summary>
/// <remarks>
/// <para>Opening a PSM per transfer is tempting and wrong. PSMs come from a small dynamic range, and one that has
/// just been released can be handed straight back out - to another process - so a number given to a central can go
/// stale before it connects. The broker opens one listener on first use and keeps it.</para>
/// <para>The cost of sharing is that an arriving channel says nothing about what it wants. <see cref="Reserve"/>
/// answers that: it returns a ticket whose token the caller passes to the central over an authenticated route (a GATT
/// command, say) alongside <see cref="Psm"/>. The first channel to present that token (see <see cref="L2CapTickets"/>)
/// is handed to the reservation's handler as a stream. A wrong, expired or already-claimed token - or a channel that
/// says nothing at all - is answered and closed.</para>
/// </remarks>
public sealed class L2CapTicketBroker : IDisposable
{
    readonly IBleHostingManager hosting;
    readonly L2CapTicketBrokerOptions options;
    readonly ILogger logger;
    readonly ConcurrentDictionary<string, L2CapTicket> tickets = new(StringComparer.Ordinal);
    readonly SemaphoreSlim gate = new(1, 1);
    L2CapInstance? listener;
    int disposed;


    /// <param name="hosting">The hosting manager the listener is opened on.</param>
    /// <param name="options">Broker settings. Defaults apply when null.</param>
    /// <param name="logger">Optional logger.</param>
    public L2CapTicketBroker(IBleHostingManager hosting, L2CapTicketBrokerOptions? options = null, ILogger<L2CapTicketBroker>? logger = null)
    {
        this.hosting = hosting ?? throw new ArgumentNullException(nameof(hosting));
        this.options = options ?? new L2CapTicketBrokerOptions();
        this.logger = (ILogger?)logger ?? NullLogger.Instance;
    }


    /// <summary>Gets the PSM centrals connect to, or zero before the first <see cref="Reserve"/> opened the listener.</summary>
    public ushort Psm => this.listener?.Psm ?? 0;

    /// <summary>Gets the tickets issued and not yet finished, released or expired.</summary>
    public int PendingTickets => this.tickets.Count;


    /// <summary>
    /// Reserves a channel for one transfer and returns the ticket that claims it. Opens the listener on first use.
    /// </summary>
    /// <param name="label">What the transfer is, for logs.</param>
    /// <param name="lifetime">How long the token is accepted before it expires unused.</param>
    /// <param name="handler">
    /// Runs once a channel presents the token. The stream is closed when the handler returns, which is what tells the
    /// central the transfer ended. The token cancels when the ticket is released or the broker disposed.
    /// </param>
    /// <param name="maxWriteSize">The largest single write on the handler's stream. Defaults to <see cref="L2CapTicketBrokerOptions.MaxWriteSize"/>.</param>
    /// <param name="cancellationToken">Cancels opening the listener - not the transfer.</param>
    public async Task<L2CapTicket> Reserve(
        string label,
        TimeSpan lifetime,
        Func<L2CapChannelStream, CancellationToken, Task> handler,
        int? maxWriteSize = null,
        CancellationToken cancellationToken = default
    )
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime), "A ticket needs a positive lifetime");
        if (this.disposed == 1) throw new ObjectDisposedException(nameof(L2CapTicketBroker));

        await this.EnsureListener(cancellationToken).ConfigureAwait(false);

        var ticket = new L2CapTicket(
            L2CapTickets.CreateToken(),
            label ?? String.Empty,
            DateTimeOffset.UtcNow.Add(lifetime),
            this.Psm,
            maxWriteSize ?? this.options.MaxWriteSize,
            handler
        );
        this.tickets[ticket.Token] = ticket;
        this.PruneExpired();

        this.logger.LogDebug("Reserved an L2CAP channel for {Label} on PSM {Psm}", ticket.Label, ticket.Psm);
        return ticket;
    }


    /// <summary>
    /// Withdraws a ticket, and cancels its handler if a channel has already claimed it.
    /// </summary>
    /// <remarks>
    /// Both halves matter: withdrawing alone would leave a claimed transfer running after the central abandoned it,
    /// and cancelling alone would leave the token usable.
    /// </remarks>
    public void Release(string token)
    {
        if (token != null && this.tickets.TryRemove(token, out var ticket))
            ticket.Cancel();
    }


    async Task EnsureListener(CancellationToken cancellationToken)
    {
        if (this.listener != null)
            return;

        await this.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this.listener != null)
                return;

            var instance = await this.hosting.OpenL2Cap(this.options.Secure, this.OnChannelOpened).ConfigureAwait(false);
            this.listener = instance;
            this.logger.LogInformation("L2CAP ticket listener open on PSM {Psm}", instance.Psm);
        }
        finally
        {
            this.gate.Release();
        }
    }


    void OnChannelOpened(L2CapChannel channel)
    {
        // The stream subscribes to the channel here, synchronously, before anything is scheduled: a central writes its
        // hello the instant the channel opens, and data that arrives before a subscription exists is lost.
        var stream = new L2CapChannelStream(channel, this.options.MaxWriteSize);
        _ = Task.Run(() => this.Handshake(stream));
    }


    async Task Handshake(L2CapChannelStream stream)
    {
        var peer = stream.Channel.Identifier;
        L2CapTicket? ticket = null;

        try
        {
            using var timeout = new CancellationTokenSource(this.options.HandshakeTimeout);

            var hello = new byte[L2CapTickets.HelloLength];
            await stream.ReadExactlyAsync(hello, timeout.Token).ConfigureAwait(false);

            var status = L2CapTickets.ReadHello(hello, out var token);
            if (status == L2CapTicketStatus.Accepted)
                status = this.TryClaim(token, out ticket);

            await stream.WriteAsync(L2CapTickets.CreateAccept(status), timeout.Token).ConfigureAwait(false);

            if (status != L2CapTicketStatus.Accepted)
            {
                ticket = null;
                this.logger.LogWarning("Refused an L2CAP channel from {Peer}: {Status}", peer, status);
            }
        }
        catch (Exception ex)
        {
            // a channel that never completed the handshake is a stale central or a probe - it gets nothing
            ticket = null;
            this.logger.LogDebug(ex, "An L2CAP channel from {Peer} failed its ticket handshake", peer);
        }

        if (ticket == null)
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            return;
        }

        await using var owned = stream;
        owned.MaxWriteSize = ticket.MaxWriteSize;
        this.logger.LogInformation("L2CAP channel from {Peer} claimed for {Label}", peer, ticket.Label);

        try
        {
            await ticket.Handler(owned, ticket.CancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            this.logger.LogInformation("L2CAP transfer {Label} was cancelled", ticket.Label);
        }
        catch (Exception ex)
        {
            // the handler owns reporting its own failure; this only keeps it off the thread pool
            this.logger.LogWarning(ex, "L2CAP transfer {Label} ended in an error", ticket.Label);
        }
        finally
        {
            this.tickets.TryRemove(ticket.Token, out _);
            ticket.Dispose();
        }
    }


    L2CapTicketStatus TryClaim(string token, out L2CapTicket? ticket)
    {
        ticket = null;

        if (!this.tickets.TryGetValue(token, out var candidate))
            return L2CapTicketStatus.UnknownTicket;

        if (candidate.HasExpired && !candidate.IsClaimed)
        {
            if (this.tickets.TryRemove(token, out _))
                candidate.Dispose();

            return L2CapTicketStatus.UnknownTicket;
        }

        if (!candidate.TryClaim())
            return L2CapTicketStatus.AlreadyClaimed;

        ticket = candidate;
        return L2CapTicketStatus.Accepted;
    }


    void PruneExpired()
    {
        foreach (var pair in this.tickets)
        {
            if (!pair.Value.HasExpired || pair.Value.IsClaimed)
                continue;

            if (this.tickets.TryRemove(pair.Key, out var expired))
            {
                this.logger.LogDebug("L2CAP ticket for {Label} expired unused", expired.Label);
                expired.Dispose();
            }
        }
    }


    /// <summary>Cancels every ticket and closes the listener.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) == 1)
            return;

        foreach (var ticket in this.tickets.Values)
        {
            ticket.Cancel();
            ticket.Dispose();
        }
        this.tickets.Clear();
        this.listener?.Dispose();
        this.gate.Dispose();
    }
}


/// <summary>
/// Settings for <see cref="L2CapTicketBroker"/>.
/// </summary>
public class L2CapTicketBrokerOptions
{
    /// <summary>
    /// Requires an encrypted link for the listener. Defaults to true. A central must open its channel with the same
    /// setting, or the connect fails - better than an unencrypted transfer nobody noticed.
    /// </summary>
    public bool Secure { get; set; } = true;

    /// <summary>
    /// How long an arriving channel has to present its ticket. Defaults to 15 seconds - a real central has its hello
    /// ready the moment the channel opens, so anything still silent after this is not one.
    /// </summary>
    public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>The largest single write on a claimed channel, unless a reservation says otherwise. Defaults to 4096.</summary>
    public int MaxWriteSize { get; set; } = L2CapChannelStream.DefaultMaxWriteSize;
}


/// <summary>
/// One reserved channel, waiting for the central that was given its token.
/// </summary>
public sealed class L2CapTicket : IDisposable
{
    readonly CancellationTokenSource cancellation = new();
    int claimed;


    internal L2CapTicket(
        string token,
        string label,
        DateTimeOffset expiresUtc,
        ushort psm,
        int maxWriteSize,
        Func<L2CapChannelStream, CancellationToken, Task> handler
    )
    {
        this.Token = token;
        this.Label = label;
        this.ExpiresUtc = expiresUtc;
        this.Psm = psm;
        this.MaxWriteSize = maxWriteSize;
        this.Handler = handler;
    }


    /// <summary>Gets the single-use token a central presents. Give it only to the central the transfer is for.</summary>
    public string Token { get; }

    /// <summary>Gets what the transfer is, for logs.</summary>
    public string Label { get; }

    /// <summary>Gets when an unclaimed token stops being accepted.</summary>
    public DateTimeOffset ExpiresUtc { get; }

    /// <summary>Gets the PSM the central connects to.</summary>
    public ushort Psm { get; }

    /// <summary>Gets the largest single write on the claimed channel - worth passing to the central too.</summary>
    public int MaxWriteSize { get; }

    /// <summary>Gets whether a channel has claimed the ticket.</summary>
    public bool IsClaimed => Volatile.Read(ref this.claimed) == 1;

    /// <summary>Gets whether the token has expired. Says nothing about a claimed transfer that is still running.</summary>
    public bool HasExpired => DateTimeOffset.UtcNow > this.ExpiresUtc;

    /// <summary>Gets the token that cancels when the transfer is abandoned.</summary>
    public CancellationToken CancellationToken => this.cancellation.Token;

    internal Func<L2CapChannelStream, CancellationToken, Task> Handler { get; }

    internal bool TryClaim() => Interlocked.Exchange(ref this.claimed, 1) == 0;


    /// <summary>Abandons the transfer, stopping a handler that is already running.</summary>
    public void Cancel()
    {
        try
        {
            this.cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // already finished, which is where the cancel was trying to get to
        }
    }


    /// <inheritdoc />
    public void Dispose() => this.cancellation.Dispose();
}


/// <summary>
/// Registration for <see cref="L2CapTicketBroker"/>.
/// </summary>
public static class L2CapTicketBrokerServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="L2CapTicketBroker"/>. Needs an <see cref="IBleHostingManager"/> -
    /// <c>AddBluetoothLeHosting()</c>.
    /// </summary>
    public static IServiceCollection AddL2CapTicketBroker(this IServiceCollection services, Action<L2CapTicketBrokerOptions>? configure = null)
    {
        var options = new L2CapTicketBrokerOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(sp => new L2CapTicketBroker(
            sp.GetRequiredService<IBleHostingManager>(),
            options,
            sp.GetService<ILogger<L2CapTicketBroker>>()
        ));
        return services;
    }
}
