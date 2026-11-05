using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Hosting;
using Shiny.Net;
#if PLATFORM
using Shiny.Notifications;
#endif

namespace Sample.Avalonia;


public partial class MainView : UserControl
{
    readonly IConnectivity connectivity;
    readonly SampleState state;


    public MainView()
    {
        this.InitializeComponent();

        var services = Host.ServiceProvider;
        var platform = services.GetRequiredService<IPlatform>();
        this.connectivity = services.GetRequiredService<IConnectivity>();
        this.state = services.GetRequiredService<SampleState>();

        this.PlatformText.Text = platform.GetType().Name;
        this.AppDataText.Text = platform.AppData.FullName;
#if !PLATFORM
        this.NotifyButton.IsEnabled = false;
        this.NotifyStatusText.Text = "Notifications run on the Android and iOS heads";
#endif
        this.UpdateConnectivity();
        this.UpdateLastTap();
    }


    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.connectivity.Changed += this.OnConnectivityChanged;
        this.state.Changed += this.OnStateChanged;
    }


    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        this.connectivity.Changed -= this.OnConnectivityChanged;
        this.state.Changed -= this.OnStateChanged;
        base.OnDetachedFromVisualTree(e);
    }


    void OnConnectivityChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(this.UpdateConnectivity);
    void OnStateChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(this.UpdateLastTap);

    void UpdateConnectivity()
        => this.ConnectivityText.Text = $"{this.connectivity.Access} ({this.connectivity.ConnectionTypes})";

    void UpdateLastTap()
        => this.LastTapText.Text = this.state.LastNotificationTap == null
            ? "No notification tapped yet"
            : $"Last tap: {this.state.LastNotificationTap}";


    async void OnSendNotification(object? sender, RoutedEventArgs e)
    {
#if PLATFORM
        try
        {
            var notifications = Host.ServiceProvider.GetRequiredService<INotificationManager>();

            // on Android this only completes because MainActivity forwards OnRequestPermissionsResult
            var access = await notifications.RequestAccess();
            this.NotifyStatusText.Text = $"Access: {access}";

            if (access == AccessState.Available)
            {
                await notifications.Send(new Shiny.Notifications.Notification
                {
                    Title = "Hello from Shiny",
                    Message = "Tap me - the tap is delivered to SampleNotificationDelegate"
                });
            }
        }
        catch (Exception ex)
        {
            this.NotifyStatusText.Text = ex.Message;
        }
#else
        await Task.CompletedTask;
#endif
    }
}
