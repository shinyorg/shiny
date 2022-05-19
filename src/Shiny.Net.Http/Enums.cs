namespace Shiny.Net.Http;


public enum DirectionFilter
{
    Both,
    Upload,
    Download
}


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
    Canceled,
    Completed
}
