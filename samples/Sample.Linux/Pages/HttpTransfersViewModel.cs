namespace Sample.Linux.Pages;

[ShellMap<HttpTransfersPage>("httptransfers")]
public partial class HttpTransfersViewModel(
    IHttpTransferManager transfers,
    HttpTransferMonitor monitor
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;

    [ObservableProperty] string downloadUrl =
        "https://proof.ovh.net/files/10Mb.dat";

    [ObservableProperty] string uploadUrl = "https://httpbin.org/post";
    [ObservableProperty] string uploadFilePath = string.Empty;

    public INotifyReadOnlyCollection<HttpTransferObject> Transfers => monitor.Transfers;

    public async void OnAppearing()
    {
        if (!monitor.IsStarted)
            await monitor.Start(removeFinished: false, removeErrors: false, removeCancelled: false);
    }

    public void OnDisappearing() { }

    [RelayCommand]
    void Preset(string sizeMb)
    {
        this.DownloadUrl = sizeMb switch
        {
            "1"   => "https://proof.ovh.net/files/1Mb.dat",
            "10"  => "https://proof.ovh.net/files/10Mb.dat",
            "100" => "https://proof.ovh.net/files/100Mb.dat",
            _     => this.DownloadUrl
        };
    }

    [RelayCommand]
    async Task QueueDownload()
    {
        if (string.IsNullOrWhiteSpace(this.DownloadUrl))
        {
            this.Status = "Download URL is required";
            return;
        }

        try
        {
            var fileName = Path.GetFileName(new Uri(this.DownloadUrl).AbsolutePath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "download.bin";

            var localPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{fileName}");

            var request = new HttpTransferRequest(
                Guid.NewGuid().ToString("N"),
                this.DownloadUrl,
                TransferType.Download,
                localPath
            );
            await transfers.Queue(request);
            this.Status = $"Download queued → {localPath}";
        }
        catch (Exception ex)
        {
            this.Status = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    async Task GenerateTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"shiny-upload-{Guid.NewGuid():N}.bin");
        var bytes = new byte[64 * 1024];
        Random.Shared.NextBytes(bytes);
        await File.WriteAllBytesAsync(path, bytes);
        this.UploadFilePath = path;
        this.Status = $"Generated {bytes.Length:N0} byte file";
    }

    [RelayCommand]
    Task QueueUploadRaw() => this.QueueUpload(TransferType.UploadRaw);

    [RelayCommand]
    Task QueueUploadMultipart() => this.QueueUpload(TransferType.UploadMultipart);

    async Task QueueUpload(TransferType type)
    {
        if (string.IsNullOrWhiteSpace(this.UploadUrl))
        {
            this.Status = "Upload URL is required";
            return;
        }
        if (string.IsNullOrWhiteSpace(this.UploadFilePath) || !File.Exists(this.UploadFilePath))
        {
            this.Status = "Upload file path is missing or does not exist";
            return;
        }

        try
        {
            var request = new HttpTransferRequest(
                Guid.NewGuid().ToString("N"),
                this.UploadUrl,
                type,
                this.UploadFilePath
            );
            await transfers.Queue(request);
            this.Status = $"{type} queued";
        }
        catch (Exception ex)
        {
            this.Status = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    async Task Cancel(string identifier)
    {
        await transfers.Cancel(identifier);
        this.Status = $"Cancelled {identifier}";
    }

    [RelayCommand]
    async Task CancelAll()
    {
        await transfers.CancelAll();
        this.Status = "All cancelled";
    }
}
