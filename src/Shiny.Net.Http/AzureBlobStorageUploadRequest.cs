using System;
using System.Collections.Generic;
using System.IO;

namespace Shiny.Net.Http;


// https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob?tabs=microsoft-entra-id
/// <summary>
/// Fluent builder that produces an <see cref="HttpTransferRequest"/> for uploading
/// a local file to an Azure Blob Storage container.
/// </summary>
public class AzureBlobStorageUploadRequest(string localFilePath)
{
    /// <summary>Gets the path of the local file to upload.</summary>
    public string LocalFilePath => localFilePath;

    /// <summary>Optional transfer identifier. A GUID is generated if not provided.</summary>
    public string? Identifier { get; set; }

    /// <summary>The destination container URI (without the blob name).</summary>
    public string? Uri { get; set; }

    /// <summary>Allow the transfer to run on a metered connection.</summary>
    public bool UseMeteredConnection { get; set; }

    /// <summary>Shared key used to populate an Authorization header.</summary>
    public string? SharedAuthorizationKey { get; set; }

    /// <summary>Optional x-ms-version date.</summary>
    public DateTimeOffset? AuthVersion { get; set; }

    /// <summary>Optional x-ms-date authorization date.</summary>
    public DateTimeOffset? AuthDate { get; set; }

    /// <summary>Optional remote blob file name; defaults to the local file name.</summary>
    public string? RemoteFileName { get; set; }

    /// <summary>Optional SAS token appended to the URI for authentication.</summary>
    public string? SasToken { get; set; }

    /// <summary>Additional HTTP headers attached to the upload request.</summary>
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>
    /// Clock source used to stamp default <c>x-ms-date</c> and <c>x-ms-version</c> values. Defaults
    /// to <see cref="TimeProvider.System"/>; tests can substitute a deterministic provider.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;


    // public async Task<AzureBlobStorageUploadRequest> WithSasTokenRequest(CancellationToken cancellationToken = default)
    // {
    //     this.SasToken =
    //     return this;
    // }

    /// <summary>Sets the destination URI from a tenant name and container name.</summary>
    public AzureBlobStorageUploadRequest WithBlobContainer(string tenant, string containerName)
    {
        this.Uri = $"https://{tenant}.blob.core.windows.net/{containerName}";
        return this;
    }
    

    /// <summary>Uses a custom container URI instead of constructing one from tenant + container.</summary>
    public AzureBlobStorageUploadRequest WithCustomUri(string uri)
    {
        this.Uri = uri;
        return this;
    }


    /// <summary>Allows the transfer to run over a metered connection.</summary>
    public AzureBlobStorageUploadRequest WithMeteredConnection()
    {
        this.UseMeteredConnection = true;
        return this;
    }


    /// <summary>Adds a custom HTTP header to the upload request.</summary>
    public AzureBlobStorageUploadRequest WithHeader(string key, string value)
    {
        this.Headers.Add(key, value);
        return this;
    }

    /// <summary>Sets the blob name that the uploaded file is stored under.</summary>
    public AzureBlobStorageUploadRequest WithRemoteFileName(string fileName)
    {
        this.RemoteFileName = fileName;
        return this;
    }

    /// <summary>Authenticates the upload using a SAS token query string.</summary>
    public AzureBlobStorageUploadRequest WithSasToken(string sasToken)
    {
        this.SasToken = sasToken;
        return this;
    }

    /// <summary>Authenticates the upload using an Azure Storage shared key.</summary>
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

    /// <summary>
    /// Builds an <see cref="HttpTransferRequest"/> that uploads the local file to the configured blob URI.
    /// </summary>
    public HttpTransferRequest Build()
    {
        if (!System.Uri.TryCreate(this.Uri, UriKind.Absolute, out _))
            throw new InvalidOperationException("Invalid URI - Use WithBlobContainer or WithCustomUri");

        var uri = this.Uri;
        this.Identifier ??= Guid.NewGuid().ToString();
        this.Headers.Add("Content-Length", new FileInfo(this.LocalFilePath).Length.ToString());
        this.Headers.Add("x-ms-blob-type", "BlockBlob");
        
        var fileName = Path.GetFileName(this.LocalFilePath);
        var fn = this.RemoteFileName ?? fileName;
        this.Headers.Add("Content-Disposition", $"attachment; filename=\"{fn}\"");
        uri += "/" + fn;

        if (this.SasToken != null)
        {
            uri += $"?{this.SasToken}";
        }
        else if (this.SharedAuthorizationKey != null)
        {
            var now = this.TimeProvider.GetUtcNow();
            this.Headers.Add("Authorization", $"SharedKey {this.SharedAuthorizationKey}");
            this.Headers.Add("x-ms-date", (this.AuthDate ?? now).ToString(DATE_FORMAT));
            this.Headers.Add("x-ms-version", (this.AuthVersion ?? now).ToString(DATE_FORMAT));
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