using System;
using System.Collections.Generic;
using System.IO;

namespace Shiny.Net.Http;


// https://tus.io/protocols/resumable-upload
/// <summary>
/// Fluent builder that produces an <see cref="HttpTransferRequest"/> for a resumable upload to a
/// tus 1.0.0 server. The transfer creates the upload, then sends the file with <c>PATCH</c> requests;
/// after a pause, network drop, or app restart it asks the server for its offset and continues from there.
/// </summary>
public class TusUploadRequest(string localFilePath)
{
    /// <summary>Gets the path of the local file to upload.</summary>
    public string LocalFilePath => localFilePath;

    /// <summary>Optional transfer identifier. A GUID is generated if not provided.</summary>
    public string? Identifier { get; set; }

    /// <summary>The server's tus creation endpoint (e.g. <c>https://tusd.example.com/files/</c>).</summary>
    public string? Endpoint { get; set; }

    /// <summary>Allow the transfer to run on a metered (e.g. cellular) connection.</summary>
    public bool UseMeteredConnection { get; set; }

    /// <summary>
    /// Maximum bytes per <c>PATCH</c>. Null (the default) sends the rest of the file in one request.
    /// </summary>
    public long? ChunkSize { get; set; }

    /// <summary>
    /// When true (the default), the local file name is sent as the <c>filename</c> metadata entry
    /// unless one is set explicitly.
    /// </summary>
    public bool IncludeFileName { get; set; } = true;

    /// <summary>Metadata sent in the <c>Upload-Metadata</c> header when the upload is created.</summary>
    public Dictionary<string, string> Metadata { get; } = new();

    /// <summary>Additional HTTP headers (such as authorization) sent with every tus request.</summary>
    public Dictionary<string, string> Headers { get; } = new();


    /// <summary>Sets the server's tus creation endpoint.</summary>
    public TusUploadRequest WithEndpoint(string endpoint)
    {
        this.Endpoint = endpoint;
        return this;
    }


    /// <summary>Adds an <c>Upload-Metadata</c> entry. Keys must not contain spaces or commas.</summary>
    public TusUploadRequest WithMetadata(string key, string value)
    {
        this.Metadata[key] = value;
        return this;
    }


    /// <summary>Limits each <c>PATCH</c> to <paramref name="bytes"/>.</summary>
    public TusUploadRequest WithChunkSize(long bytes)
    {
        if (bytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "Chunk size must be greater than zero");

        this.ChunkSize = bytes;
        return this;
    }


    /// <summary>Stops the local file name being sent as <c>filename</c> metadata.</summary>
    public TusUploadRequest WithoutFileName()
    {
        this.IncludeFileName = false;
        return this;
    }


    /// <summary>Allows the transfer to run over a metered connection.</summary>
    public TusUploadRequest WithMeteredConnection()
    {
        this.UseMeteredConnection = true;
        return this;
    }


    /// <summary>Adds a custom HTTP header sent with every tus request.</summary>
    public TusUploadRequest WithHeader(string key, string value)
    {
        this.Headers[key] = value;
        return this;
    }


    /// <summary>Sets an <c>Authorization: Bearer</c> header sent with every tus request.</summary>
    public TusUploadRequest WithBearerToken(string token)
        => this.WithHeader("Authorization", $"Bearer {token}");


    /// <summary>
    /// Builds an <see cref="HttpTransferRequest"/> of type <see cref="TransferType.UploadTus"/>.
    /// </summary>
    public HttpTransferRequest Build()
    {
        if (!Uri.TryCreate(this.Endpoint, UriKind.Absolute, out _))
            throw new InvalidOperationException("Invalid endpoint - use WithEndpoint()");

        if (!File.Exists(this.LocalFilePath))
            throw new FileNotFoundException("Local file not found", this.LocalFilePath);

        this.Identifier ??= Guid.NewGuid().ToString();

        var metadata = new Dictionary<string, string>(this.Metadata);
        if (this.IncludeFileName)
            metadata.TryAdd("filename", Path.GetFileName(this.LocalFilePath));

        return new HttpTransferRequest(
            this.Identifier,
            this.Endpoint!,
            TransferType.UploadTus,
            this.LocalFilePath,
            this.UseMeteredConnection,
            null,
            this.Headers
        )
        {
            HttpMethod = "POST",
            TusMetadata = metadata,
            TusChunkSize = this.ChunkSize
        };
    }
}
