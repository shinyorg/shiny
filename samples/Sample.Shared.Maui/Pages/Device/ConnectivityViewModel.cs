using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Device;

[ShellMap<ConnectivityPage>("connectivity")]
public partial class ConnectivityViewModel(
    Shiny.Net.IConnectivity connectivity,
    IMainThread mainThread
) : ObservableObject, IPageLifecycleAware
{
    IDisposable? sub;

    [ObservableProperty] string access = "Unknown";
    [ObservableProperty] string connectionTypes = "Unknown";

    public void OnAppearing()
    {
        this.sub = connectivity
            .WhenChanged()
            .Subscribe(c => mainThread.InvokeOnMainThreadAsync(() =>
            {
                this.Access = c.Access.ToString();
                this.ConnectionTypes = c.ConnectionTypes.ToString();
            }));
    }

    public void OnDisappearing() => this.sub?.Dispose();
}
