using System;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public interface IHttpTransferDelegate
{
    // could offer a chance to ask delegate if error should continue? maybe put that in a separate concern?
    Task OnError(HttpTransferRequest request, Exception ex);
    Task OnCompleted(HttpTransferRequest request);
}

#if ANDROID
public interface IAndroidHttpTransferDelegate : IHttpTransferDelegate
{
    // TODO: may want to pass the state, not just the request
    void ConfigureNotification(AndroidX.Core.App.NotificationCompat.Builder builder, HttpTransferRequest request);
}
#endif