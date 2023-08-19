using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public record HttpTransferRequest(
    string Identifier,
    string Uri,
    bool IsUpload,
    string LocalFilePath,
    bool UseMeteredConnection = true,
    TransferHttpContent? HttpContent = null,
    IDictionary<string, string>? Headers = null
)
{
    public string? HttpMethod { get; set; }
    public string FileFormDataName { get; set; } = "file";

    //public void SetAuthHeader(string authType, string authValue)
    //    this.Headers.Add("Authentication", $"{authType} {authValue}");

    public HttpMethod GetHttpMethod()
    {
        var defMethod = this.IsUpload ? "POST" : "GET";
        var httpMethod = new HttpMethod(this.HttpMethod ?? defMethod);

        return httpMethod;
    }
}


public record HttpTransfer(
    HttpTransferRequest Request,
    long? BytesToTransfer,
    long BytesTransferred,
    HttpTransferState Status,
    DateTimeOffset CreatedAt
) : IRepositoryEntity
{
    public string Identifier => this.Request.Identifier;    
};

public record HttpTransferResult(
    HttpTransferRequest Request,
    HttpTransferState Status,
    TransferProgress Progress,
    Exception? Exception
)
{
    public bool IsDeterministic => this.Progress.BytesToTransfer != null;
};


public record TransferProgress(    
    long BytesPerSecond,
    long? BytesToTransfer,
    long BytesTransferred
)
{
    public static TransferProgress Empty { get; } = new(0, 0, 0);
    public bool IsDeterministic => this.BytesToTransfer != null;

    double? percentComplete;
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
/// How to send POST/PUT args to a transfer request
/// </summary>
/// <param name="Content">You actual string content</param>
/// <param name="ContentType">The content type</param>
/// <param name="Encoding">Defaults to utf-8, should be the webname of the encodings</param>
/// <param name="ContentName">This is to match binding names for things like json binding - the name should be small & simple (no spaces or weird characters) to prevent screwing up protocol</param>
public record TransferHttpContent(
    string Content,
    string ContentType = "text/plain",
    string Encoding = "utf-8"
)
{
    public string? ContentFormDataName { get; set; }

    public static TransferHttpContent FromJson(object obj, JsonSerializerOptions? jsonOptions = null)
    {
        var json = JsonSerializer.Serialize(obj, jsonOptions);
        return new TransferHttpContent(
            json,
            "application/json",
            "utf-8"
        );
    }

    public static TransferHttpContent FromFormData(params (string Key,string Value)[] formValues)
    {
        var list = new List<KeyValuePair<string, string>>();
        foreach (var fv in formValues)
            list.Add(new KeyValuePair<string, string>(fv.Key, fv.Value));

        return FromFormData(list);
    }


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


public enum HttpTransferState
{
    Unknown,
    Pending,
    Paused,
    PausedByNoNetwork,
    PausedByCostedNetwork,
    InProgress,
    Error,
    Canceled,
    Completed
}