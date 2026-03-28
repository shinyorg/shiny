namespace Sample.Maui.Pages.HttpTransfers;

[ShellMap<HttpTransferAzureBlobPage>("httptransferazureblob")]
public partial class HttpTransferAzureBlobViewModel(IHttpTransferManager transfers) : ObservableObject
{
    const string PrefTenant = "azure_blob_tenant";
    const string PrefContainer = "azure_blob_container";
    const string PrefSasToken = "azure_blob_sas";

    [ObservableProperty] string blobTenant = Preferences.Get(PrefTenant, "");
    [ObservableProperty] string blobContainer = Preferences.Get(PrefContainer, "");
    [ObservableProperty] string blobSasToken = Preferences.Get(PrefSasToken, "");
    [ObservableProperty] string remoteFileName = string.Empty;
    [ObservableProperty] string selectedFilePath = string.Empty;
    [ObservableProperty] string selectedFileName = string.Empty;
    [ObservableProperty] bool useMeteredConnection;
    [ObservableProperty] string status = string.Empty;

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
    async Task Upload()
    {
        if (string.IsNullOrWhiteSpace(this.BlobTenant) || string.IsNullOrWhiteSpace(this.BlobContainer) || string.IsNullOrWhiteSpace(this.BlobSasToken))
        {
            this.Status = "Fill in tenant, container, and SAS token";
            return;
        }

        if (string.IsNullOrWhiteSpace(this.SelectedFilePath))
        {
            this.Status = "Pick a file first";
            return;
        }

        Preferences.Set(PrefTenant, this.BlobTenant);
        Preferences.Set(PrefContainer, this.BlobContainer);
        Preferences.Set(PrefSasToken, this.BlobSasToken);

        var builder = new AzureBlobStorageUploadRequest(this.SelectedFilePath)
            .WithBlobContainer(this.BlobTenant, this.BlobContainer)
            .WithSasToken(this.BlobSasToken);

        if (!string.IsNullOrWhiteSpace(this.RemoteFileName))
            builder.WithRemoteFileName(this.RemoteFileName);

        if (this.UseMeteredConnection)
            builder.WithMeteredConnection();

        var request = builder.Build();
        await transfers.Queue(request);
        this.Status = $"Azure Blob upload queued: {this.SelectedFileName}";

        this.SelectedFilePath = string.Empty;
        this.SelectedFileName = string.Empty;
        this.RemoteFileName = string.Empty;
    }
}
