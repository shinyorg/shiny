using Shiny.Push;

namespace Sample.Shared.Maui.Pages.Push;

[ShellMap<PushPage>("push")]
public partial class PushViewModel(IPushManager pushManager) : ObservableObject
{
    [ObservableProperty] string status = "Not registered";
    [ObservableProperty] string token = string.Empty;

    [RelayCommand]
    async Task Register()
    {
        var result = await pushManager.RequestAccess();
        this.Status = result.Status.ToString();
        this.Token = pushManager.RegistrationToken ?? "N/A";
    }

    [RelayCommand]
    async Task UnRegister()
    {
        await pushManager.UnRegister();
        this.Status = "Unregistered";
        this.Token = string.Empty;
    }
}
