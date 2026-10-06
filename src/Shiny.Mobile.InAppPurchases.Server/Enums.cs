using System.Text.Json.Serialization;

namespace Shiny.InAppPurchases.Server;


[JsonConverter(typeof(JsonStringEnumConverter<StorePlatform>))]
public enum StorePlatform
{
    AppStore,
    GooglePlay
}


[JsonConverter(typeof(JsonStringEnumConverter<StoreEnvironment>))]
public enum StoreEnvironment
{
    Unknown,
    Production,
    Sandbox,

    /// <summary>Apple: StoreKit configuration file testing in Xcode (not Apple-signed, never accepted by the server)</summary>
    Xcode,

    /// <summary>Apple: local StoreKit testing (not Apple-signed, never accepted by the server)</summary>
    LocalTesting
}


[JsonConverter(typeof(JsonStringEnumConverter<PurchaseKind>))]
public enum PurchaseKind
{
    OneTime,
    Subscription
}


/// <summary>
/// Normalized store notification type. The exact store value is always available in <see cref="PurchaseEvent.RawType"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PurchaseEventType>))]
public enum PurchaseEventType
{
    /// <summary>New purchase or subscription (Apple SUBSCRIBED / ONE_TIME_CHARGE, Google *_PURCHASED)</summary>
    Purchased,

    /// <summary>Subscription renewed successfully</summary>
    Renewed,

    /// <summary>Renewal failed and the subscription entered billing retry (no grace period)</summary>
    RenewalFailed,

    /// <summary>Renewal failed but the user keeps access during a billing grace period</summary>
    GracePeriodStarted,

    /// <summary>Billing grace period ended without recovery</summary>
    GracePeriodExpired,

    /// <summary>User turned off auto-renew / cancelled - access continues until expiry</summary>
    AutoRenewDisabled,

    /// <summary>User turned auto-renew back on</summary>
    AutoRenewEnabled,

    /// <summary>Upgrade, downgrade or crossgrade within a subscription group / items changed</summary>
    PlanChanged,

    /// <summary>Subscription expired - remove access</summary>
    Expired,

    /// <summary>Purchase refunded or voided - remove access</summary>
    Refunded,

    /// <summary>Apple declined a refund request</summary>
    RefundDeclined,

    /// <summary>A previous refund was reversed - restore access</summary>
    RefundReversed,

    /// <summary>Access revoked (Family Sharing removed, Google revoke) - remove access</summary>
    Revoked,

    /// <summary>Google: subscription paused</summary>
    Paused,

    /// <summary>Google: subscription in account hold - remove access until recovered</summary>
    OnHold,

    /// <summary>Recovered from billing retry / account hold / pause</summary>
    Recovered,

    /// <summary>Google: user restored a cancelled subscription before it expired</summary>
    Restarted,

    /// <summary>Price increase / price change notification</summary>
    PriceChange,

    /// <summary>A pending purchase was cancelled before payment completed</summary>
    PendingCanceled,

    /// <summary>Apple: customer requested a refund for a consumable - respond with consumption info within 12 hours</summary>
    ConsumptionRequest,

    /// <summary>Apple: an offer code or promotional offer was redeemed</summary>
    OfferRedeemed,

    /// <summary>Test notification requested from App Store Connect / Play Console</summary>
    Test,

    /// <summary>Anything else - inspect <see cref="PurchaseEvent.RawType"/></summary>
    Other
}
