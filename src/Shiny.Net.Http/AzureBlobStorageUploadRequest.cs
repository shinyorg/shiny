using System;
using System.Collections.Generic;
using System.IO;

namespace Shiny.Net.Http;


// https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob?tabs=microsoft-entra-id
public class AzureBlobStorageUploadRequest(string localFilePath)
{
    public string LocalFilePath => localFilePath;
    public string? Identifier { get; set; }
    public string? Uri { get; set; }
    public bool UseMeteredConnection { get; set; }
    public string? SharedAuthorizationKey { get; set; }
    public DateTimeOffset? AuthVersion { get; set; }
    public DateTimeOffset? AuthDate { get; set; }
    
    public string? SasToken { get; set; }
    public Dictionary<string, string> Headers { get; } = new();


    // public async Task<AzureBlobStorageUploadRequest> WithSasTokenRequest(CancellationToken cancellationToken = default)
    // {
    //     this.SasToken = 
    //     return this;
    // }
    
    public AzureBlobStorageUploadRequest WithBlobContainer(string tenant, string containerName)
    {
        this.Uri = $"https://{tenant}.blob.core.windows.net/{containerName}";
        return this;
    }
    

    public AzureBlobStorageUploadRequest WithCustomUri(string uri)
    {
        this.Uri = uri;
        return this;
    }

    
    public AzureBlobStorageUploadRequest WithMeteredConnection()
    {
        this.UseMeteredConnection = true;
        return this;
    }


    public AzureBlobStorageUploadRequest WithHeader(string key, string value)
    {
        this.Headers.Add(key, value);
        return this;
    }


    public AzureBlobStorageUploadRequest WithSasToken(string sasToken)
    {
        this.SasToken = sasToken;
        return this;
    }

    public AzureBlobStorageUploadRequest WithSharedKeyAuthorization(
        string sharedKey,
        DateTimeOffset? Version = null,
        DateTimeOffset? AuthDate = null
    )
    {
        this.SharedAuthorizationKey = sharedKey;
        this.AuthVersion = Version;
        this.AuthDate = AuthDate;
        return this;
    }

    
    const string DATE_FORMAT = "yyyy-MM-dd";
    
    public HttpTransferRequest Build()
    {
        if (!System.Uri.TryCreate(this.Uri, UriKind.Absolute, out _))
            throw new InvalidOperationException("Invalid URI - Use WithBlobContainer or WithCustomUri");

        var uri = this.Uri;
        this.Identifier ??= Guid.NewGuid().ToString();
        this.Headers.Add("Content-Length", new FileInfo(this.LocalFilePath).Length.ToString());
        this.Headers.Add("x-ms-blob-type", "BlockBlob");
        
        var fileName = Path.GetFileName(this.LocalFilePath);
        this.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        if (this.SasToken != null)
        {
            uri += $"?{this.SasToken}";
        }
        else if (this.SharedAuthorizationKey != null)
        {
            this.Headers.Add("Authorization", $"SharedKey {this.SharedAuthorizationKey}");
            this.Headers.Add("x-ms-date", (this.AuthDate ?? DateTimeOffset.UtcNow).ToString(DATE_FORMAT));
            this.Headers.Add("x-ms-version", (this.AuthVersion ?? DateTimeOffset.UtcNow).ToString(DATE_FORMAT));
        }
        
        return new HttpTransferRequest(
            this.Identifier,
            uri,
            TransferType.UploadRaw,
            this.LocalFilePath,
            this.UseMeteredConnection,
            null,
            this.Headers
        )
        {
            HttpMethod = "PUT"
        };
    }
}