using System;
using System.Linq;
using Foundation;


namespace Shiny.Net.Http
{
    static class Extensions
    {
        public static NSUrlRequest ToNative(this HttpTransferRequest request)
        {
            var url = NSUrl.FromString(request.Uri);
            var native = new NSMutableUrlRequest(url)
            {
                HttpMethod = request.HttpMethod.Method,
                AllowsCellularAccess = request.UseMeteredConnection
            };

            if (!request.PostData.IsEmpty())
                native.Body = NSData.FromString(request.PostData);

            if (request.Headers.Any())
                native.Headers = NSDictionary.FromObjectsAndKeys(
                    request.Headers.Values.ToArray(),
                    request.Headers.Keys.ToArray()
                );

            return native;
        }
    }
}
