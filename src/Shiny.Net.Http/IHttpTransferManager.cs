using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


/// <summary>
/// Manages background HTTP upload and download transfers
/// </summary>
public interface IHttpTransferManager
{
    /// <summary>
    /// Gets all pending and active transfers
    /// </summary>
    /// <returns>The list of current transfers</returns>
    Task<IList<HttpTransfer>> GetTransfers();

    /// <summary>
    /// Queues a new HTTP transfer for background execution
    /// </summary>
    /// <param name="request">The transfer request configuration</param>
    /// <returns>The queued transfer</returns>
    Task<HttpTransfer> Queue(HttpTransferRequest request);

    /// <summary>
    /// Cancels a transfer by identifier
    /// </summary>
    /// <param name="identifier">The transfer identifier</param>
    Task Cancel(string identifier);

    /// <summary>
    /// Cancels all pending and active transfers
    /// </summary>
    Task CancelAll();

    /// <summary>
    /// Returns an observable that emits the current transfer count when it changes
    /// </summary>
    /// <returns>An observable of the active transfer count</returns>
    IObservable<int> WatchCount();

    /// <summary>
    /// Returns an observable that emits transfer progress and completion updates
    /// </summary>
    /// <returns>An observable of transfer result updates</returns>
    IObservable<HttpTransferResult> WhenUpdateReceived();
}
