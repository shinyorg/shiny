using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


/// <summary>
/// Implemented by peripherals whose backing platform supports opening L2CAP Connection-Oriented Channels.
/// </summary>
public interface ICanL2Cap : IPeripheral
{
    /// <summary>
    /// Opens an L2CAP channel to the connected peripheral on the supplied PSM.
    /// </summary>
    /// <param name="psm">The PSM advertised by the peripheral.</param>
    /// <param name="secure">
    /// On Android, when true an encrypted/authenticated channel is requested
    /// (requires API 29+). On Apple platforms this parameter is ignored — security
    /// is determined by whether the peripheral published the channel as encrypted.
    /// </param>
    /// <returns>A single-value observable with the opened <see cref="L2CapChannel"/>.</returns>
    IObservable<L2CapChannel> OpenL2CapChannel(ushort psm, bool secure);
}


/// <summary>
/// Helpers around the optional <see cref="ICanL2Cap"/> capability.
/// </summary>
public static class FeatureL2Cap
{
    /// <summary>
    /// Returns true when the supplied peripheral's platform implementation supports L2CAP.
    /// </summary>
    public static bool IsL2CapAvailable(this IPeripheral peripheral) => peripheral is ICanL2Cap;


    /// <summary>
    /// Attempts to open an L2CAP channel; returns an empty stream when the platform does not support L2CAP.
    /// </summary>
    /// <param name="peripheral">The peripheral to query.</param>
    /// <param name="psm">The PSM advertised by the peripheral.</param>
    /// <param name="secure">See <see cref="ICanL2Cap.OpenL2CapChannel"/>.</param>
    public static IObservable<L2CapChannel> TryOpenL2CapChannel(this IPeripheral peripheral, ushort psm, bool secure)
        => peripheral is ICanL2Cap support
            ? support.OpenL2CapChannel(psm, secure)
            : Observable.Empty<L2CapChannel>();


    /// <summary>
    /// Opens an L2CAP channel and claims an <see cref="L2CapTickets">L2CAP ticket</see> on it, returning the channel as
    /// a stream once the peripheral accepts the ticket.
    /// </summary>
    /// <param name="peripheral">The connected peripheral.</param>
    /// <param name="psm">The PSM the peripheral named alongside the ticket.</param>
    /// <param name="token">The ticket's token.</param>
    /// <param name="secure">
    /// On Android, must match how the peripheral's listener was opened - <c>L2CapTicketBroker</c> defaults to true. Ignored on
    /// Apple platforms, as for <see cref="ICanL2Cap.OpenL2CapChannel"/>.
    /// </param>
    /// <param name="maxWriteSize">The largest single write on the returned stream.</param>
    /// <param name="cancellationToken">Cancels the connect and the handshake.</param>
    /// <exception cref="NotSupportedException">This platform cannot open L2CAP channels.</exception>
    /// <exception cref="L2CapTicketException">The peripheral refused the ticket.</exception>
    public static async Task<L2CapChannelStream> OpenL2CapTicketChannel(
        this IPeripheral peripheral,
        ushort psm,
        string token,
        bool secure = true,
        int maxWriteSize = L2CapChannelStream.DefaultMaxWriteSize,
        CancellationToken cancellationToken = default
    )
    {
        if (peripheral is not ICanL2Cap support)
            throw new NotSupportedException("This platform cannot open L2CAP channels");

        var channel = await support
            .OpenL2CapChannel(psm, secure)
            .ToTask(cancellationToken)
            .ConfigureAwait(false);

        return await channel
            .ClaimTicket(token, maxWriteSize, null, cancellationToken)
            .ConfigureAwait(false);
    }
}
