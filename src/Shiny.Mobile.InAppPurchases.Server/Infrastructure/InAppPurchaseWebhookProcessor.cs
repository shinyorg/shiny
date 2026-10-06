using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shiny.InAppPurchases.Server.Infrastructure;


sealed class InAppPurchaseWebhookProcessor(
    IOptions<InAppPurchaseServerOptions> options,
    IAppleStoreClient apple,
    IGooglePlayClient google,
    GooglePubSubAuthenticator pubSub,
    IEnumerable<IPurchaseEventHandler> handlers,
    IPurchaseEventDeduplicator deduplicator,
    IPurchaseVerifier verifier,
    TimeProvider timeProvider,
    ILogger<InAppPurchaseWebhookProcessor> logger
)
{
    public async Task HandleAppleAsync(HttpContext context)
    {
        var ct = context.RequestAborted;
        if (options.Value.Apple == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var body = await ReadJsonAsync(context, InAppPurchaseServerJsonContext.Default.AppleSignedPayloadBody);
        if (String.IsNullOrWhiteSpace(body?.SignedPayload))
        {
            logger.LogWarning("App Store notification rejected - body has no signedPayload");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        PurchaseEvent purchaseEvent;
        try
        {
            var notification = apple.VerifyNotification(body.SignedPayload);
            if (String.IsNullOrWhiteSpace(notification.NotificationUuid) || String.IsNullOrWhiteSpace(notification.NotificationType))
                throw new AppleVerificationException(AppleVerificationFailure.InvalidFormat, "Notification is missing notificationUUID or notificationType");

            var transaction = String.IsNullOrWhiteSpace(notification.Data?.SignedTransactionInfo)
                ? null
                : apple.VerifyTransaction(notification.Data.SignedTransactionInfo);

            var renewal = String.IsNullOrWhiteSpace(notification.Data?.SignedRenewalInfo)
                ? null
                : apple.VerifyRenewalInfo(notification.Data.SignedRenewalInfo);

            purchaseEvent = this.BuildAppleEvent(notification, transaction, renewal);
        }
        catch (AppleVerificationException ex)
        {
            logger.LogWarning("App Store notification rejected - {Failure}: {Message}", ex.Failure, ex.Message);
            context.Response.StatusCode = ex.Failure is AppleVerificationFailure.InvalidSignature or AppleVerificationFailure.InvalidCertificateChain or AppleVerificationFailure.InvalidAlgorithm
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status400BadRequest;
            return;
        }

        context.Response.StatusCode = await this.DispatchAsync(purchaseEvent, ct);
    }


    public async Task HandleGoogleAsync(HttpContext context)
    {
        var ct = context.RequestAborted;
        var config = options.Value.Google;
        if (config == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (config.RequirePubSubAuthentication)
        {
            var failure = await pubSub.ValidateAsync(context.Request.Headers.Authorization.ToString(), config, ct);
            if (failure != null)
            {
                logger.LogWarning("Google Pub/Sub push rejected - {Reason}", failure);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        var envelope = await ReadJsonAsync(context, InAppPurchaseServerJsonContext.Default.PubSubPushEnvelope);
        var message = envelope?.Message;
        if (String.IsNullOrWhiteSpace(message?.Data) || String.IsNullOrWhiteSpace(message.MessageId))
        {
            logger.LogWarning("Google Pub/Sub push rejected - envelope has no message data or messageId");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        GoogleDeveloperNotification? notification;
        try
        {
            notification = JsonSerializer.Deserialize(Convert.FromBase64String(message.Data), InAppPurchaseServerJsonContext.Default.GoogleDeveloperNotification);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            notification = null;
        }

        if (notification == null)
        {
            logger.LogWarning("Google Pub/Sub message {MessageId} rejected - data is not a DeveloperNotification", message.MessageId);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!String.Equals(notification.PackageName, config.PackageName, StringComparison.Ordinal))
        {
            logger.LogWarning("Google Pub/Sub message {MessageId} rejected - package '{Package}' does not match Google.PackageName", message.MessageId, notification.PackageName);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // skip the Play Developer API round trip for redeliveries
        if (await deduplicator.HasProcessedAsync(StorePlatform.GooglePlay, message.MessageId, ct))
        {
            logger.LogInformation("Duplicate Google Play notification {NotificationId} ignored", message.MessageId);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return;
        }

        PurchaseEvent purchaseEvent;
        try
        {
            purchaseEvent = await this.BuildGoogleEventAsync(message.MessageId, envelope!.Subscription, notification, config, ct);
        }
        catch (Exception ex) when (ex is GooglePlayApiException or HttpRequestException)
        {
            logger.LogError(ex, "Failed to fetch purchase details for Google Play notification {NotificationId} - returning 502 so Pub/Sub retries", message.MessageId);
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            return;
        }

        context.Response.StatusCode = await this.DispatchAsync(purchaseEvent, ct);
    }


    public async Task HandleVerifyAsync(HttpContext context)
    {
        var ct = context.RequestAborted;
        var request = await ReadJsonAsync(context, InAppPurchaseServerJsonContext.Default.PurchaseVerificationRequest);
        if (request == null || String.IsNullOrWhiteSpace(request.VerificationData))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        try
        {
            var result = await verifier.VerifyAsync(request, ct);
            await context.Response.WriteAsJsonAsync(result, InAppPurchaseServerJsonContext.Default.VerifiedPurchase, cancellationToken: ct);
        }
        catch (Exception ex) when (ex is GooglePlayApiException or AppleStoreApiException or HttpRequestException)
        {
            logger.LogError(ex, "Purchase verification failed to reach the {Platform} store", request.Platform);
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
        }
    }


    internal async Task<int> DispatchAsync(PurchaseEvent purchaseEvent, CancellationToken ct)
    {
        if (await deduplicator.HasProcessedAsync(purchaseEvent.Platform, purchaseEvent.NotificationId, ct))
        {
            logger.LogInformation("Duplicate {Platform} notification {NotificationId} ignored", purchaseEvent.Platform, purchaseEvent.NotificationId);
            return StatusCodes.Status200OK;
        }

        var list = handlers as IPurchaseEventHandler[] ?? handlers.ToArray();
        if (list.Length == 0)
            logger.LogWarning("No IPurchaseEventHandler is registered - {Platform} {Type} notification {NotificationId} was not handled", purchaseEvent.Platform, purchaseEvent.Type, purchaseEvent.NotificationId);

        foreach (var handler in list)
        {
            try
            {
                await handler.HandleAsync(purchaseEvent, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "{Handler} failed for {Platform} {Type} notification {NotificationId} - returning 500 so the store retries",
                    handler.GetType().Name,
                    purchaseEvent.Platform,
                    purchaseEvent.Type,
                    purchaseEvent.NotificationId
                );
                return StatusCodes.Status500InternalServerError;
            }
        }

        await deduplicator.MarkProcessedAsync(purchaseEvent.Platform, purchaseEvent.NotificationId, ct);
        logger.LogInformation(
            "Processed {Platform} {RawType} notification {NotificationId} for product {ProductId}",
            purchaseEvent.Platform,
            purchaseEvent.RawType,
            purchaseEvent.NotificationId,
            purchaseEvent.ProductId
        );
        return StatusCodes.Status200OK;
    }


    PurchaseEvent BuildAppleEvent(AppleNotificationPayload notification, AppleTransaction? transaction, AppleRenewalInfo? renewal) => new()
    {
        NotificationId = notification.NotificationUuid,
        Platform = StorePlatform.AppStore,
        Type = AppleNotificationMapper.Map(notification.NotificationType, notification.Subtype),
        RawType = AppleNotificationMapper.RawType(notification.NotificationType, notification.Subtype),
        ProductId = transaction?.ProductId ?? renewal?.ProductId ?? notification.Summary?.ProductId,
        TransactionId = transaction?.TransactionId,
        OriginalTransactionId = transaction?.OriginalTransactionId ?? renewal?.OriginalTransactionId,
        AccountToken = InAppPurchaseUtils.ParseGuid(transaction?.AppAccountToken ?? renewal?.AppAccountToken),
        Environment = InAppPurchaseUtils.ParseAppleEnvironment(notification.Data?.Environment ?? notification.Summary?.Environment),
        OccurredAt = notification.SignedAt ?? timeProvider.GetUtcNow(),
        ExpiresAt = transaction?.ExpiresAt,
        IsAutoRenewing = renewal?.AutoRenewStatus is int status ? status == 1 : null,
        Quantity = transaction?.Quantity,
        Apple = new AppleNotificationDetail(notification, transaction, renewal)
    };


    async Task<PurchaseEvent> BuildGoogleEventAsync(
        string messageId,
        string? pubSubSubscription,
        GoogleDeveloperNotification notification,
        GooglePlayOptions config,
        CancellationToken ct
    )
    {
        var canFetch = config.HasServiceAccount;
        PurchaseEventType type;
        string rawType;
        string? token = null;
        string? productId = null;
        string? orderId = null;
        GoogleSubscriptionPurchase? subscription = null;
        GoogleProductPurchase? product = null;

        if (notification.SubscriptionNotification is { } sub)
        {
            (type, rawType) = GoogleNotificationMapper.MapSubscription(sub.NotificationType);
            token = sub.PurchaseToken;
            productId = sub.SubscriptionId;
            if (canFetch && !String.IsNullOrWhiteSpace(token))
                subscription = await google.GetSubscriptionAsync(token, ct);
        }
        else if (notification.OneTimeProductNotification is { } oneTime)
        {
            (type, rawType) = GoogleNotificationMapper.MapOneTimeProduct(oneTime.NotificationType);
            token = oneTime.PurchaseToken;
            productId = oneTime.Sku;
            if (canFetch && !String.IsNullOrWhiteSpace(token))
                product = await google.GetProductAsync(token, ct);
        }
        else if (notification.VoidedPurchaseNotification is { } voided)
        {
            (type, rawType) = (PurchaseEventType.Refunded, "VOIDED_PURCHASE");
            token = voided.PurchaseToken;
            orderId = voided.OrderId;
            if (canFetch && !String.IsNullOrWhiteSpace(token))
            {
                if (voided.ProductType == 1)
                    subscription = await google.GetSubscriptionAsync(token, ct);
                else
                    product = await google.GetProductAsync(token, ct);
            }
        }
        else if (notification.PendingRefundReviewNotification is { } review)
        {
            (type, rawType) = (PurchaseEventType.Other, "PENDING_REFUND_REVIEW");
            orderId = review.OrderId;
        }
        else if (notification.TestNotification != null)
        {
            (type, rawType) = (PurchaseEventType.Test, "TEST_NOTIFICATION");
        }
        else
        {
            (type, rawType) = (PurchaseEventType.Other, "UNKNOWN_NOTIFICATION");
        }

        var line = subscription?.LineItems?.OrderByDescending(x => x.ExpiryTime).FirstOrDefault();
        var productLine = product?.ProductLineItem?.FirstOrDefault();

        var environment = subscription != null
            ? (subscription.IsTestPurchase ? StoreEnvironment.Sandbox : StoreEnvironment.Production)
            : product != null
                ? (product.IsTestPurchase ? StoreEnvironment.Sandbox : StoreEnvironment.Production)
                : StoreEnvironment.Unknown;

        return new PurchaseEvent
        {
            NotificationId = messageId,
            Platform = StorePlatform.GooglePlay,
            Type = type,
            RawType = rawType,
            ProductId = line?.ProductId ?? productLine?.ProductId ?? productId,
            TransactionId = line?.LatestSuccessfulOrderId ?? subscription?.LatestOrderId ?? product?.OrderId ?? orderId,
            OriginalTransactionId = token,
            AccountToken = InAppPurchaseUtils.ParseGuid(
                subscription?.ExternalAccountIdentifiers?.ObfuscatedExternalAccountId ??
                product?.ObfuscatedExternalAccountId ??
                notification.PendingRefundReviewNotification?.ObfuscatedAccountId
            ),
            Environment = environment,
            OccurredAt = notification.EventTime ?? timeProvider.GetUtcNow(),
            ExpiresAt = subscription?.ExpiresAt,
            IsAutoRenewing = line?.AutoRenewingPlan?.AutoRenewEnabled,
            Quantity = productLine?.ProductOfferDetails?.Quantity,
            Google = new GoogleNotificationDetail(notification, pubSubSubscription, subscription, product)
        };
    }


    static async Task<T?> ReadJsonAsync<T>(HttpContext context, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo) where T : class
    {
        try
        {
            return await JsonSerializer.DeserializeAsync(context.Request.Body, typeInfo, context.RequestAborted);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
