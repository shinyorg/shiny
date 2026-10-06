namespace Shiny.InAppPurchases.Server;


/// <summary>
/// Receives verified store notifications. Register with <c>AddInAppPurchaseServer(...).AddPurchaseEventHandler&lt;T&gt;()</c>
/// (scoped - inject your DbContext freely). Throwing makes the endpoint return 500 so the store redelivers the
/// notification; because every handler runs again on redelivery, keep handlers idempotent (key on
/// <see cref="PurchaseEvent.NotificationId"/> or <see cref="PurchaseEvent.TransactionId"/>).
/// </summary>
public interface IPurchaseEventHandler
{
    Task HandleAsync(PurchaseEvent purchaseEvent, CancellationToken cancellationToken);
}


/// <summary>
/// A store notification, verified and normalized across the App Store and Google Play.
/// </summary>
public sealed record PurchaseEvent
{
    /// <summary>Apple notificationUUID / Pub/Sub messageId - unique per notification, stable across redeliveries</summary>
    public required string NotificationId { get; init; }

    public required StorePlatform Platform { get; init; }

    public required PurchaseEventType Type { get; init; }

    /// <summary>The store's own type - "SUBSCRIBED/INITIAL_BUY" (Apple type/subtype) or "SUBSCRIPTION_PURCHASED" (Google)</summary>
    public required string RawType { get; init; }

    public string? ProductId { get; init; }

    /// <summary>Apple transactionId / Google order id (latest successful order for subscriptions)</summary>
    public string? TransactionId { get; init; }

    /// <summary>Stable ownership id - Apple originalTransactionId / Google purchase token</summary>
    public string? OriginalTransactionId { get; init; }

    /// <summary>The account token the app supplied at purchase (Apple appAccountToken / Google obfuscatedExternalAccountId)</summary>
    public Guid? AccountToken { get; init; }

    /// <summary>Production or Sandbox. Google is Unknown unless purchase details were fetched (requires ServiceAccountJson).</summary>
    public StoreEnvironment Environment { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public bool? IsAutoRenewing { get; init; }

    public int? Quantity { get; init; }

    /// <summary>Full decoded App Store payload (null for Google)</summary>
    public AppleNotificationDetail? Apple { get; init; }

    /// <summary>Full Google RTDN plus fetched purchase details (null for Apple)</summary>
    public GoogleNotificationDetail? Google { get; init; }
}


/// <param name="Notification">The verified responseBodyV2DecodedPayload</param>
/// <param name="Transaction">The verified, decoded signedTransactionInfo (if present)</param>
/// <param name="RenewalInfo">The verified, decoded signedRenewalInfo (if present)</param>
public sealed record AppleNotificationDetail(
    AppleNotificationPayload Notification,
    AppleTransaction? Transaction,
    AppleRenewalInfo? RenewalInfo
);


/// <param name="Notification">The decoded DeveloperNotification</param>
/// <param name="PubSubSubscription">The Pub/Sub subscription that pushed the message</param>
/// <param name="Subscription">purchases.subscriptionsv2.get result (null when not a subscription or no service account is configured)</param>
/// <param name="Product">purchases.productsv2.getproductpurchasev2 result (null when not a one-time product or no service account is configured)</param>
public sealed record GoogleNotificationDetail(
    GoogleDeveloperNotification Notification,
    string? PubSubSubscription,
    GoogleSubscriptionPurchase? Subscription,
    GoogleProductPurchase? Product
);
