using System;
using Shiny;
using Shiny.Push;


namespace Sample
{
    public class MainViewModel : ViewModel
    {
        readonly IPushManager pushManager = ShinyHost.Resolve<IPushManager>();
        IDisposable? sub;


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.sub = this.pushManager
                .WhenReceived()
                .Subscribe(async pr =>
                {
                    var msg = pr.Notification?.Message ?? "Push seen on the foreground";
                    await this.Alert(msg, "Push Received");
                });
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.sub?.Dispose();
        }
    }
}
