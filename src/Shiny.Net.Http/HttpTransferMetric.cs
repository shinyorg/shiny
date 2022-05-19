using System;

namespace Shiny.Net.Http;

public record HttpTransferMetric(
    HttpTransfer Transfer,
    long BytesPerSecond = 0,
    TimeSpan? EstimatedTimeRemaining = null
);
