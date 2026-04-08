namespace Sample.Shared.Maui.Pages.HttpTransfers;

[ShellMap<HttpTransferDownloadPage>("httptransferdownload")]
public partial class HttpTransferDownloadViewModel(IHttpTransferManager transfers) : ObservableObject
{
    [ObservableProperty] string identifier = Guid.NewGuid().ToString();
    [ObservableProperty] string uri = string.Empty;
    [ObservableProperty] string localFileName = string.Empty;
    [ObservableProperty] string httpMethod = string.Empty;
    [ObservableProperty] bool useMeteredConnection = true;
    [ObservableProperty] string headerKey = string.Empty;
    [ObservableProperty] string headerValue = string.Empty;
    [ObservableProperty] string status = string.Empty;

    public ObservableCollection<HeaderViewModel> Headers { get; } = [];

    [RelayCommand]
    void AddHeader()
    {
        if (string.IsNullOrWhiteSpace(this.HeaderKey))
            return;

        this.Headers.Add(new HeaderViewModel { Key = this.HeaderKey, Value = this.HeaderValue });
        this.HeaderKey = string.Empty;
        this.HeaderValue = string.Empty;
    }

    [RelayCommand]
    void RemoveHeader(HeaderViewModel header) => this.Headers.Remove(header);

    [RelayCommand]
    async Task Queue()
    {
        if (string.IsNullOrWhiteSpace(this.Uri))
        {
            this.Status = "URI is required";
            return;
        }

        var fileName = string.IsNullOrWhiteSpace(this.LocalFileName)
            ? Path.GetFileName(new Uri(this.Uri).AbsolutePath)
            : this.LocalFileName;

        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "download.bin";

        var localPath = Path.Combine(FileSystem.CacheDirectory, fileName);

        Dictionary<string, string>? headers = null;
        if (this.Headers.Count > 0)
        {
            headers = [];
            foreach (var h in this.Headers)
                headers[h.Key] = h.Value;
        }

        var request = new HttpTransferRequest(
            this.Identifier,
            this.Uri,
            TransferType.Download,
            localPath,
            this.UseMeteredConnection,
            Headers: headers
        );

        if (!string.IsNullOrWhiteSpace(this.HttpMethod))
            request.HttpMethod = this.HttpMethod;

        await transfers.Queue(request);
        this.Status = "Download queued";

        this.Identifier = Guid.NewGuid().ToString();
        this.Uri = string.Empty;
        this.LocalFileName = string.Empty;
    }
}
