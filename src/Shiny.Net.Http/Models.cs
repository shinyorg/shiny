using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Net.Http;


/// <summary>
/// Describes a queued background HTTP upload or download.
/// </summary>
/// <param name="Identifier">Unique identifier for the transfer.</param>
/// <param name="Uri">The remote endpoint URI.</param>
/// <param name="Type">Indicates whether this is an upload (multipart/raw) or a download.</param>
/// <param name="LocalFilePath">Path to the local file to upload or to write a download into.</param>
/// <param name="UseMeteredConnection">When true, the platform may run the transfer over a metered (e.g. cellular) connection.</param>
/// <param name="HttpContent">Optional additional content (form fields, JSON body) sent with the request.</param>
/// <param name="Headers">Optional HTTP headers to include with the request.</param>
public record HttpTransferRequest(
    string Identifier,
    string Uri,
    TransferType Type,
    string LocalFilePath,
    bool UseMeteredConnection = true,
    TransferHttpContent? HttpContent = null,
    IDictionary<string, string>? Headers = null
)
{
    /// <summary>
    /// Optional HTTP method override. Defaults to GET for downloads and POST for uploads.
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Form field name to use for the file part when sending as multipart. Defaults to "file".
    /// </summary>
    public string FileFormDataName { get; set; } = "file";

    //public void SetAuthHeader(string authType, string authValue)
    //    this.Headers.Add("Authentication", $"{authType} {authValue}");

    /// <summary>
    /// Returns the resolved <see cref="System.Net.Http.HttpMethod"/> for this request,
    /// falling back to GET for downloads and POST for uploads.
    /// </summary>
    public HttpMethod GetHttpMethod()
    {
        var defMethod = this.Type == TransferType.Download ? "GET" : "POST";
        var httpMethod = new HttpMethod(this.HttpMethod ?? defMethod);

        return httpMethod;
    }
}

/// <summary>
/// Identifies the kind of HTTP transfer.
/// </summary>
public enum TransferType
{
    /// <summary>Upload sent as multipart/form-data.</summary>
    UploadMultipart,
    /// <summary>Upload sent as a raw request body.</summary>
    UploadRaw,
    /// <summary>Download from a remote endpoint into a local file.</summary>
    Download
}

/// <summary>
/// Persistent record of a queued or in-flight HTTP transfer.
/// </summary>
/// <param name="Request">The original transfer request.</param>
/// <param name="BytesToTransfer">Total bytes expected for the transfer, when known.</param>
/// <param name="BytesTransferred">Bytes transferred so far.</param>
/// <param name="Status">The current state of the transfer.</param>
/// <param name="CreatedAt">Timestamp when the transfer was queued.</param>
public record HttpTransfer(
    HttpTransferRequest Request,
    long? BytesToTransfer,
    long BytesTransferred,
    HttpTransferState Status,
    DateTimeOffset CreatedAt
) : IRepositoryEntity
{
    /// <inheritdoc />
    public string Identifier => this.Request.Identifier;
};

/// <summary>
/// A snapshot emitted whenever a transfer's progress or status changes.
/// </summary>
/// <param name="Request">The transfer request this update relates to.</param>
/// <param name="Status">The current transfer state.</param>
/// <param name="Progress">Current progress information.</param>
/// <param name="Exception">Exception, if the transfer is in an error state.</param>
public record HttpTransferResult(
    HttpTransferRequest Request,
    HttpTransferState Status,
    TransferProgress Progress,
    Exception? Exception
)
{
    /// <summary>
    /// Gets a value indicating whether the total transfer size is known.
    /// </summary>
    public bool IsDeterministic => this.Progress.BytesToTransfer != null;
};


/// <summary>
/// Represents the progress of an in-flight HTTP transfer.
/// </summary>
/// <param name="BytesPerSecond">Estimated current throughput in bytes per second.</param>
/// <param name="BytesToTransfer">Total bytes expected, or null when unknown.</param>
/// <param name="BytesTransferred">Bytes transferred so far.</param>
public record TransferProgress(
    long BytesPerSecond,
    long? BytesToTransfer,
    long BytesTransferred
)
{
    /// <summary>
    /// Gets an empty progress instance (zero bytes, zero throughput).
    /// </summary>
    public static TransferProgress Empty { get; } = new(0, 0, 0);

    /// <summary>
    /// Gets a value indicating whether the total transfer size is known.
    /// </summary>
    public bool IsDeterministic => this.BytesToTransfer != null;

    double? percentComplete;

    /// <summary>
    /// Gets the percent complete as a value between 0.0 and 1.0, or -1 when not deterministic.
    /// </summary>
    public double PercentComplete
    {
        get
        {
            if (!this.IsDeterministic)
                return -1;

            if (this.percentComplete == null)
            {
                if (this.BytesToTransfer == null)
                {
                    this.percentComplete = -1;
                }
                else
                {
                    this.percentComplete = Math.Round((double)this.BytesTransferred / this.BytesToTransfer!.Value, 2);
                }
            }
            return this.percentComplete.Value;
        }
    }


    TimeSpan? estimate;

    /// <summary>
    /// Gets the estimated time remaining based on current throughput, or <see cref="TimeSpan.Zero"/> when unknown.
    /// </summary>
    public TimeSpan EstimatedTimeRemaining
    {
        get
        {
            if (this.estimate == null)
            {
                if (this.BytesToTransfer == null || this.BytesPerSecond == 0)
                {
                    this.estimate = TimeSpan.Zero;
                }
                else
                {
                    var bytesRemaining = this.BytesToTransfer.Value - this.BytesTransferred;
                    this.estimate = TimeSpan.FromSeconds(bytesRemaining / this.BytesPerSecond);
                }
            }
            return this.estimate!.Value;
        }
    }
};


/// <summary>
/// Body content attached to an upload transfer request.
/// </summary>
/// <param name="Content">The literal string content to send.</param>
/// <param name="ContentType">The MIME content type. Defaults to text/plain.</param>
/// <param name="Encoding">The character encoding webname. Defaults to utf-8.</param>
public record TransferHttpContent(
    string Content,
    string ContentType = "text/plain",
    string Encoding = "utf-8"
)
{
    /// <summary>
    /// Optional form-data field name when the parent request is sent as multipart.
    /// </summary>
    public string? ContentFormDataName { get; set; }

    /// <summary>
    /// Creates a JSON-encoded <see cref="TransferHttpContent"/> from an object.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="jsonOptions">Optional serializer options.</param>
    public static TransferHttpContent FromJson(object obj, JsonSerializerOptions? jsonOptions = null)
    {
        var json = JsonSerializer.Serialize(obj, jsonOptions);
        return new TransferHttpContent(
            json,
            "application/json",
            "utf-8"
        );
    }

    /// <summary>
    /// Creates a URL-encoded form data <see cref="TransferHttpContent"/> from key/value pairs.
    /// </summary>
    /// <param name="formValues">The form fields to send.</param>
    public static TransferHttpContent FromFormData(params (string Key,string Value)[] formValues)
    {
        var list = new List<KeyValuePair<string, string>>();
        foreach (var fv in formValues)
            list.Add(new KeyValuePair<string, string>(fv.Key, fv.Value));

        return FromFormData(list);
    }


    /// <summary>
    /// Creates a URL-encoded form data <see cref="TransferHttpContent"/> from a dictionary.
    /// </summary>
    /// <param name="dictionary">The form fields to send.</param>
    public static TransferHttpContent FromFormData(IDictionary<string, string> dictionary)
    {
        var list = new List<KeyValuePair<string, string>>();
        foreach (var fv in dictionary)
            list.Add(new KeyValuePair<string, string>(fv.Key, fv.Value));

        return FromFormData(list);
    }


    static TransferHttpContent FromFormData(List<KeyValuePair<string, string>> list)
    {
        var form = new FormUrlEncodedContent(list);
        var encoded = form.ToString()!;

        // TODO: ensure URL encoded
        return new TransferHttpContent(
            encoded,
            "application/x-www-form-urlencoded"
        );
    }
}


/// <summary>
/// Lifecycle state of an HTTP transfer.
/// </summary>
public enum HttpTransferState
{
    /// <summary>The state could not be determined.</summary>
    Unknown,
    /// <summary>The transfer is queued but has not begun.</summary>
    Pending,
    /// <summary>The transfer is paused.</summary>
    Paused,
    /// <summary>The transfer is paused because no network is available.</summary>
    PausedByNoNetwork,
    /// <summary>The transfer is paused because only a metered connection is available and that is disallowed.</summary>
    PausedByCostedNetwork,
    /// <summary>The transfer is actively running.</summary>
    InProgress,
    /// <summary>The transfer ended in an error.</summary>
    Error,
    /// <summary>The transfer was cancelled.</summary>
    Canceled,
    /// <summary>The transfer completed successfully.</summary>
    Completed
}
