using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


/// <summary>
/// Manages background HTTP upload and download transfers that continue while the app is suspended.
/// </summary>
public interface IHttpTransferManager
{
    /// <summary>
    /// Gets all pending and active transfers currently tracked by the manager.
    /// </summary>
    /// <returns>The list of known transfers.</returns>
    Task<IList<HttpTransfer>> GetTransfers();

    /// <summary>
    /// Queues a new HTTP transfer for background execution.
    /// </summary>
    /// <param name="request">The transfer request describing the upload or download.</param>
    /// <returns>The created transfer in its initial pending state.</returns>
    Task<HttpTransfer> Queue(HttpTransferRequest request);

    /// <summary>
    /// Cancels and removes a single transfer by its identifier.
    /// </summary>
    /// <param name="identifier">The unique identifier of the transfer to cancel.</param>
    Task Cancel(string identifier);

    /// <summary>
    /// Pauses an in-flight or pending transfer without cancelling it - the transfer
    /// remains queued and can be resumed later with <see cref="Resume"/>.
    /// <para>
    /// Downloads resume from where they left off (HTTP range / native resume). Uploads are
    /// stopped but are not resumable, so resuming an upload restarts it from the beginning.
    /// </para>
    /// No-op if the transfer does not exist or is already in a terminal state.
    /// </summary>
    /// <param name="identifier">The unique identifier of the transfer to pause.</param>
    Task Pause(string identifier);

    /// <summary>
    /// Resumes a transfer previously paused with <see cref="Pause"/>. Downloads continue
    /// from their existing progress; uploads restart from the beginning.
    /// No-op if the transfer does not exist or is not in a paused state.
    /// </summary>
    /// <param name="identifier">The unique identifier of the transfer to resume.</param>
    Task Resume(string identifier);

    /// <summary>
    /// Cancels and removes all pending and active transfers.
    /// </summary>
    Task CancelAll();

    /// <summary>
    /// Raised whenever the number of active transfers changes.
    /// </summary>
    event EventHandler<int> CountChanged;

    /// <summary>
    /// Raised whenever a transfer emits a progress or completion update.
    /// </summary>
    event EventHandler<HttpTransferResult> UpdateReceived;
}
