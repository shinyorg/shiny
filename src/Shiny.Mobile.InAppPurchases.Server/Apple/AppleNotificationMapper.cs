namespace Shiny.InAppPurchases.Server.Infrastructure;


static class AppleNotificationMapper
{
    public static string RawType(string notificationType, string? subtype)
        => String.IsNullOrEmpty(subtype) ? notificationType : $"{notificationType}/{subtype}";


    public static PurchaseEventType Map(string notificationType, string? subtype) => notificationType switch
    {
        "SUBSCRIBED" => PurchaseEventType.Purchased,                 // INITIAL_BUY, RESUBSCRIBE
        "ONE_TIME_CHARGE" => PurchaseEventType.Purchased,
        "DID_RENEW" => subtype == "BILLING_RECOVERY" ? PurchaseEventType.Recovered : PurchaseEventType.Renewed,
        "DID_FAIL_TO_RENEW" => subtype == "GRACE_PERIOD" ? PurchaseEventType.GracePeriodStarted : PurchaseEventType.RenewalFailed,
        "GRACE_PERIOD_EXPIRED" => PurchaseEventType.GracePeriodExpired,
        "DID_CHANGE_RENEWAL_STATUS" => subtype switch
        {
            "AUTO_RENEW_ENABLED" => PurchaseEventType.AutoRenewEnabled,
            "AUTO_RENEW_DISABLED" => PurchaseEventType.AutoRenewDisabled,
            _ => PurchaseEventType.Other
        },
        "DID_CHANGE_RENEWAL_PREF" => PurchaseEventType.PlanChanged,  // UPGRADE, DOWNGRADE, or none (downgrade cancelled)
        "EXPIRED" => PurchaseEventType.Expired,                      // VOLUNTARY, BILLING_RETRY, PRICE_INCREASE, PRODUCT_NOT_FOR_SALE
        "OFFER_REDEEMED" => PurchaseEventType.OfferRedeemed,
        "PRICE_INCREASE" or "PRICE_CHANGE" => PurchaseEventType.PriceChange,
        "REFUND" => PurchaseEventType.Refunded,
        "REFUND_DECLINED" => PurchaseEventType.RefundDeclined,
        "REFUND_REVERSED" => PurchaseEventType.RefundReversed,
        "REVOKE" => PurchaseEventType.Revoked,
        "CONSUMPTION_REQUEST" => PurchaseEventType.ConsumptionRequest,
        "TEST" => PurchaseEventType.Test,

        // RENEWAL_EXTENDED, RENEWAL_EXTENSION, METADATA_UPDATE, MIGRATION, EXTERNAL_PURCHASE_TOKEN, RESCIND_CONSENT, future types
        _ => PurchaseEventType.Other
    };
}
