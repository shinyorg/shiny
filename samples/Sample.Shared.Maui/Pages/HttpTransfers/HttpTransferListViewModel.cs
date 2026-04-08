namespace Sample.Shared.Maui.Pages.HttpTransfers;

[ShellMap<HttpTransferListPage>("httptransfers")]
public partial class HttpTransferListViewModel(
    IHttpTransferManager transfers,
    HttpTransferMonitor monitor,
    INavigator navigator
) : ObservableObject, IPageLifecycleAware
{
    public INotifyReadOnlyCollection<HttpTransferObject> Transfers => monitor.Transfers;

    public async void OnAppearing()
    {
        if (!monitor.IsStarted)
            await monitor.Start(removeFinished: false, removeErrors: false, removeCancelled: false);
    }

    public void OnDisappearing() { }

    [RelayCommand]
    Task GoToDownload() => navigator.NavigateTo("httptransferdownload");

    [RelayCommand]
    Task GoToUpload() => navigator.NavigateTo("httptransferupload");

    [RelayCommand]
    Task GoToAzureBlob() => navigator.NavigateTo("httptransferazureblob");

    [RelayCommand]
    Task Cancel(HttpTransferObject transfer) => transfers.Cancel(transfer.Identifier);

    [RelayCommand]
    Task CancelAll() => transfers.CancelAll();
}
