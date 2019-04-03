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
        //Resumed,
        Retrying,
        Error,
        Cancelled,
        Completed
    }
}
