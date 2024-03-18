using Foundation;

namespace Shiny.Net.Http;


public interface INativeConfigurator
{
    void Configure(NSUrlSessionConfiguration configuration);
    void Configure(NSMutableUrlRequest nativeRequest, HttpTransferRequest request);
}