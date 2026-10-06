namespace Shiny.InAppPurchases;


public enum StorePlatform
{
    AppStore,
    GooglePlay
}


public enum ProductType
{
    /// <summary>
    /// A one-time purchase. Google Play does not distinguish consumable from non-consumable products - that is decided
    /// by how you finish the purchase (<see cref="IInAppPurchaseManager.FinishPurchaseAsync"/>).
    /// Apple non-renewing subscriptions are reported as one-time purchases.
    /// </summary>
    OneTime,

    /// <summary>
    /// An auto-renewing subscription.
    /// </summary>
    Subscription
}


public enum PurchaseState
{
    /// <summary>
    /// Paid for - grant the entitlement.
    /// </summary>
    Purchased,

    /// <summary>
    /// Waiting on Ask to Buy approval or a slow payment method. Do NOT grant the entitlement; a
    /// <see cref="IInAppPurchaseManager.PurchaseUpdated"/> event follows when it resolves.
    /// </summary>
    Pending,

    /// <summary>
    /// Refunded or revoked (Apple: revocationDate set, e.g. refund or Family Sharing removed). Remove the entitlement.
    /// </summary>
    Revoked
}


public enum PurchaseResultStatus
{
    /// <summary>
    /// Payment completed - <see cref="PurchaseResult.Purchase"/> is set.
    /// </summary>
    Success,

    /// <summary>
    /// Waiting on Ask to Buy approval or a slow payment method - watch <see cref="IInAppPurchaseManager.PurchaseUpdated"/>.
    /// </summary>
    Pending,

    /// <summary>
    /// The user dismissed the purchase sheet.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Google Play: the user already owns this product (an unconsumed consumable or an owned non-consumable/subscription).
    /// </summary>
    AlreadyOwned
}


public enum StoreEnvironment
{
    Unknown,
    Production,
    Sandbox,

    /// <summary>
    /// Apple: StoreKit configuration file testing in Xcode.
    /// </summary>
    Xcode
}


public enum SubscriptionPaymentMode
{
    /// <summary>
    /// Regular recurring price (the base plan).
    /// </summary>
    Recurring,
    FreeTrial,
    PayAsYouGo,
    PayUpFront
}


public enum SubscriptionReplacementMode
{
    /// <summary>
    /// Store default - Apple always decides based on the subscription group level; Google uses the base plan's configured mode.
    /// </summary>
    Default,
    WithTimeProration,
    ChargeProratedPrice,
    ChargeFullPrice,
    WithoutProration,
    Deferred
}


/// <summary>
/// A localized product from the store.
/// </summary>
/// <param name="Id">Store product identifier</param>
/// <param name="Type">One-time purchase or auto-renewing subscription</param>
/// <param name="Title">Localized product name</param>
/// <param name="Description">Localized product description</param>
/// <param name="DisplayPrice">Localized, formatted price (for subscriptions, the recurring price of the default base plan/offer)</param>
/// <param name="Price">Numeric price in <paramref name="CurrencyCode"/></param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="SubscriptionOffers">
/// Subscription offers. Google Play: one entry per base plan and per eligible offer (pass
/// <see cref="SubscriptionOffer.OfferToken"/> in <see cref="PurchaseOptions.OfferToken"/>).
/// Apple: the introductory offer (if the user is eligible) followed by promotional offers.
/// </param>
/// <param name="SubscriptionGroupId">Apple subscription group identifier (null on Android and for one-time products)</param>
public record StoreProduct(
    string Id,
    ProductType Type,
    string Title,
    string Description,
    string DisplayPrice,
    decimal Price,
    string CurrencyCode,
    IReadOnlyList<SubscriptionOffer> SubscriptionOffers,
    string? SubscriptionGroupId = null
);


/// <summary>
/// A purchasable subscription base plan or offer.
/// </summary>
/// <param name="OfferId">Apple offer identifier / Google offer id (null for Apple introductory offers and Google base plans)</param>
/// <param name="BasePlanId">Google base plan id (null on Apple)</param>
/// <param name="OfferToken">Google offer token required to purchase this offer (null on Apple)</param>
/// <param name="IsIntroductory">True for Apple introductory offers and Google free-trial/intro-price offers</param>
/// <param name="PricingPhases">The price schedule, in order (e.g. free trial then recurring)</param>
/// <param name="Tags">Google offer tags (empty on Apple)</param>
public record SubscriptionOffer(
    string? OfferId,
    string? BasePlanId,
    string? OfferToken,
    bool IsIntroductory,
    IReadOnlyList<PricingPhase> PricingPhases,
    IReadOnlyList<string> Tags
);


/// <summary>
/// One phase of a subscription price schedule.
/// </summary>
/// <param name="DisplayPrice">Localized, formatted price for one billing period</param>
/// <param name="Price">Numeric price for one billing period</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="BillingPeriod">ISO 8601 duration of one billing period, e.g. P1W, P1M, P1Y</param>
/// <param name="BillingCycleCount">Number of billing periods this phase lasts (0 = recurs until cancelled)</param>
/// <param name="PaymentMode">How this phase is charged</param>
public record PricingPhase(
    string DisplayPrice,
    decimal Price,
    string CurrencyCode,
    string BillingPeriod,
    int BillingCycleCount,
    SubscriptionPaymentMode PaymentMode
);


/// <summary>
/// A purchase/transaction as reported by the device's store.
/// </summary>
/// <param name="Platform">The store the purchase was made through</param>
/// <param name="ProductId">Store product identifier</param>
/// <param name="TransactionId">
/// Unique per charge. Apple: transaction id (a new one on each renewal). Google: order id, or the purchase token
/// while the purchase is still pending (Google does not assign an order id until payment completes).
/// </param>
/// <param name="OriginalTransactionId">
/// Stable ownership identifier. Apple: original transaction id (constant across renewals). Google: purchase token.
/// </param>
/// <param name="State">Purchased, pending or revoked</param>
/// <param name="PurchaseDate">When the purchase (or this renewal) happened</param>
/// <param name="ExpirationDate">Apple subscriptions only - Google does not expose expiry on device (query your server)</param>
/// <param name="IsFinished">Apple: transaction finished. Google: acknowledged (or consumed)</param>
/// <param name="IsAutoRenewing">True while an auto-renewable subscription is set to renew</param>
/// <param name="Quantity">Number of units purchased</param>
/// <param name="AccountToken">The <see cref="PurchaseOptions.AccountToken"/> supplied when the purchase was made</param>
/// <param name="VerificationData">
/// Send this to your server to verify the purchase. Apple: the signed JWS transaction (verify offline against the
/// Apple Root CA G3 chain). Google: the purchase token (look it up with the Google Play Developer API).
/// Shiny.InAppPurchases.Server's <c>IPurchaseVerifier</c> accepts it directly.
/// </param>
/// <param name="AppId">App bundle id (Apple) / package name (Google)</param>
/// <param name="Environment">Store environment (always Unknown on Google Play - the server can tell)</param>
/// <param name="OriginalJson">Raw JSON as returned by the store (Apple transaction jsonRepresentation / Google originalJson)</param>
/// <param name="Signature">Google Play: RSA-SHA1 signature of <paramref name="OriginalJson"/>. Null on Apple.</param>
/// <param name="RevocationDate">Apple: date the transaction was refunded/revoked</param>
public record Purchase(
    StorePlatform Platform,
    string ProductId,
    string TransactionId,
    string OriginalTransactionId,
    PurchaseState State,
    DateTimeOffset PurchaseDate,
    DateTimeOffset? ExpirationDate,
    bool IsFinished,
    bool IsAutoRenewing,
    int Quantity,
    Guid? AccountToken,
    string VerificationData,
    string AppId,
    StoreEnvironment Environment,
    string? OriginalJson = null,
    string? Signature = null,
    DateTimeOffset? RevocationDate = null
);


/// <summary>
/// The outcome of <see cref="IInAppPurchaseManager.PurchaseAsync"/>.
/// </summary>
/// <param name="Status">What happened</param>
/// <param name="Purchase">Set when <paramref name="Status"/> is Success (and Pending when the store reports one)</param>
public record PurchaseResult(PurchaseResultStatus Status, Purchase? Purchase = null);


public record PurchaseOptions
{
    /// <summary>
    /// Ties the purchase to your user account. Apple: appAccountToken. Google: obfuscatedAccountId.
    /// It is echoed back on every transaction and in server notifications, so your backend can map a purchase to a user.
    /// </summary>
    public Guid? AccountToken { get; init; }

    /// <summary>
    /// Google Play: the <see cref="SubscriptionOffer.OfferToken"/> to purchase. Defaults to the first base plan offer.
    /// Ignored on Apple.
    /// </summary>
    public string? OfferToken { get; init; }

    /// <summary>
    /// Apple: number of consumable units to buy (1-10). Google Play lets the user pick quantity in the sheet when
    /// multi-quantity is enabled for the product.
    /// </summary>
    public int Quantity { get; init; } = 1;

    /// <summary>
    /// Google Play: upgrade/downgrade an existing subscription. Apple handles changes within a subscription group
    /// automatically, so this is ignored there.
    /// </summary>
    public SubscriptionReplacement? Replacement { get; init; }
}


/// <summary>
/// Google Play subscription upgrade/downgrade details.
/// </summary>
/// <param name="OldProductId">The subscription product being replaced</param>
/// <param name="OldPurchaseToken">Purchase token (<see cref="Purchase.OriginalTransactionId"/>) of the subscription being replaced</param>
/// <param name="Mode">How the change is prorated/charged</param>
public record SubscriptionReplacement(
    string OldProductId,
    string OldPurchaseToken,
    SubscriptionReplacementMode Mode = SubscriptionReplacementMode.Default
);
