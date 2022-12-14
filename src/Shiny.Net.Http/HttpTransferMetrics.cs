using System;

namespace Shiny.Net.Http;


public record HttpTransferMetrics(
    TimeSpan EstimatedTimeRemaining,
    long BytesPerSecond,
    long BytesToTransfer,
    long BytesTransferred,
    double PercentComplete,
    HttpTransferState Status
);