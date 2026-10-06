namespace Shiny.InAppPurchases;


/// <summary>
/// Cross-platform in-app purchase API over StoreKit 2 (iOS) and Google Play Billing (Android).
/// </summary>
public interface IInAppPurchaseManager
{
    /// <summary>
    /// The store this device purchases through.
    /// </summary>
    StorePlatform Platform { get; }

    /// <summary>
    /// True when the store is reachable and this user/device is allowed to make purchases
    /// (Apple: parental controls / MDM can block payments; Google: Play Store installed and billing available).
    /// </summary>
    Task<bool> CanMakePaymentsAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// Loads localized product information. Unknown product IDs are omitted from the result rather than throwing.
    /// </summary>
    Task<IReadOnlyList<StoreProduct>> GetProductsAsync(IEnumerable<string> productIds, CancellationToken cancelToken = default);

    /// <summary>
    /// Launches the native purchase sheet. User cancellation and pending (Ask to Buy / slow payment) purchases are
    /// reported through <see cref="PurchaseResult.Status"/>; genuine failures throw <see cref="InAppPurchaseException"/>.
    /// A successful purchase is NOT finished automatically - grant the entitlement (ideally after verifying
    /// <see cref="Purchase.VerificationData"/> on your server) and then call <see cref="FinishPurchaseAsync"/>.
    /// Google Play refunds purchases that are not acknowledged within 3 days.
    /// </summary>
    Task<PurchaseResult> PurchaseAsync(string productId, PurchaseOptions? options = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Products the user currently owns: non-consumables and active subscriptions (plus consumables that have not been finished yet).
    /// </summary>
    Task<IReadOnlyList<Purchase>> GetEntitlementsAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// Purchases that completed but were never finished (acknowledged/consumed) - check this at startup so an app
    /// crash between payment and <see cref="FinishPurchaseAsync"/> never loses a purchase.
    /// </summary>
    Task<IReadOnlyList<Purchase>> GetUnfinishedPurchasesAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// Completes a purchase after the entitlement has been granted.
    /// Apple: finishes the transaction. Google: consumes it when <paramref name="consume"/> is true (so a consumable
    /// can be bought again), otherwise acknowledges it.
    /// </summary>
    Task FinishPurchaseAsync(Purchase purchase, bool consume, CancellationToken cancelToken = default);

    /// <summary>
    /// Restores purchases. Apple: forces an App Store sync (may prompt for sign-in, so only call from a user-initiated
    /// "Restore Purchases" button). Google: re-queries owned purchases. Returns the current entitlements.
    /// </summary>
    Task<IReadOnlyList<Purchase>> RestorePurchasesAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// Opens the platform subscription management UI (optionally focused on a single subscription product on Android).
    /// </summary>
    Task ShowManageSubscriptionsAsync(string? productId = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Raised for purchases that change outside of an awaited <see cref="PurchaseAsync"/> call - Ask to Buy approvals,
    /// pending payments completing, renewals, refunds/revocations, purchases made on another device, and
    /// offer-code redemptions. Registered <see cref="IPurchaseDelegate"/>s receive the same updates.
    /// </summary>
    event EventHandler<Purchase>? PurchaseUpdated;
}
