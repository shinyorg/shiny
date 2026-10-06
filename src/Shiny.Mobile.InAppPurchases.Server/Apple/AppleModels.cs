using System.Text.Json;
using System.Text.Json.Serialization;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server;


/// <summary>
/// App Store Server Notifications V2 responseBodyV2DecodedPayload
/// </summary>
public sealed class AppleNotificationPayload
{
    public string NotificationType { get; set; } = String.Empty;
    public string? Subtype { get; set; }

    [JsonPropertyName("notificationUUID")]
    public string NotificationUuid { get; set; } = String.Empty;

    public string? Version { get; set; }

    /// <summary>Unix milliseconds</summary>
    public long? SignedDate { get; set; }

    public AppleNotificationData? Data { get; set; }
    public AppleNotificationSummary? Summary { get; set; }
    public AppleExternalPurchaseToken? ExternalPurchaseToken { get; set; }
    public JsonElement? AppData { get; set; }

    [JsonIgnore]
    public DateTimeOffset? SignedAt => InAppPurchaseUtils.FromUnixMilliseconds(this.SignedDate);
}


public sealed class AppleNotificationData
{
    public long? AppAppleId { get; set; }
    public string? BundleId { get; set; }
    public string? BundleVersion { get; set; }
    public string? Environment { get; set; }
    public string? SignedTransactionInfo { get; set; }
    public string? SignedRenewalInfo { get; set; }

    /// <summary>1 active, 2 expired, 3 billing retry, 4 grace period, 5 revoked</summary>
    public int? Status { get; set; }

    public string? ConsumptionRequestReason { get; set; }
}


/// <summary>Sent with RENEWAL_EXTENSION/SUMMARY notifications instead of data</summary>
public sealed class AppleNotificationSummary
{
    public string? RequestIdentifier { get; set; }
    public string? Environment { get; set; }
    public long? AppAppleId { get; set; }
    public string? BundleId { get; set; }
    public string? ProductId { get; set; }
    public List<string>? StorefrontCountryCodes { get; set; }
    public long? SucceededCount { get; set; }
    public long? FailedCount { get; set; }
}


/// <summary>Sent with EXTERNAL_PURCHASE_TOKEN notifications instead of data</summary>
public sealed class AppleExternalPurchaseToken
{
    public string? ExternalPurchaseId { get; set; }
    public long? TokenCreationDate { get; set; }
    public long? AppAppleId { get; set; }
    public string? BundleId { get; set; }
}


/// <summary>
/// JWSTransactionDecodedPayload - dates are Unix milliseconds, prices are in milliunits
/// </summary>
public sealed class AppleTransaction
{
    public string? AppAccountToken { get; set; }
    public string? AppTransactionId { get; set; }
    public string? BundleId { get; set; }
    public string? Currency { get; set; }
    public string? Environment { get; set; }
    public long? ExpiresDate { get; set; }

    /// <summary>PURCHASED or FAMILY_SHARED</summary>
    public string? InAppOwnershipType { get; set; }

    public bool? IsUpgraded { get; set; }
    public string? OfferDiscountType { get; set; }
    public string? OfferIdentifier { get; set; }
    public string? OfferPeriod { get; set; }
    public int? OfferType { get; set; }
    public long? OriginalPurchaseDate { get; set; }
    public string? OriginalTransactionId { get; set; }

    /// <summary>Price in milliunits of <see cref="Currency"/> (1990 = 1.99)</summary>
    public long? Price { get; set; }

    public string? ProductId { get; set; }
    public long? PurchaseDate { get; set; }
    public int? Quantity { get; set; }
    public long? RevocationDate { get; set; }
    public int? RevocationPercentage { get; set; }

    /// <summary>0 = other, 1 = app issue</summary>
    public int? RevocationReason { get; set; }

    public string? RevocationType { get; set; }
    public long? SignedDate { get; set; }
    public string? Storefront { get; set; }
    public string? StorefrontId { get; set; }
    public string? SubscriptionGroupIdentifier { get; set; }
    public string? TransactionId { get; set; }

    /// <summary>PURCHASE or RENEWAL</summary>
    public string? TransactionReason { get; set; }

    /// <summary>Auto-Renewable Subscription, Non-Consumable, Consumable, Non-Renewing Subscription</summary>
    public string? Type { get; set; }

    public string? WebOrderLineItemId { get; set; }

    [JsonIgnore] public DateTimeOffset? PurchasedAt => InAppPurchaseUtils.FromUnixMilliseconds(this.PurchaseDate);
    [JsonIgnore] public DateTimeOffset? ExpiresAt => InAppPurchaseUtils.FromUnixMilliseconds(this.ExpiresDate);
    [JsonIgnore] public DateTimeOffset? RevokedAt => InAppPurchaseUtils.FromUnixMilliseconds(this.RevocationDate);
    [JsonIgnore] public bool IsSubscription => this.Type == "Auto-Renewable Subscription";
}


/// <summary>
/// JWSRenewalInfoDecodedPayload - dates are Unix milliseconds, prices are in milliunits
/// </summary>
public sealed class AppleRenewalInfo
{
    public string? AppAccountToken { get; set; }
    public string? AppTransactionId { get; set; }
    public string? AutoRenewProductId { get; set; }

    /// <summary>0 = off, 1 = on</summary>
    public int? AutoRenewStatus { get; set; }

    public string? Currency { get; set; }
    public List<string>? EligibleWinBackOfferIds { get; set; }
    public string? Environment { get; set; }

    /// <summary>1 cancelled, 2 billing error, 3 declined price increase, 4 product unavailable, 5 other</summary>
    public int? ExpirationIntent { get; set; }

    public long? GracePeriodExpiresDate { get; set; }
    public bool? IsInBillingRetryPeriod { get; set; }
    public string? OfferDiscountType { get; set; }
    public string? OfferIdentifier { get; set; }
    public string? OfferPeriod { get; set; }
    public int? OfferType { get; set; }
    public string? OriginalTransactionId { get; set; }
    public int? PriceIncreaseStatus { get; set; }
    public string? ProductId { get; set; }
    public long? RecentSubscriptionStartDate { get; set; }
    public long? RenewalDate { get; set; }
    public long? RenewalPrice { get; set; }
    public long? SignedDate { get; set; }

    [JsonIgnore] public bool IsAutoRenewing => this.AutoRenewStatus == 1;
    [JsonIgnore] public DateTimeOffset? RenewsAt => InAppPurchaseUtils.FromUnixMilliseconds(this.RenewalDate);
    [JsonIgnore] public DateTimeOffset? GracePeriodExpiresAt => InAppPurchaseUtils.FromUnixMilliseconds(this.GracePeriodExpiresDate);
}


public enum AppleSubscriptionState
{
    Unknown = 0,
    Active = 1,
    Expired = 2,
    BillingRetry = 3,
    GracePeriod = 4,
    Revoked = 5
}


/// <summary>A verified entry from Get All Subscription Statuses</summary>
public sealed record AppleSubscriptionStatus(
    string? SubscriptionGroupIdentifier,
    string? OriginalTransactionId,
    AppleSubscriptionState State,
    AppleTransaction Transaction,
    AppleRenewalInfo RenewalInfo
);


sealed class AppleSignedPayloadBody
{
    public string? SignedPayload { get; set; }
}


sealed class AppleTransactionInfoResponse
{
    public string? SignedTransactionInfo { get; set; }
}


sealed class AppleStatusResponse
{
    public string? Environment { get; set; }
    public string? BundleId { get; set; }
    public long? AppAppleId { get; set; }
    public List<AppleStatusGroup>? Data { get; set; }
}


sealed class AppleStatusGroup
{
    public string? SubscriptionGroupIdentifier { get; set; }
    public List<AppleLastTransaction>? LastTransactions { get; set; }
}


sealed class AppleLastTransaction
{
    public int Status { get; set; }
    public string? OriginalTransactionId { get; set; }
    public string? SignedTransactionInfo { get; set; }
    public string? SignedRenewalInfo { get; set; }
}


sealed class AppleTestNotificationResponse
{
    public string? TestNotificationToken { get; set; }
}


sealed class AppleApiError
{
    public long? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
