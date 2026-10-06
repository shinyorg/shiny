namespace Shiny.InAppPurchases.Server.Infrastructure;


static class GoogleNotificationMapper
{
    public static (PurchaseEventType Type, string RawType) MapSubscription(int notificationType) => notificationType switch
    {
        1 => (PurchaseEventType.Recovered, "SUBSCRIPTION_RECOVERED"),
        2 => (PurchaseEventType.Renewed, "SUBSCRIPTION_RENEWED"),
        3 => (PurchaseEventType.AutoRenewDisabled, "SUBSCRIPTION_CANCELED"),
        4 => (PurchaseEventType.Purchased, "SUBSCRIPTION_PURCHASED"),
        5 => (PurchaseEventType.OnHold, "SUBSCRIPTION_ON_HOLD"),
        6 => (PurchaseEventType.GracePeriodStarted, "SUBSCRIPTION_IN_GRACE_PERIOD"),
        7 => (PurchaseEventType.Restarted, "SUBSCRIPTION_RESTARTED"),
        8 => (PurchaseEventType.PriceChange, "SUBSCRIPTION_PRICE_CHANGE_CONFIRMED"),
        9 => (PurchaseEventType.Other, "SUBSCRIPTION_DEFERRED"),
        10 => (PurchaseEventType.Paused, "SUBSCRIPTION_PAUSED"),
        11 => (PurchaseEventType.Other, "SUBSCRIPTION_PAUSE_SCHEDULE_CHANGED"),
        12 => (PurchaseEventType.Revoked, "SUBSCRIPTION_REVOKED"),
        13 => (PurchaseEventType.Expired, "SUBSCRIPTION_EXPIRED"),
        17 => (PurchaseEventType.PlanChanged, "SUBSCRIPTION_ITEMS_CHANGED"),
        18 => (PurchaseEventType.AutoRenewDisabled, "SUBSCRIPTION_CANCELLATION_SCHEDULED"),
        19 => (PurchaseEventType.PriceChange, "SUBSCRIPTION_PRICE_CHANGE_UPDATED"),
        20 => (PurchaseEventType.PendingCanceled, "SUBSCRIPTION_PENDING_PURCHASE_CANCELED"),
        22 => (PurchaseEventType.PriceChange, "SUBSCRIPTION_PRICE_STEP_UP_CONSENT_UPDATED"),
        _ => (PurchaseEventType.Other, $"SUBSCRIPTION_NOTIFICATION_{notificationType}")
    };


    public static (PurchaseEventType Type, string RawType) MapOneTimeProduct(int notificationType) => notificationType switch
    {
        1 => (PurchaseEventType.Purchased, "ONE_TIME_PRODUCT_PURCHASED"),
        2 => (PurchaseEventType.PendingCanceled, "ONE_TIME_PRODUCT_CANCELED"),
        _ => (PurchaseEventType.Other, $"ONE_TIME_PRODUCT_NOTIFICATION_{notificationType}")
    };
}
