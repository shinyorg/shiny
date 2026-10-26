namespace Shiny.AgeSignals;


public enum AgeSignalsErrorReason
{
    Unknown,

    /// <summary>The store can't provide an age range on this device or account (Apple <c>notAvailable</c>, Play
    /// <c>API_NOT_AVAILABLE</c>).</summary>
    NotAvailable,

    /// <summary>The account can't share an age range (Apple <c>invalidAccount</c>).</summary>
    InvalidAccount,

    /// <summary>A network failure - retry later.</summary>
    Network,

    /// <summary>A transient store error - retry later (Play <c>CLIENT_TRANSIENT_ERROR</c>, <c>CANNOT_BIND_TO_SERVICE</c>).</summary>
    Transient,

    /// <summary>The Play Store or Play services is missing or too old, or the library needs updating.</summary>
    StoreUnavailable,

    /// <summary>Google Play: the app was not installed from Play.</summary>
    AppNotOwned,

    /// <summary>There is no window or activity to present the request from.</summary>
    NoUserInterface
}


public class AgeSignalsException(AgeSignalsErrorReason reason, string message, int? nativeCode = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public AgeSignalsErrorReason Reason => reason;

    /// <summary>The Play <c>AgeSignalsErrorCode</c>, where there is one.</summary>
    public int? NativeCode => nativeCode;
}
