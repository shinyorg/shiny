using System.Text.Json;
using System.Text.Json.Serialization;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server;


/// <summary>
/// Google Play Real-time Developer Notification (the base64 data of the Pub/Sub message).
/// Exactly one of the notification properties is set.
/// </summary>
public sealed class GoogleDeveloperNotification
{
    public string? Version { get; set; }
    public string? PackageName { get; set; }
    public long? EventTimeMillis { get; set; }
    public GoogleSubscriptionNotification? SubscriptionNotification { get; set; }
    public GoogleOneTimeProductNotification? OneTimeProductNotification { get; set; }
    public GoogleVoidedPurchaseNotification? VoidedPurchaseNotification { get; set; }
    public GooglePendingRefundReviewNotification? PendingRefundReviewNotification { get; set; }
    public GoogleTestNotification? TestNotification { get; set; }

    [JsonIgnore]
    public DateTimeOffset? EventTime => InAppPurchaseUtils.FromUnixMilliseconds(this.EventTimeMillis);
}


public sealed class GoogleSubscriptionNotification
{
    public string? Version { get; set; }
    public int NotificationType { get; set; }
    public string? PurchaseToken { get; set; }

    /// <summary>Deprecated by Google but still sent - use subscriptionsv2 line items instead</summary>
    public string? SubscriptionId { get; set; }
}


public sealed class GoogleOneTimeProductNotification
{
    public string? Version { get; set; }
    public int NotificationType { get; set; }
    public string? PurchaseToken { get; set; }
    public string? Sku { get; set; }
}


public sealed class GoogleVoidedPurchaseNotification
{
    public string? PurchaseToken { get; set; }
    public string? OrderId { get; set; }

    /// <summary>1 = subscription, 2 = one-time</summary>
    public int ProductType { get; set; }

    /// <summary>1 = full refund, 2 = quantity-based partial refund</summary>
    public int RefundType { get; set; }
}


public sealed class GooglePendingRefundReviewNotification
{
    public string? Version { get; set; }
    public string? PendingRefundToken { get; set; }
    public string? OrderId { get; set; }
    public int? RefundReason { get; set; }
    public string? ObfuscatedAccountId { get; set; }
    public string? ObfuscatedProfileId { get; set; }
}


public sealed class GoogleTestNotification
{
    public string? Version { get; set; }
}


/// <summary>purchases.subscriptionsv2 SubscriptionPurchaseV2</summary>
public sealed class GoogleSubscriptionPurchase
{
    public string? Kind { get; set; }
    public string? RegionCode { get; set; }

    [JsonConverter(typeof(Rfc3339DateTimeOffsetConverter))]
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>SUBSCRIPTION_STATE_ACTIVE, _PENDING, _PAUSED, _IN_GRACE_PERIOD, _ON_HOLD, _CANCELED, _EXPIRED, _PENDING_PURCHASE_CANCELED</summary>
    public string? SubscriptionState { get; set; }

    public string? LatestOrderId { get; set; }
    public string? LinkedPurchaseToken { get; set; }

    /// <summary>ACKNOWLEDGEMENT_STATE_PENDING or ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED</summary>
    public string? AcknowledgementState { get; set; }

    public GoogleExternalAccountIdentifiers? ExternalAccountIdentifiers { get; set; }

    /// <summary>Present (an empty object) for license tester purchases</summary>
    public JsonElement? TestPurchase { get; set; }

    public JsonElement? CanceledStateContext { get; set; }
    public JsonElement? PausedStateContext { get; set; }
    public List<GoogleSubscriptionLineItem>? LineItems { get; set; }

    [JsonIgnore] public bool IsTestPurchase => this.TestPurchase.HasValue;
    [JsonIgnore] public bool IsAcknowledged => this.AcknowledgementState == "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED";
    [JsonIgnore] public DateTimeOffset? ExpiresAt => this.LineItems?.Max(x => x.ExpiryTime);
}


public sealed class GoogleExternalAccountIdentifiers
{
    public string? ExternalAccountId { get; set; }
    public string? ObfuscatedExternalAccountId { get; set; }
    public string? ObfuscatedExternalProfileId { get; set; }
}


public sealed class GoogleSubscriptionLineItem
{
    public string? ProductId { get; set; }

    [JsonConverter(typeof(Rfc3339DateTimeOffsetConverter))]
    public DateTimeOffset? ExpiryTime { get; set; }

    public string? LatestSuccessfulOrderId { get; set; }
    public GoogleAutoRenewingPlan? AutoRenewingPlan { get; set; }
    public GooglePrepaidPlan? PrepaidPlan { get; set; }
    public GoogleOfferDetails? OfferDetails { get; set; }
}


public sealed class GoogleAutoRenewingPlan
{
    public bool? AutoRenewEnabled { get; set; }
    public GoogleMoney? RecurringPrice { get; set; }
}


public sealed class GooglePrepaidPlan
{
    [JsonConverter(typeof(Rfc3339DateTimeOffsetConverter))]
    public DateTimeOffset? AllowExtendAfterTime { get; set; }
}


public sealed class GoogleOfferDetails
{
    public string? BasePlanId { get; set; }
    public string? OfferId { get; set; }
    public List<string>? OfferTags { get; set; }
}


public sealed class GoogleMoney
{
    public string? CurrencyCode { get; set; }
    public string? Units { get; set; }
    public int? Nanos { get; set; }
}


/// <summary>purchases.productsv2 ProductPurchaseV2</summary>
public sealed class GoogleProductPurchase
{
    public string? Kind { get; set; }
    public string? OrderId { get; set; }
    public string? RegionCode { get; set; }

    [JsonConverter(typeof(Rfc3339DateTimeOffsetConverter))]
    public DateTimeOffset? PurchaseCompletionTime { get; set; }

    public string? AcknowledgementState { get; set; }
    public string? ObfuscatedExternalAccountId { get; set; }
    public string? ObfuscatedExternalProfileId { get; set; }
    public GooglePurchaseStateContext? PurchaseStateContext { get; set; }

    /// <summary>Present for license tester purchases</summary>
    public JsonElement? TestPurchaseContext { get; set; }

    public List<GoogleProductLineItem>? ProductLineItem { get; set; }

    [JsonIgnore] public bool IsTestPurchase => this.TestPurchaseContext.HasValue;
    [JsonIgnore] public bool IsAcknowledged => this.AcknowledgementState == "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED";

    /// <summary>PURCHASED, CANCELLED or PENDING</summary>
    [JsonIgnore] public string? PurchaseState => this.PurchaseStateContext?.PurchaseState;
}


public sealed class GooglePurchaseStateContext
{
    public string? PurchaseState { get; set; }
}


public sealed class GoogleProductLineItem
{
    public string? ProductId { get; set; }
    public GoogleProductOfferDetails? ProductOfferDetails { get; set; }
}


public sealed class GoogleProductOfferDetails
{
    public string? OfferId { get; set; }
    public string? PurchaseOptionId { get; set; }
    public int? Quantity { get; set; }
    public int? RefundableQuantity { get; set; }
    public string? OfferToken { get; set; }

    /// <summary>CONSUMPTION_STATE_YET_TO_BE_CONSUMED or CONSUMPTION_STATE_CONSUMED</summary>
    public string? ConsumptionState { get; set; }

    public List<string>? OfferTags { get; set; }
}


sealed class PubSubPushEnvelope
{
    public PubSubMessage? Message { get; set; }
    public string? Subscription { get; set; }
}


sealed class PubSubMessage
{
    public Dictionary<string, string>? Attributes { get; set; }
    public string? Data { get; set; }
    public string? MessageId { get; set; }
    public string? PublishTime { get; set; }
}


sealed class GoogleServiceAccountCredentials
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("project_id")] public string? ProjectId { get; set; }
    [JsonPropertyName("private_key_id")] public string? PrivateKeyId { get; set; }
    [JsonPropertyName("private_key")] public string? PrivateKey { get; set; }
    [JsonPropertyName("client_email")] public string? ClientEmail { get; set; }
    [JsonPropertyName("token_uri")] public string? TokenUri { get; set; }
}


sealed class GoogleTokenResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("token_type")] public string? TokenType { get; set; }
}


sealed class GoogleApiErrorResponse
{
    public GoogleApiError? Error { get; set; }
}


sealed class GoogleApiError
{
    public int? Code { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
}
