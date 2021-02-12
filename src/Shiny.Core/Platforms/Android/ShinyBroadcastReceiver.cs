using System;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Extensions.Logging;


namespace Shiny
{
    public abstract class ShinyBroadcastReceiver : BroadcastReceiver
    {
        readonly Lazy<IMessageBus> messageBus = ShinyHost.LazyResolve<IMessageBus>();


        protected void Publish<T>(T args) => this.messageBus.Value.Publish(args);
        protected void Publish<T>(string name, T args) => this.messageBus.Value.Publish(name, args);
        protected void Publish(string name) => this.messageBus.Value.Publish(name);

        protected T Resolve<T>() => ShinyHost.Resolve<T>();
        protected Lazy<T> ResolveLazy<T>() => ShinyHost.LazyResolve<T>();
        protected virtual void LogError<T>(Exception exception, string message)
            => ShinyHost.LoggerFactory.CreateLogger<T>().LogError(exception, message);


        protected abstract Task OnReceiveAsync(Context? context, Intent? intent);
        public override void OnReceive(Context? context, Intent? intent)
        {
            var pendingResult = this.GoAsync();
            this.OnReceiveAsync(context, intent).ContinueWith(x =>
            {
                if (x.IsFaulted)
                {
                    ShinyHost
                        .LoggerFactory
                        .CreateLogger<ILogger<ShinyBroadcastReceiver>>()
                        .LogError(x.Exception, "Error in broadcast receiver");
                }
                pendingResult.Finish();
            });
        }
    }
}
