using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server.Tests;


public class NotificationMappingTests
{
    [Theory]
    [InlineData("SUBSCRIBED", "INITIAL_BUY", PurchaseEventType.Purchased)]
    [InlineData("SUBSCRIBED", "RESUBSCRIBE", PurchaseEventType.Purchased)]
    [InlineData("ONE_TIME_CHARGE", null, PurchaseEventType.Purchased)]
    [InlineData("DID_RENEW", null, PurchaseEventType.Renewed)]
    [InlineData("DID_RENEW", "BILLING_RECOVERY", PurchaseEventType.Recovered)]
    [InlineData("DID_FAIL_TO_RENEW", null, PurchaseEventType.RenewalFailed)]
    [InlineData("DID_FAIL_TO_RENEW", "GRACE_PERIOD", PurchaseEventType.GracePeriodStarted)]
    [InlineData("GRACE_PERIOD_EXPIRED", null, PurchaseEventType.GracePeriodExpired)]
    [InlineData("DID_CHANGE_RENEWAL_STATUS", "AUTO_RENEW_ENABLED", PurchaseEventType.AutoRenewEnabled)]
    [InlineData("DID_CHANGE_RENEWAL_STATUS", "AUTO_RENEW_DISABLED", PurchaseEventType.AutoRenewDisabled)]
    [InlineData("DID_CHANGE_RENEWAL_PREF", "UPGRADE", PurchaseEventType.PlanChanged)]
    [InlineData("DID_CHANGE_RENEWAL_PREF", "DOWNGRADE", PurchaseEventType.PlanChanged)]
    [InlineData("DID_CHANGE_RENEWAL_PREF", null, PurchaseEventType.PlanChanged)]
    [InlineData("EXPIRED", "VOLUNTARY", PurchaseEventType.Expired)]
    [InlineData("EXPIRED", "BILLING_RETRY", PurchaseEventType.Expired)]
    [InlineData("EXPIRED", "PRICE_INCREASE", PurchaseEventType.Expired)]
    [InlineData("EXPIRED", "PRODUCT_NOT_FOR_SALE", PurchaseEventType.Expired)]
    [InlineData("OFFER_REDEEMED", "UPGRADE", PurchaseEventType.OfferRedeemed)]
    [InlineData("PRICE_INCREASE", "PENDING", PurchaseEventType.PriceChange)]
    [InlineData("PRICE_INCREASE", "ACCEPTED", PurchaseEventType.PriceChange)]
    [InlineData("PRICE_CHANGE", null, PurchaseEventType.PriceChange)]
    [InlineData("REFUND", null, PurchaseEventType.Refunded)]
    [InlineData("REFUND_DECLINED", null, PurchaseEventType.RefundDeclined)]
    [InlineData("REFUND_REVERSED", null, PurchaseEventType.RefundReversed)]
    [InlineData("REVOKE", null, PurchaseEventType.Revoked)]
    [InlineData("CONSUMPTION_REQUEST", null, PurchaseEventType.ConsumptionRequest)]
    [InlineData("TEST", null, PurchaseEventType.Test)]
    [InlineData("RENEWAL_EXTENDED", null, PurchaseEventType.Other)]
    [InlineData("RENEWAL_EXTENSION", "SUMMARY", PurchaseEventType.Other)]
    [InlineData("RENEWAL_EXTENSION", "FAILURE", PurchaseEventType.Other)]
    [InlineData("METADATA_UPDATE", null, PurchaseEventType.Other)]
    [InlineData("MIGRATION", null, PurchaseEventType.Other)]
    [InlineData("EXTERNAL_PURCHASE_TOKEN", "UNREPORTED", PurchaseEventType.Other)]
    [InlineData("RESCIND_CONSENT", null, PurchaseEventType.Other)]
    [InlineData("SOMETHING_NEW", null, PurchaseEventType.Other)]
    public void Apple(string type, string? subtype, PurchaseEventType expected)
        => Assert.Equal(expected, AppleNotificationMapper.Map(type, subtype));


    [Fact]
    public void Apple_RawType()
    {
        Assert.Equal("SUBSCRIBED/INITIAL_BUY", AppleNotificationMapper.RawType("SUBSCRIBED", "INITIAL_BUY"));
        Assert.Equal("REFUND", AppleNotificationMapper.RawType("REFUND", null));
    }


    [Theory]
    [InlineData(1, PurchaseEventType.Recovered, "SUBSCRIPTION_RECOVERED")]
    [InlineData(2, PurchaseEventType.Renewed, "SUBSCRIPTION_RENEWED")]
    [InlineData(3, PurchaseEventType.AutoRenewDisabled, "SUBSCRIPTION_CANCELED")]
    [InlineData(4, PurchaseEventType.Purchased, "SUBSCRIPTION_PURCHASED")]
    [InlineData(5, PurchaseEventType.OnHold, "SUBSCRIPTION_ON_HOLD")]
    [InlineData(6, PurchaseEventType.GracePeriodStarted, "SUBSCRIPTION_IN_GRACE_PERIOD")]
    [InlineData(7, PurchaseEventType.Restarted, "SUBSCRIPTION_RESTARTED")]
    [InlineData(8, PurchaseEventType.PriceChange, "SUBSCRIPTION_PRICE_CHANGE_CONFIRMED")]
    [InlineData(9, PurchaseEventType.Other, "SUBSCRIPTION_DEFERRED")]
    [InlineData(10, PurchaseEventType.Paused, "SUBSCRIPTION_PAUSED")]
    [InlineData(11, PurchaseEventType.Other, "SUBSCRIPTION_PAUSE_SCHEDULE_CHANGED")]
    [InlineData(12, PurchaseEventType.Revoked, "SUBSCRIPTION_REVOKED")]
    [InlineData(13, PurchaseEventType.Expired, "SUBSCRIPTION_EXPIRED")]
    [InlineData(17, PurchaseEventType.PlanChanged, "SUBSCRIPTION_ITEMS_CHANGED")]
    [InlineData(18, PurchaseEventType.AutoRenewDisabled, "SUBSCRIPTION_CANCELLATION_SCHEDULED")]
    [InlineData(19, PurchaseEventType.PriceChange, "SUBSCRIPTION_PRICE_CHANGE_UPDATED")]
    [InlineData(20, PurchaseEventType.PendingCanceled, "SUBSCRIPTION_PENDING_PURCHASE_CANCELED")]
    [InlineData(22, PurchaseEventType.PriceChange, "SUBSCRIPTION_PRICE_STEP_UP_CONSENT_UPDATED")]
    [InlineData(99, PurchaseEventType.Other, "SUBSCRIPTION_NOTIFICATION_99")]
    public void GoogleSubscription(int code, PurchaseEventType expected, string raw)
        => Assert.Equal((expected, raw), GoogleNotificationMapper.MapSubscription(code));


    [Theory]
    [InlineData(1, PurchaseEventType.Purchased, "ONE_TIME_PRODUCT_PURCHASED")]
    [InlineData(2, PurchaseEventType.PendingCanceled, "ONE_TIME_PRODUCT_CANCELED")]
    [InlineData(7, PurchaseEventType.Other, "ONE_TIME_PRODUCT_NOTIFICATION_7")]
    public void GoogleOneTimeProduct(int code, PurchaseEventType expected, string raw)
        => Assert.Equal((expected, raw), GoogleNotificationMapper.MapOneTimeProduct(code));
}
