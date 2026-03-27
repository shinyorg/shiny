using System;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


/// <summary>
/// Delegate that receives HTTP transfer lifecycle events
/// </summary>
public interface IHttpTransferDelegate
{
    // void OnQueueChanged(int itemCount);

    // could offer a chance to ask delegate if error should continue? maybe put that in a separate concern?

    /// <summary>
    /// Called when a transfer encounters an error
    /// </summary>
    /// <param name="request">The transfer request that failed</param>
    /// <param name="statusCode">The HTTP status code, or 0 for non-HTTP errors</param>
    /// <param name="ex">The exception that occurred</param>
    Task OnError(HttpTransferRequest request, int statusCode, Exception ex);

    /// <summary>
    /// Called when a transfer completes successfully
    /// </summary>
    /// <param name="request">The completed transfer request</param>
    Task OnCompleted(HttpTransferRequest request);
}