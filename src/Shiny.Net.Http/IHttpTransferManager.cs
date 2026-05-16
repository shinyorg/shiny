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
    Task<IList<HttpTransfer>> GetTransfers();

    /// <summary>
    /// Queues a new HTTP transfer for background execution
    /// </summary>
    Task<HttpTransfer> Queue(HttpTransferRequest request);

    /// <summary>
    /// Cancels a transfer by identifier
    /// </summary>
    Task Cancel(string identifier);

    /// <summary>
    /// Cancels all pending and active transfers
    /// </summary>
    Task CancelAll();

    /// <summary>
    /// Raised whenever the number of active transfers changes
    /// </summary>
    event EventHandler<int> CountChanged;

    /// <summary>
    /// Raised whenever a transfer emits a progress or completion update
    /// </summary>
    event EventHandler<HttpTransferResult> UpdateReceived;
}
