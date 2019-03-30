using System;
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

            foreach (var header in request.Headers)
            {
                native.Headers.SetValueForKey(
                    new NSString(header.Value),
                    new NSString(header.Key)
                );
            }
            return native;
        }
    }
}
