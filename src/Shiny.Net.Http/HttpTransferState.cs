using System;


namespace Shiny.Net.Http
{
    public enum HttpTransferState
    {
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
