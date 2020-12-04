using System;
using System.Threading.Tasks;
using Android.Content;
using Shiny.Logging;


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


        protected abstract Task OnReceiveAsync(Context? context, Intent? intent);
        public override void OnReceive(Context? context, Intent? intent)
        {
            var pendingResult = this.GoAsync();
            this.OnReceiveAsync(context, intent).ContinueWith(x =>
            {
                if (x.IsFaulted)
                    Log.Write(x.Exception);

                pendingResult.Finish();
            });
        }
    }
}
