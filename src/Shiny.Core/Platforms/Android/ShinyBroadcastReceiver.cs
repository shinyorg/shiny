using System;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny;


public abstract class ShinyBroadcastReceiver : BroadcastReceiver
{
    protected T Resolve<T>() => Host.Current.Services.GetRequiredService<T>()!;
    protected virtual void LogError<T>(Exception exception, string message)
        => Host.Current.Logging.CreateLogger<T>().LogError(exception, message);


    protected abstract Task OnReceiveAsync(Context? context, Intent? intent);
    public override void OnReceive(Context? context, Intent? intent)
    {
        var pendingResult = this.GoAsync();
        this.OnReceiveAsync(context, intent).ContinueWith(x =>
        {
            if (x.IsFaulted)
            {
                this.LogError<ShinyBroadcastReceiver>(x.Exception!, "Error in broadcast receiver");
            }
            pendingResult!.Finish();
        });
    }
}
