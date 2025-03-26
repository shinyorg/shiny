using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny.Net.Http;

namespace Sample.HttpTransfers;

public partial class AzureBlobViewModel(IHttpTransferManager manager) : ObservableObject
{
    [ObservableProperty] string connectionString = String.Empty;
    [ObservableProperty] string containerName = String.Empty;

    [RelayCommand]
    async Task QueueBlob()
    {
        var client = new BlobServiceClient(this.ConnectionString);
        var blobClient = client.GetBlobContainerClient(this.ContainerName);

        await blobClient.CreateIfNotExistsAsync();
        // if (!blobClient.CanGenerateSasUri)
        var uri = blobClient.GenerateSasUri(
            BlobContainerSasPermissions.Add | BlobContainerSasPermissions.Create, 
            DateTimeOffset.UtcNow.AddHours(1)
        );
        await manager.Queue(new AzureBlobStorageUploadRequest("file")
            .WithCustomUri(uri.ToString())
            .Build()
        );
    }
}