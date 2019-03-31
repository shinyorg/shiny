using System;


namespace Shiny.Net.Http
{
    public enum HttpTransferState
    {
        Unknown,
        Paused,
        PausedByNoNetwork,
        PausedByCostedNetwork,
        Running,
        Resumed,
        Retrying,
        Error,
        Cancelled,
        Completed
    }
}
