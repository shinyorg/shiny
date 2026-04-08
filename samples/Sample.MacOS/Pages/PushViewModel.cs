using Shiny.Push;

namespace Sample.MacOS.Pages.Push;

[ShellMap<PushPage>("push")]
public partial class PushViewModel(IPushManager pushManager, IDialogs dialogs) : ObservableObject
{
    [ObservableProperty] string status = "Unknown";
    [ObservableProperty] string? registrationToken;
    [ObservableProperty] string? nativeToken;

    [RelayCommand]
    async Task GetCurrentAccess()
    {
        var state = await pushManager.GetCurrentAccess();
        this.Status = state.ToString();
        this.RegistrationToken = pushManager.RegistrationToken;
        this.NativeToken = pushManager.NativeRegistrationToken;
    }

    [RelayCommand]
    async Task RequestAccess()
    {
        try
        {
            var result = await pushManager.RequestAccess();
            this.Status = result.Status.ToString();
            this.RegistrationToken = result.RegistrationToken;
            this.NativeToken = pushManager.NativeRegistrationToken;
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
    }

    [RelayCommand]
    async Task UnRegister()
    {
        await pushManager.UnRegister();
        this.RegistrationToken = null;
        this.NativeToken = null;
        this.Status = "Unregistered";
    }
}
