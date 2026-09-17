using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


/// <summary>
/// The answer a host gives a channel that presented an L2CAP ticket.
/// </summary>
public enum L2CapTicketStatus : byte
{
    /// <summary>The ticket was valid; the channel now belongs to the transfer it was issued for.</summary>
    Accepted = 0,

    /// <summary>The host issued no such ticket, or it expired before a channel presented it.</summary>
    UnknownTicket = 1,

    /// <summary>The two sides speak different versions of the handshake.</summary>
    VersionMismatch = 2,

    /// <summary>What arrived was not a ticket handshake.</summary>
    Malformed = 3,

    /// <summary>Another channel already claimed this ticket.</summary>
    AlreadyClaimed = 4,

    /// <summary>The host is carrying as many transfers as it will.</summary>
    Busy = 5
}


/// <summary>
/// Raised on the central when a host refuses the ticket a channel presented.
/// </summary>
public class L2CapTicketException : BleException
{
    /// <param name="status">What the host answered.</param>
    /// <param name="message">A description of the refusal.</param>
    public L2CapTicketException(L2CapTicketStatus status, string message) : base(message)
        => this.Status = status;

    /// <summary>Gets what the host answered.</summary>
    public L2CapTicketStatus Status { get; }
}


/// <summary>
/// The handshake that lets one L2CAP PSM carry many independent, authorised transfers.
/// </summary>
/// <remarks>
/// <para>A host that listens on one PSM cannot tell what an arriving channel is for, or whether it is allowed.
/// So the host issues a single-use ticket - a random token, returned over whatever authenticated route the
/// transfer was requested on (typically a GATT command) - and the first thing a central writes on the channel is
/// that token:</para>
/// <code>
/// central -> host   hello   "SL2T" [version:1] [length:1] [token:32 ASCII]
/// host -> central   accept  "SL2T" [version:1] [status:1]
/// </code>
/// <para>Only after <see cref="L2CapTicketStatus.Accepted"/> does the channel carry the transfer. Anyone else who
/// can reach the PSM gets a refusal and a closed channel. The host side is <c>L2CapTicketBroker</c> in
/// Shiny.BluetoothLE.Hosting; the central side is <see cref="ClaimTicket"/>, or <c>OpenL2CapTicketChannel</c> on a
/// peripheral in Shiny.BluetoothLE.</para>
/// </remarks>
public static class L2CapTickets
{
    /// <summary>The handshake version this build speaks.</summary>
    public const byte ProtocolVersion = 1;

    /// <summary>The length of a ticket token, in ASCII characters.</summary>
    public const int TokenLength = 32;

    /// <summary>The length of the central's hello frame.</summary>
    public const int HelloLength = 4 + 1 + 1 + TokenLength;

    /// <summary>The length of the host's accept frame.</summary>
    public const int AcceptLength = 4 + 1 + 1;

    static ReadOnlySpan<byte> Magic => "SL2T"u8;


    /// <summary>Creates a new random token.</summary>
    public static string CreateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenLength / 2)).ToLowerInvariant();


    /// <summary>Builds the hello frame a central sends first.</summary>
    /// <param name="token">The token the host issued.</param>
    public static byte[] CreateHello(string token)
    {
        if (String.IsNullOrWhiteSpace(token) || token.Length != TokenLength)
            throw new ArgumentException($"A ticket token is exactly {TokenLength} characters", nameof(token));

        var frame = new byte[HelloLength];
        Magic.CopyTo(frame);
        frame[4] = ProtocolVersion;
        frame[5] = TokenLength;
        Encoding.ASCII.GetBytes(token, frame.AsSpan(6));
        return frame;
    }


    /// <summary>Parses a hello frame.</summary>
    /// <returns><see cref="L2CapTicketStatus.Accepted"/> with <paramref name="token"/> set, or why the frame is unusable.</returns>
    public static L2CapTicketStatus ReadHello(ReadOnlySpan<byte> frame, out string token)
    {
        token = String.Empty;

        if (frame.Length < HelloLength || !frame.Slice(0, 4).SequenceEqual(Magic))
            return L2CapTicketStatus.Malformed;

        if (frame[4] != ProtocolVersion)
            return L2CapTicketStatus.VersionMismatch;

        if (frame[5] != TokenLength)
            return L2CapTicketStatus.Malformed;

        var candidate = frame.Slice(6, TokenLength);
        foreach (var c in candidate)
        {
            // printable ASCII only, so a token can never carry a control byte into a log line or a dictionary key
            if (c is < 0x21 or > 0x7E)
                return L2CapTicketStatus.Malformed;
        }

        token = Encoding.ASCII.GetString(candidate);
        return L2CapTicketStatus.Accepted;
    }


    /// <summary>Builds the accept frame a host answers with.</summary>
    public static byte[] CreateAccept(L2CapTicketStatus status)
    {
        var frame = new byte[AcceptLength];
        Magic.CopyTo(frame);
        frame[4] = ProtocolVersion;
        frame[5] = (byte)status;
        return frame;
    }


    /// <summary>Parses an accept frame.</summary>
    public static L2CapTicketStatus ReadAccept(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < AcceptLength || !frame.Slice(0, 4).SequenceEqual(Magic))
            return L2CapTicketStatus.Malformed;

        // reported as itself rather than as malformed - it is the one failure a user can act on, by updating
        if (frame[4] != ProtocolVersion)
            return L2CapTicketStatus.VersionMismatch;

        var status = (L2CapTicketStatus)frame[5];
        return Enum.IsDefined(status) ? status : L2CapTicketStatus.Malformed;
    }


    /// <summary>
    /// Presents a ticket on a channel the central has just opened, and returns the channel as a stream once the host
    /// accepts it.
    /// </summary>
    /// <param name="channel">A channel opened to the PSM the host named alongside the ticket.</param>
    /// <param name="token">The ticket's token.</param>
    /// <param name="maxWriteSize">The largest single write on the returned stream.</param>
    /// <param name="timeout">How long to wait for the host's answer. Defaults to 20 seconds.</param>
    /// <param name="cancellationToken">Cancels the handshake.</param>
    /// <exception cref="L2CapTicketException">The host refused the ticket. The channel has been closed.</exception>
    public static async Task<L2CapChannelStream> ClaimTicket(
        this L2CapChannel channel,
        string token,
        int maxWriteSize = L2CapChannelStream.DefaultMaxWriteSize,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));

        var hello = CreateHello(token);
        var stream = new L2CapChannelStream(channel, maxWriteSize);
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout ?? TimeSpan.FromSeconds(20));

            await stream.WriteAsync(hello, cts.Token).ConfigureAwait(false);

            var accept = new byte[AcceptLength];
            await stream.ReadExactlyAsync(accept, cts.Token).ConfigureAwait(false);

            var status = ReadAccept(accept);
            if (status != L2CapTicketStatus.Accepted)
                throw new L2CapTicketException(status, Describe(status));

            return stream;
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }


    static string Describe(L2CapTicketStatus status) => status switch
    {
        L2CapTicketStatus.UnknownTicket => "The host does not recognise this ticket. It most likely expired - request the transfer again",
        L2CapTicketStatus.VersionMismatch => "The host speaks a different L2CAP ticket protocol version. One side needs updating",
        L2CapTicketStatus.AlreadyClaimed => "Another channel has already claimed this ticket",
        L2CapTicketStatus.Busy => "The host is already carrying as many transfers as it will",
        _ => "The host rejected the ticket handshake"
    };
}
