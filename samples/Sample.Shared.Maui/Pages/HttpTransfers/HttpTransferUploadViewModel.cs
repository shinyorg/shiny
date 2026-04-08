namespace Sample.Shared.Maui.Pages.HttpTransfers;

[ShellMap<HttpTransferUploadPage>("httptransferupload")]
public partial class HttpTransferUploadViewModel(IHttpTransferManager transfers) : ObservableObject
{
    [ObservableProperty] string identifier = Guid.NewGuid().ToString();
    [ObservableProperty] string uri = string.Empty;
    [ObservableProperty] string selectedFilePath = string.Empty;
    [ObservableProperty] string selectedFileName = string.Empty;
    [ObservableProperty] int selectedTransferTypeIndex;
    [ObservableProperty] string fileFormDataName = "file";
    [ObservableProperty] string httpMethod = string.Empty;
    [ObservableProperty] bool useMeteredConnection = true;
    [ObservableProperty] string headerKey = string.Empty;
    [ObservableProperty] string headerValue = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsJsonVisible))]
    [NotifyPropertyChangedFor(nameof(IsFormDataVisible))]
    int selectedContentTypeIndex;

    [ObservableProperty] string jsonBody = string.Empty;
    [ObservableProperty] string contentFormDataName = string.Empty;
    [ObservableProperty] string formDataKey = string.Empty;
    [ObservableProperty] string formDataValue = string.Empty;
    [ObservableProperty] string status = string.Empty;

    public bool IsJsonVisible => this.SelectedContentTypeIndex == 1;
    public bool IsFormDataVisible => this.SelectedContentTypeIndex == 2;
    public List<string> TransferTypes { get; } = ["Upload Multipart", "Upload Raw"];
    public List<string> ContentTypes { get; } = ["None", "JSON", "Form Data"];
    public ObservableCollection<HeaderViewModel> Headers { get; } = [];
    public ObservableCollection<HeaderViewModel> FormDataPairs { get; } = [];

    [RelayCommand]
    async Task PickFile()
    {
        var result = await FilePicker.PickAsync();
        if (result == null)
            return;

        this.SelectedFilePath = result.FullPath;
        this.SelectedFileName = result.FileName;
    }

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
    void AddFormData()
    {
        if (string.IsNullOrWhiteSpace(this.FormDataKey))
            return;

        this.FormDataPairs.Add(new HeaderViewModel { Key = this.FormDataKey, Value = this.FormDataValue });
        this.FormDataKey = string.Empty;
        this.FormDataValue = string.Empty;
    }

    [RelayCommand]
    void RemoveFormData(HeaderViewModel pair) => this.FormDataPairs.Remove(pair);

    [RelayCommand]
    async Task Queue()
    {
        if (string.IsNullOrWhiteSpace(this.Uri))
        {
            this.Status = "URI is required";
            return;
        }
        if (string.IsNullOrWhiteSpace(this.SelectedFilePath))
        {
            this.Status = "File is required";
            return;
        }

        Dictionary<string, string>? headers = null;
        if (this.Headers.Count > 0)
        {
            headers = [];
            foreach (var h in this.Headers)
                headers[h.Key] = h.Value;
        }

        TransferHttpContent? httpContent = null;
        switch (this.SelectedContentTypeIndex)
        {
            case 1:
                httpContent = new TransferHttpContent(this.JsonBody, "application/json")
                {
                    ContentFormDataName = string.IsNullOrWhiteSpace(this.ContentFormDataName) ? null : this.ContentFormDataName
                };
                break;

            case 2:
                if (this.FormDataPairs.Count > 0)
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var p in this.FormDataPairs)
                        dict[p.Key] = p.Value;
                    httpContent = TransferHttpContent.FromFormData(dict);
                }
                break;
        }

        var transferType = this.SelectedTransferTypeIndex == 0
            ? TransferType.UploadMultipart
            : TransferType.UploadRaw;

        var request = new HttpTransferRequest(
            this.Identifier,
            this.Uri,
            transferType,
            this.SelectedFilePath,
            this.UseMeteredConnection,
            httpContent,
            headers
        )
        {
            FileFormDataName = this.FileFormDataName
        };

        if (!string.IsNullOrWhiteSpace(this.HttpMethod))
            request.HttpMethod = this.HttpMethod;

        await transfers.Queue(request);
        this.Status = "Upload queued";

        this.Identifier = Guid.NewGuid().ToString();
        this.Uri = string.Empty;
        this.SelectedFilePath = string.Empty;
        this.SelectedFileName = string.Empty;
    }
}
