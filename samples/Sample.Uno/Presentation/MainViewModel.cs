using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Net;
using Shiny.Power;
using Uno.Extensions;
#if PLATFORM
using Shiny.Notifications;
#endif

namespace Sample.Uno.Presentation;


/// <summary>
/// Created by Uno.Extensions navigation out of the same container Shiny registered into - IConnectivity, IBattery and
/// the notification manager are injected like any other service.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    readonly IServiceProvider services;
    readonly IConnectivity connectivity;
    readonly IBattery? battery;
    readonly SampleState state;
    readonly IDispatcher dispatcher;


    public MainViewModel(IServiceProvider services, IDispatcher dispatcher, IPlatform platform, IConnectivity connectivity, SampleState state)
    {
        this.services = services;
        this.connectivity = connectivity;
        // plain .NET on macOS and Windows (the desktop head there) has no battery API, so AddBattery registers nothing
        this.battery = services.GetService<IBattery>();
        this.state = state;
        // navigation can build view models off the UI thread (it does on Android) - its scoped IDispatcher is the window's
        this.dispatcher = dispatcher;

        this.Platform = $"{platform.GetType().Name} - {platform.AppData.FullName}";
        this.HostInfo = state.ShinyStarted == null
            ? "Shiny startup tasks have not run"
            : $"Shiny started at {state.ShinyStarted:HH:mm:ss.fff}, page created at {DateTimeOffset.Now:HH:mm:ss.fff}";

#if PLATFORM
        this.NotificationsSupported = true;
#else
        this.NotificationStatus = "Notifications run on the Android and iOS heads";
#endif
        this.connectivity.Changed += (_, _) => this.dispatcher.TryEnqueue(this.UpdateConnectivity);
        if (this.battery != null)
            this.battery.Changed += (_, _) => this.dispatcher.TryEnqueue(this.UpdateBattery);
        this.state.Changed += (_, _) => this.dispatcher.TryEnqueue(this.UpdateLastTap);

        this.UpdateConnectivity();
        this.UpdateBattery();
        this.UpdateLastTap();
    }


    public string Platform { get; }
    public string HostInfo { get; }
    public bool NotificationsSupported { get; }

    public string? Connectivity { get; private set => this.SetProperty(ref field, value); }
    public string? Battery { get; private set => this.SetProperty(ref field, value); }
    public string? NotificationStatus { get; private set => this.SetProperty(ref field, value); }
    public string? LastTap { get; private set => this.SetProperty(ref field, value); }


    void UpdateConnectivity()
        => this.Connectivity = $"{this.connectivity.Access} ({this.connectivity.ConnectionTypes})";

    void UpdateBattery()
        => this.Battery = this.battery == null
            ? "No battery API on this head"
            : $"{this.battery.Status} - {this.battery.Level:P0}";

    void UpdateLastTap()
        => this.LastTap = this.state.LastNotificationTap == null
            ? "No notification tapped yet"
            : $"Last tap: {this.state.LastNotificationTap}";


    [RelayCommand]
    async Task SendNotification()
    {
#if PLATFORM
        try
        {
            var notifications = this.services.GetRequiredService<INotificationManager>();

            // on Android this only completes because MainActivity forwards OnRequestPermissionsResult
            var access = await notifications.RequestAccess();
            this.NotificationStatus = $"Access: {access}";

            if (access == AccessState.Available)
            {
#if ANDROID
                await notifications.Send(new AndroidNotification
                {
                    Title = "Hello from Shiny",
                    Message = "Tap me - the tap is delivered to SampleNotificationDelegate",

                    // Uno's Skia renderer does not re-host its window in a recreated activity, so a tap must bring the
                    // running MainActivity forward (OnNewIntent) rather than clear the task and start a new one -
                    // Shiny's default (NewTask | ClearTask) leaves a blank screen
                    LaunchActivityFlags = Android.Content.ActivityFlags.NewTask
                        | Android.Content.ActivityFlags.ClearTop
                        | Android.Content.ActivityFlags.SingleTop
                });
#else
                await notifications.Send(new Shiny.Notifications.Notification
                {
                    Title = "Hello from Shiny",
                    Message = "Tap me - the tap is delivered to SampleNotificationDelegate"
                });
#endif
            }
        }
        catch (Exception ex)
        {
            this.NotificationStatus = ex.Message;
        }
#else
        await Task.CompletedTask;
#endif
    }
}
