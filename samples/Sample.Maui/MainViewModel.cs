using Shiny.Notifications;

namespace Sample;


public class MainViewModel : ReactiveObject, IPageLifecycleAware
{
    readonly IDeviceDisplay display;


    public MainViewModel(
        INavigationService navigator,
        IPageDialogService dialogs,
        IDeviceDisplay display,
        IAppInfo appInfo,
        INotificationManager notifications
    )
    {
        this.display = display;

        this.Navigate = ReactiveCommand.CreateFromTask<string>(async uri =>
        {
            await navigator.Navigate("NavigationPage/" + uri);
            this.IsMenuVisible = false;
        });

        this.NotificationPermissions = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await notifications.RequestAccess();
            if (result != AccessState.Available)
                await dialogs.DisplayAlertAsync("Permission Denied", "Most background operations in this app use notifications", "OK");
        });

        this.OpenAppSettings = ReactiveCommand.Create(() => appInfo.ShowSettingsUI());
    }


    [Reactive] public bool IsMenuVisible { get; private set; }
    public ICommand Navigate { get; }
    public ICommand NotificationPermissions { get; }
    public ICommand OpenAppSettings { get; }

    public void OnAppearing() => this.display.KeepScreenOn = true;
    public void OnDisappearing() { }
}
