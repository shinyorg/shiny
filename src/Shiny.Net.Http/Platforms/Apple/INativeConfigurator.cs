using Foundation;

namespace Shiny.Net.Http;


/// <summary>
/// Allows customization of native Apple NSUrlSession configuration and request objects
/// </summary>
public interface INativeConfigurator
{
    /// <summary>
    /// Configures the NSUrlSession configuration used for background transfers
    /// </summary>
    /// <param name="configuration">The session configuration to customize</param>
    void Configure(NSUrlSessionConfiguration configuration);

    /// <summary>
    /// Configures a native URL request before it is sent
    /// </summary>
    /// <param name="nativeRequest">The native request to customize</param>
    /// <param name="request">The Shiny transfer request for reference</param>
    void Configure(NSMutableUrlRequest nativeRequest, HttpTransferRequest request);
}