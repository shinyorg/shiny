namespace Shiny.Wearables;


/// <summary>Why a wearable operation failed.</summary>
public enum WearableErrorCode
{
    /// <summary>The platform has no wearable API.</summary>
    NotSupported,

    /// <summary>No wearable running the companion app can be reached right now.</summary>
    NotReachable,

    /// <summary>The platform refused or failed the operation.</summary>
    Failed
}


/// <summary>A wearable operation failed; <see cref="Code"/> says why.</summary>
public class WearableException(WearableErrorCode code, string message, Exception? inner = null) : Exception(message, inner)
{
    /// <summary>Why it failed.</summary>
    public WearableErrorCode Code { get; } = code;
}
