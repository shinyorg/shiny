using Shiny.InAppPurchases.Server;

namespace Sample.InAppPurchases.Server;


/// <summary>
/// Replace with your own logic: look up the user by AccountToken (the Guid the app passed to PurchaseAsync), then grant or
/// revoke the entitlement. Throwing returns 500 so the store redelivers - keep this idempotent.
/// </summary>
public class LoggingPurchaseEventHandler(ILogger<LoggingPurchaseEventHandler> logger) : IPurchaseEventHandler
{
    public Task HandleAsync(PurchaseEvent e, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "{Platform} {Type} ({RawType}) product={ProductId} transaction={TransactionId} user={AccountToken} env={Environment} expires={ExpiresAt}",
            e.Platform,
            e.Type,
            e.RawType,
            e.ProductId,
            e.TransactionId,
            e.AccountToken,
            e.Environment,
            e.ExpiresAt
        );

        switch (e.Type)
        {
            case PurchaseEventType.Purchased:
            case PurchaseEventType.Renewed:
            case PurchaseEventType.Recovered:
            case PurchaseEventType.Restarted:
            case PurchaseEventType.RefundReversed:
                // grant / extend access until e.ExpiresAt
                break;

            case PurchaseEventType.Expired:
            case PurchaseEventType.Refunded:
            case PurchaseEventType.Revoked:
            case PurchaseEventType.OnHold:
            case PurchaseEventType.GracePeriodExpired:
                // remove access
                break;
        }
        return Task.CompletedTask;
    }
}
