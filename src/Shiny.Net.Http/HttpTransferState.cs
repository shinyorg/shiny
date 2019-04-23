using System;


namespace Shiny.Net.Http
{
    public enum HttpTransferState
    {
        Unknown,
        Pending,
        Paused,
        PausedByNoNetwork,
        PausedByCostedNetwork,
        InProgress,
        Retrying,
        Error,
        Cancelled,
        Completed
    }
}
