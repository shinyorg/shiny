namespace Shiny.GameCenter;


public enum GameCenterErrorCode
{
    Unknown,

    /// <summary>No player is signed in to the game service</summary>
    NotAuthenticated,

    /// <summary>A network error stopped the service from completing the request - queued work is retried</summary>
    Network,

    /// <summary>
    /// The achievement or leaderboard id is not known to the service, or the operation does not fit it
    /// (incrementing a standard achievement). Queued work that fails this way is dropped rather than retried forever.
    /// </summary>
    InvalidId,

    /// <summary>
    /// The app is not set up for the service - Google: the signing certificate SHA-1 or the games APP_ID is missing
    /// from the Play Console / manifest; Apple: Game Center is not enabled for the app or the entitlement is missing
    /// </summary>
    NotConfigured,

    /// <summary>The game service is not available on this device</summary>
    Unavailable,

    /// <summary>No foreground activity / window is available to present the service's UI</summary>
    NoUserInterface
}


public class GameCenterException(GameCenterErrorCode errorCode, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public GameCenterErrorCode ErrorCode { get; } = errorCode;

    /// <summary>Raw platform error code (Google status code / GKError) when available</summary>
    public string? NativeErrorCode { get; init; }
}
