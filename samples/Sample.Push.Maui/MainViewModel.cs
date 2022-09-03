using System;
using Shiny;
using Shiny.Push;

namespace Sample;


public class MainViewModel : ViewModel
{
    readonly IPushManager pushManager;
    IDisposable? sub;

    public MainViewModel(IPushManager pushManager)
    {
        this.pushManager = pushManager;
    }


    public bool IsTagsSupported => this.pushManager is IPushTagSupport;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        this.sub = this.pushManager
            .WhenReceived()
            .Subscribe(async pr =>
            {
                var msg = pr.Notification?.Message ?? "Push seen on the foreground";
                await this.Alert(msg, "Push Received");
            });
    }


    public override void OnNavigatedFrom()
    {
        base.OnNavigatedFrom();
        this.sub?.Dispose();
    }
}
