using Microsoft.Extensions.Logging;
using Shiny.InAppPurchases;

namespace Sample.InAppPurchases.Maui;


/// <summary>
/// Receives purchases that change outside of an awaited PurchaseAsync call - Ask to Buy approvals,
/// slow payments clearing, renewals, refunds and purchases made on another device.
/// </summary>
public class SamplePurchaseDelegate(
    EntitlementService entitlements,
    ILogger<SamplePurchaseDelegate> logger
) : IPurchaseDelegate
{
    public async Task OnPurchaseUpdated(Purchase purchase)
    {
        logger.LogInformation("Purchase update: {ProductId} {State} {TransactionId}", purchase.ProductId, purchase.State, purchase.TransactionId);
        await entitlements.ProcessAsync(purchase);
    }
}
