namespace Shiny.InAppPurchases;


public enum InAppPurchaseErrorCode
{
    Unknown,

    /// <summary>The store or billing service is not available on this device (Play Store missing, billing unsupported)</summary>
    StoreUnavailable,

    /// <summary>The user is not allowed to make payments (parental controls, MDM, unsupported country)</summary>
    NotAllowed,

    /// <summary>The product id was not found in the store (check configuration, agreements and tax/banking status)</summary>
    ProductNotFound,

    /// <summary>The product exists but cannot be purchased right now</summary>
    ProductUnavailable,

    /// <summary>A network error prevented the store from completing the request</summary>
    Network,

    /// <summary>Invalid arguments supplied by the app (Google DEVELOPER_ERROR, Apple invalid offer)</summary>
    DeveloperError,

    /// <summary>The store signature on the transaction could not be verified on device</summary>
    VerificationFailed,

    /// <summary>No foreground activity/window scene is available to present the purchase UI</summary>
    NoUserInterface,

    /// <summary>The purchase is not in a state that allows the operation (e.g. finishing a pending purchase)</summary>
    InvalidState
}


public class InAppPurchaseException(InAppPurchaseErrorCode errorCode, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public InAppPurchaseErrorCode ErrorCode { get; } = errorCode;

    /// <summary>Raw platform error code (Google BillingResponseCode / StoreKit error) when available</summary>
    public string? NativeErrorCode { get; init; }
}
