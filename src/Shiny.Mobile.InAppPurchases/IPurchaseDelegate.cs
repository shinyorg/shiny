namespace Shiny.InAppPurchases;


/// <summary>
/// Register with <c>services.AddInAppPurchases&lt;TDelegate&gt;()</c> to receive purchase updates that happen outside of
/// an awaited purchase call (Ask to Buy approvals, pending payments completing, renewals, refunds, other devices).
/// Listening starts at app launch, so updates delivered while no page is open are not lost.
/// </summary>
public interface IPurchaseDelegate
{
    /// <summary>
    /// Called for each updated purchase. The same purchase can be delivered more than once (for example on every launch
    /// until it is finished), so make your handling idempotent - key on <see cref="Purchase.TransactionId"/>.
    /// </summary>
    Task OnPurchaseUpdated(Purchase purchase);
}
