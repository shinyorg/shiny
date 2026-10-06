using Microsoft.Extensions.Logging;
using Shiny.InAppPurchases;

namespace Sample.InAppPurchases.Maui;


/// <summary>
/// The one place purchases turn into entitlements: verify -> grant -> finish.
/// Both the purchase button and the out-of-band delegate go through <see cref="ProcessAsync"/>, so a purchase is
/// handled identically however it arrives (and only once, even if the store delivers it again).
/// A real app keeps entitlements on its server; Preferences stand in for that here.
/// </summary>
public class EntitlementService(
    IInAppPurchaseManager purchases,
    PurchaseApi api,
    ILogger<EntitlementService> logger
)
{
    const string ProcessedKey = "processed_transactions";
    readonly SemaphoreSlim gate = new(1, 1);

    public event EventHandler? Changed;

    public int Coins => Preferences.Get(SampleConfig.Coins, 0);
    public bool AdsRemoved => Preferences.Get(SampleConfig.RemoveAds, false);
    public bool IsPremium => Preferences.Get(SampleConfig.Premium, false);


    /// <summary>
    /// Stable per-install id sent as the purchase AccountToken. In a real app use your signed-in user's id so
    /// App Store / Google Play server notifications can be matched to the user.
    /// </summary>
    public static Guid AccountToken
    {
        get
        {
            var value = Preferences.Get("account_token", null);
            if (Guid.TryParse(value, out var token))
                return token;

            token = Guid.NewGuid();
            Preferences.Set("account_token", token.ToString());
            return token;
        }
    }


    public async Task<bool> ProcessAsync(Purchase purchase, CancellationToken cancelToken = default)
    {
        if (purchase.State == PurchaseState.Pending)
        {
            // Ask to Buy / slow payment - never grant; the store delivers the purchase again when it resolves
            logger.LogInformation("{ProductId} is pending", purchase.ProductId);
            return false;
        }

        await this.gate.WaitAsync(cancelToken);
        try
        {
            if (purchase.State == PurchaseState.Revoked)
            {
                this.Revoke(purchase.ProductId);
                return false;
            }

            if (!this.IsProcessed(purchase.TransactionId))
            {
                var verified = await api.VerifyAsync(purchase, cancelToken);
                if (!verified.IsValid)
                {
                    // leave it unfinished: Apple redelivers it, Google refunds it if never acknowledged
                    logger.LogWarning("Verification failed for {TransactionId}: {Error}", purchase.TransactionId, verified.Error);
                    return false;
                }
                if (verified.IsActive)
                    this.Grant(purchase);

                this.MarkProcessed(purchase.TransactionId);
            }

            // finish only AFTER the grant is durable - a crash before this line means the purchase comes back next launch
            if (!purchase.IsFinished || SampleConfig.IsConsumable(purchase.ProductId))
                await purchases.FinishPurchaseAsync(purchase, SampleConfig.IsConsumable(purchase.ProductId), cancelToken);

            return true;
        }
        finally
        {
            this.gate.Release();
            this.Changed?.Invoke(this, EventArgs.Empty);
        }
    }


    /// <summary>
    /// Call at startup and after "Restore Purchases": recovers unfinished purchases and re-syncs owned products.
    /// </summary>
    public async Task RefreshAsync(IReadOnlyList<Purchase>? owned = null, CancellationToken cancelToken = default)
    {
        foreach (var unfinished in await purchases.GetUnfinishedPurchasesAsync(cancelToken))
            await this.ProcessAsync(unfinished, cancelToken);

        owned ??= await purchases.GetEntitlementsAsync(cancelToken);
        var ownedIds = owned.Where(x => x.State == PurchaseState.Purchased).Select(x => x.ProductId).ToHashSet();

        // non-consumables and subscriptions mirror what the store says is owned right now
        Preferences.Set(SampleConfig.RemoveAds, ownedIds.Contains(SampleConfig.RemoveAds));
        Preferences.Set(SampleConfig.Premium, ownedIds.Contains(SampleConfig.Premium));
        this.Changed?.Invoke(this, EventArgs.Empty);
    }


    void Grant(Purchase purchase)
    {
        logger.LogInformation("Granting {ProductId} x{Quantity}", purchase.ProductId, purchase.Quantity);
        if (SampleConfig.IsConsumable(purchase.ProductId))
            Preferences.Set(SampleConfig.Coins, this.Coins + SampleConfig.CoinsPerPack * Math.Max(1, purchase.Quantity));
        else
            Preferences.Set(purchase.ProductId, true);
    }


    void Revoke(string productId)
    {
        logger.LogInformation("Revoking {ProductId}", productId);
        if (!SampleConfig.IsConsumable(productId))
            Preferences.Set(productId, false);
    }


    bool IsProcessed(string transactionId)
        => Preferences.Get(ProcessedKey, "").Split('|').Contains(transactionId);


    void MarkProcessed(string transactionId)
    {
        var ids = Preferences.Get(ProcessedKey, "")
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .TakeLast(199)
            .Append(transactionId);

        Preferences.Set(ProcessedKey, String.Join('|', ids));
    }
}
