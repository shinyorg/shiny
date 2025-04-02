using System.Diagnostics;
using System.Reflection;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny.Net.Http;

namespace Sample.HttpTransfers;

public partial class AzureBlobViewModel(
    ILogger<AzureBlobViewModel> logger,
    IHttpTransferManager manager, 
    IPageDialogService dialogs
) : ObservableObject, IPageLifecycleAware, IConfirmNavigationAsync
{
    [ObservableProperty] string connectionString = String.Empty;
    [ObservableProperty] string containerName = String.Empty;
    [ObservableProperty] string progressText = String.Empty;
    
    [RelayCommand]
    async Task QueueBlob()
    {
        try
        {
            
            var blobClient = new BlobContainerClient(this.ConnectionString, this.ContainerName.ToLower());
            await blobClient.CreateIfNotExistsAsync();
            if (!blobClient.CanGenerateSasUri)
                throw new InvalidOperationException("Can't generate sas");

            var file = await this.ResourceToTemp();
            var sas = new BlobSasBuilder
            {
                Identifier = Guid.NewGuid().ToString(),
                BlobName = Path.GetFileName(file),
                BlobContainerName = blobClient.Name
            };
            blobClient.GenerateSasUri(sas);
            
            var uri = blobClient.GenerateSasUri(
                BlobContainerSasPermissions.Add | BlobContainerSasPermissions.Create,
                DateTimeOffset.UtcNow.AddHours(1)
            );
            logger.LogInformation($"Queueing blob {uri}");
            
            
            var newTransfer = await manager.Queue(new AzureBlobStorageUploadRequest(file)
                .WithCustomUri(uri.ToString())
                .Build()
            );
            
            this.uploadSub = manager
                .WatchTransfer(newTransfer.Identifier)
                .SubOnMainThread(
                    x => this.ProgressText = $"({x.Progress.PercentComplete}%) {x.Progress.EstimatedTimeRemaining}",
                    ex => logger.LogError(ex, "Error uploading blob")
                );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to queue azure blob"); 
        }
    }


    async Task<string> ResourceToTemp()
    {
        var ass = Assembly.GetExecutingAssembly(); 
        await using var resource = ass.GetManifestResourceStream("Sample.HttpTransfers.funny.jpeg");
        
        var file = Path.GetTempFileName();
        await using var fs = File.Create(file);
        await resource!.CopyToAsync(fs);

        return file;
    }


    IDisposable? uploadSub;
    public void OnAppearing() {}
    public void OnDisappearing() => this.uploadSub?.Dispose();
    

    public async Task<bool> CanNavigateAsync(INavigationParameters parameters)
    {
        var transfers = await manager.GetTransfers();
        if (transfers.Count == 0)
            return true;
     
        var confirm = await dialogs.DisplayAlertAsync("Cancel Upload", "Are you sure you want to cancel upload?", "Yes", "No");
        if (confirm)
        {
            await manager.CancelAll();
            return true;
        }
        return false;
    }
}