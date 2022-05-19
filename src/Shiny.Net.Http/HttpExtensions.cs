namespace Shiny.Net.Http;


public static class HttpExtensions
{
    public static bool IsCompleted(this HttpTransferState status)
    {
        switch (status)
        {
            case HttpTransferState.Completed:
            case HttpTransferState.Error:
            case HttpTransferState.Canceled:
                return true;

            default:
                return false;
        }
    }


    public static bool IsPaused(this HttpTransferState status)
    {
        switch (status)
        {
            case HttpTransferState.Paused:
            case HttpTransferState.PausedByCostedNetwork:
            case HttpTransferState.PausedByNoNetwork:
                return true;

            default:
                return false;
        }
    }
}
