using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.InAppPurchases;


/// <summary>
/// StoreKit 2 implementation, calling the ShinyStoreKit Swift bridge.
/// </summary>
public class InAppPurchaseManager(
    IServiceProvider services,
    ILogger<InAppPurchaseManager> logger
) : IInAppPurchaseManager, IShinyStartupTask
{
    int started;

    public StorePlatform Platform => StorePlatform.AppStore;
    public event EventHandler<Purchase>? PurchaseUpdated;


    public void Start()
    {
        if (Interlocked.Exchange(ref this.started, 1) == 1)
            return;

        // Transaction.updates must be observed from launch or Ask to Buy approvals, renewals and
        // purchases from other devices are missed until the next foreground purchase
        StoreKitBridge.StartUpdates(this.OnNativeUpdate);
    }


    public Task<bool> CanMakePaymentsAsync(CancellationToken cancelToken = default)
        => Task.FromResult(StoreKitBridge.CanMakePayments());


    public async Task<IReadOnlyList<StoreProduct>> GetProductsAsync(IEnumerable<string> productIds, CancellationToken cancelToken = default)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var request = JsonSerializer.Serialize(ids, StoreKitJsonContext.Default.ListString);
        var json = await StoreKitBridge.GetProductsAsync(request, cancelToken).ConfigureAwait(false);
        var products = JsonSerializer.Deserialize(json, StoreKitJsonContext.Default.ListNativeProduct) ?? [];

        return products.Select(StoreKitMapper.ToProduct).ToList();
    }


    public async Task<PurchaseResult> PurchaseAsync(string productId, PurchaseOptions? options = null, CancellationToken cancelToken = default)
    {
        this.Start();

        var quantity = options?.Quantity ?? 1;
        if (quantity is < 1 or > 10)
            throw new InAppPurchaseException(InAppPurchaseErrorCode.DeveloperError, "Quantity must be between 1 and 10");

        var request = JsonSerializer.Serialize(
            new NativePurchaseOptions
            {
                AppAccountToken = options?.AccountToken?.ToString("D"),
                Quantity = quantity
            },
            StoreKitJsonContext.Default.NativePurchaseOptions
        );

        string json;
        try
        {
            json = await StoreKitBridge.PurchaseAsync(productId, request, cancelToken).ConfigureAwait(false);
        }
        catch (InAppPurchaseException ex) when (StoreKitMapper.IsCancellation(ex))
        {
            return new PurchaseResult(PurchaseResultStatus.Cancelled);
        }

        var result = JsonSerializer.Deserialize(json, StoreKitJsonContext.Default.NativePurchaseResult)!;
        return result.Status switch
        {
            "success" => new PurchaseResult(PurchaseResultStatus.Success, StoreKitMapper.ToPurchase(result.Transaction!)),
            "pending" => new PurchaseResult(PurchaseResultStatus.Pending),
            "cancelled" => new PurchaseResult(PurchaseResultStatus.Cancelled),
            _ => throw new InAppPurchaseException(InAppPurchaseErrorCode.Unknown, $"Unknown StoreKit purchase status '{result.Status}'")
        };
    }


    public async Task<IReadOnlyList<Purchase>> GetEntitlementsAsync(CancellationToken cancelToken = default)
    {
        var json = await StoreKitBridge.CurrentEntitlementsAsync(cancelToken).ConfigureAwait(false);
        return ToPurchases(json);
    }


    public async Task<IReadOnlyList<Purchase>> GetUnfinishedPurchasesAsync(CancellationToken cancelToken = default)
    {
        var json = await StoreKitBridge.UnfinishedAsync(cancelToken).ConfigureAwait(false);
        return ToPurchases(json);
    }


    public async Task FinishPurchaseAsync(Purchase purchase, bool consume, CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchase);
        if (purchase.Platform != StorePlatform.AppStore)
            throw new InAppPurchaseException(InAppPurchaseErrorCode.InvalidState, "This purchase did not come from the App Store");

        if (purchase.State == PurchaseState.Pending)
            throw new InAppPurchaseException(InAppPurchaseErrorCode.InvalidState, "A pending purchase cannot be finished");

        // StoreKit has no separate consume step - finishing a consumable is what allows it to be bought again
        await StoreKitBridge.FinishAsync(purchase.TransactionId, cancelToken).ConfigureAwait(false);
    }


    public async Task<IReadOnlyList<Purchase>> RestorePurchasesAsync(CancellationToken cancelToken = default)
    {
        await StoreKitBridge.SyncAsync(cancelToken).ConfigureAwait(false);
        return await this.GetEntitlementsAsync(cancelToken).ConfigureAwait(false);
    }


    public Task ShowManageSubscriptionsAsync(string? productId = null, CancellationToken cancelToken = default)
        => StoreKitBridge.ShowManageSubscriptionsAsync(cancelToken);


    void OnNativeUpdate(string json)
    {
        Purchase purchase;
        try
        {
            var native = JsonSerializer.Deserialize(json, StoreKitJsonContext.Default.NativeTransaction)!;
            purchase = StoreKitMapper.ToPurchase(native);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse StoreKit transaction update");
            return;
        }

        _ = Task.Run(() => this.Dispatch(purchase));
    }


    async Task Dispatch(Purchase purchase)
    {
        try
        {
            this.PurchaseUpdated?.Invoke(this, purchase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PurchaseUpdated handler failed for {TransactionId}", purchase.TransactionId);
        }

        // resolved lazily - delegates commonly depend on IInAppPurchaseManager themselves
        foreach (var @delegate in services.GetServices<IPurchaseDelegate>())
        {
            try
            {
                await @delegate.OnPurchaseUpdated(purchase).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Delegate} failed for {TransactionId}", @delegate.GetType().Name, purchase.TransactionId);
            }
        }
    }


    static IReadOnlyList<Purchase> ToPurchases(string json)
        => (JsonSerializer.Deserialize(json, StoreKitJsonContext.Default.ListNativeTransaction) ?? [])
            .Select(StoreKitMapper.ToPurchase)
            .ToList();
}
