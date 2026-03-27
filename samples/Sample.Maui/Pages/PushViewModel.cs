using Shiny.Push;

namespace Sample.Maui.Pages;

[ShellMap<PushPage>("push")]
public partial class PushViewModel(IPushManager pushManager) : ObservableObject
{
    [ObservableProperty] string status = "Not registered";
    [ObservableProperty] string token = string.Empty;

    [RelayCommand]
    async Task Register()
    {
        var result = await pushManager.RequestAccess();
        Status = result.Status.ToString();
        Token = pushManager.RegistrationToken ?? "N/A";
    }

    [RelayCommand]
    async Task UnRegister()
    {
        await pushManager.UnRegister();
        Status = "Unregistered";
        Token = string.Empty;
    }
}
