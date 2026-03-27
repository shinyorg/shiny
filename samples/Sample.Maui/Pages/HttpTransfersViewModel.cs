namespace Sample.Maui.Pages;

[ShellMap<HttpTransfersPage>("httptransfers")]
public partial class HttpTransfersViewModel(IHttpTransferManager transfers) : ObservableObject, IPageLifecycleAware, IDisposable
{
    IDisposable? _sub;

    [ObservableProperty] string status = string.Empty;
    public ObservableCollection<TransferViewModel> Transfers { get; } = [];

    public void OnAppearing()
    {
        LoadTransfers();
        _sub = transfers
            .WhenUpdateReceived()
            .Subscribe(result => MainThread.BeginInvokeOnMainThread(() =>
            {
                Status = $"Transfer update: {result.Request.Uri}";
                LoadTransfers();
            }));
    }

    public void OnDisappearing()
    {
        _sub?.Dispose();
        _sub = null;
    }

    async void LoadTransfers()
    {
        var list = await transfers.GetTransfers();
        Transfers.Clear();
        foreach (var t in list)
        {
            Transfers.Add(new TransferViewModel
            {
                Identifier = t.Identifier,
                Uri = t.Request.Uri,
                Status = t.Status.ToString()
            });
        }
    }

    [RelayCommand]
    async Task QueueDownload()
    {
        var localPath = Path.Combine(FileSystem.CacheDirectory, "100MB.bin");
        var request = new HttpTransferRequest(
            "download-test",
            "https://speed.hetzner.de/100MB.bin",
            TransferType.Download,
            localPath
        );
        await transfers.Queue(request);
        Status = "Download queued";
        LoadTransfers();
    }

    [RelayCommand]
    async Task CancelAll()
    {
        await transfers.CancelAll();
        Transfers.Clear();
        Status = "All transfers cancelled";
    }

    public void Dispose() => _sub?.Dispose();
}

public partial class TransferViewModel : ObservableObject
{
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string uri = string.Empty;
    [ObservableProperty] string status = string.Empty;
}
