using System;
using System.Collections.Generic;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public record HttpTransferRequest(
    string Identifier,
    string Uri,
    bool IsUpload,
    string LocalFilePath,
    bool UseMeteredConnection = true,
    string? PostData = null,
    string? HttpMethod = null,
    IDictionary<string, string>? Headers = null
);


public record HttpTransfer(
    HttpTransferRequest Request,
    long? BytesToTransfer,
    long BytesTransferred,
    HttpTransferState Status,
    DateTimeOffset CreatedAt
) : IRepositoryEntity
{
    public string Identifier => this.Request.Identifier;
    public bool IsDeterministic => this.BytesToTransfer != null;
};

public record HttpTransferResult(
    HttpTransferRequest Request,
    HttpTransferState Status,
    TransferProgress Progress
)
{
    public bool IsDeterministic => this.Progress.BytesToTransfer != null;
};


public record TransferProgress(    
    long BytesPerSecond,
    long? BytesToTransfer,
    long BytesTransferred,
    TimeSpan EstimatedTimeRemaining,
    double PercentComplete
)
{
    public bool IsDeterministic => this.BytesToTransfer != null;
};


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