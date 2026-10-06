using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server
{
    /// <summary>
    /// Verifies a purchase reported by the app (Shiny.InAppPurchases <c>Purchase.VerificationData</c>) before granting an entitlement.
    /// </summary>
    public interface IPurchaseVerifier
    {
        /// <summary>
        /// Apple: offline verification of the signed transaction. Google: purchase token lookup through the Play Developer API.
        /// Invalid purchases return <see cref="VerifiedPurchase.IsValid"/> false; store/network outages throw.
        /// </summary>
        Task<VerifiedPurchase> VerifyAsync(PurchaseVerificationRequest request, CancellationToken cancellationToken = default);
    }


    public sealed record PurchaseVerificationRequest
    {
        public required StorePlatform Platform { get; init; }

        /// <summary>Apple: signed JWS transaction. Google: purchase token.</summary>
        public required string VerificationData { get; init; }

        /// <summary>Optional - when set, the verified purchase must be for this product</summary>
        public string? ProductId { get; init; }
    }


    public sealed record VerifiedPurchase
    {
        public bool IsValid { get; init; }

        /// <summary>Why verification failed (null when valid)</summary>
        public string? Error { get; init; }

        /// <summary>
        /// Whether the user should currently have access: not revoked/refunded, subscription not expired (active, grace
        /// period, or cancelled-but-not-yet-expired), one-time purchase in PURCHASED state.
        /// </summary>
        public bool IsActive { get; init; }

        public StorePlatform Platform { get; init; }
        public PurchaseKind? Kind { get; init; }
        public string? ProductId { get; init; }
        public string? TransactionId { get; init; }
        public string? OriginalTransactionId { get; init; }
        public Guid? AccountToken { get; init; }
        public DateTimeOffset? PurchaseDate { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public StoreEnvironment Environment { get; init; }

        /// <summary>Google only - unacknowledged purchases are refunded after 3 days</summary>
        public bool? IsAcknowledged { get; init; }

        public int? Quantity { get; init; }
        public AppleTransaction? AppleTransaction { get; init; }
        public GoogleSubscriptionPurchase? GoogleSubscription { get; init; }
        public GoogleProductPurchase? GoogleProduct { get; init; }


        internal static VerifiedPurchase Invalid(StorePlatform platform, string error) => new()
        {
            IsValid = false,
            Platform = platform,
            Error = error
        };
    }
}


namespace Shiny.InAppPurchases.Server.Infrastructure
{
    sealed class PurchaseVerifier(
        IAppleStoreClient apple,
        IGooglePlayClient google,
        IOptions<InAppPurchaseServerOptions> options,
        TimeProvider timeProvider,
        ILogger<PurchaseVerifier> logger
    ) : IPurchaseVerifier
    {
        public Task<VerifiedPurchase> VerifyAsync(PurchaseVerificationRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (String.IsNullOrWhiteSpace(request.VerificationData))
                return Task.FromResult(VerifiedPurchase.Invalid(request.Platform, "VerificationData is required"));

            return request.Platform switch
            {
                StorePlatform.AppStore => Task.FromResult(this.VerifyApple(request)),
                StorePlatform.GooglePlay => this.VerifyGoogleAsync(request, cancellationToken),
                _ => Task.FromResult(VerifiedPurchase.Invalid(request.Platform, "Unknown platform"))
            };
        }


        VerifiedPurchase VerifyApple(PurchaseVerificationRequest request)
        {
            if (options.Value.Apple == null)
                return VerifiedPurchase.Invalid(StorePlatform.AppStore, "The App Store is not configured");

            AppleTransaction transaction;
            try
            {
                transaction = apple.VerifyTransaction(request.VerificationData);
            }
            catch (AppleVerificationException ex)
            {
                logger.LogWarning("App Store transaction verification failed: {Failure}", ex.Failure);
                return VerifiedPurchase.Invalid(StorePlatform.AppStore, ex.Message);
            }

            if (request.ProductId != null && transaction.ProductId != request.ProductId)
                return VerifiedPurchase.Invalid(StorePlatform.AppStore, "Product id does not match the signed transaction");

            var expires = transaction.ExpiresAt;
            return new VerifiedPurchase
            {
                IsValid = true,
                IsActive = transaction.RevocationDate == null && (expires == null || expires > timeProvider.GetUtcNow()),
                Platform = StorePlatform.AppStore,
                Kind = transaction.IsSubscription ? PurchaseKind.Subscription : PurchaseKind.OneTime,
                ProductId = transaction.ProductId,
                TransactionId = transaction.TransactionId,
                OriginalTransactionId = transaction.OriginalTransactionId,
                AccountToken = InAppPurchaseUtils.ParseGuid(transaction.AppAccountToken),
                PurchaseDate = transaction.PurchasedAt,
                ExpiresAt = expires,
                Environment = InAppPurchaseUtils.ParseAppleEnvironment(transaction.Environment),
                Quantity = transaction.Quantity,
                AppleTransaction = transaction
            };
        }


        async Task<VerifiedPurchase> VerifyGoogleAsync(PurchaseVerificationRequest request, CancellationToken cancellationToken)
        {
            var config = options.Value.Google;
            if (config == null)
                return VerifiedPurchase.Invalid(StorePlatform.GooglePlay, "Google Play is not configured");

            if (!config.HasServiceAccount)
                return VerifiedPurchase.Invalid(StorePlatform.GooglePlay, "Google.ServiceAccountJson is required to verify Google Play purchases");

            var token = request.VerificationData;
            GoogleSubscriptionPurchase? subscription;
            try
            {
                subscription = await google.GetSubscriptionAsync(token, cancellationToken).ConfigureAwait(false);
            }
            catch (GooglePlayApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                // a one-time product token is rejected by subscriptionsv2
                subscription = null;
            }

            var now = timeProvider.GetUtcNow();
            if (subscription != null)
            {
                var line = request.ProductId == null
                    ? subscription.LineItems?.OrderByDescending(x => x.ExpiryTime).FirstOrDefault()
                    : subscription.LineItems?.FirstOrDefault(x => x.ProductId == request.ProductId);

                if (line == null)
                    return VerifiedPurchase.Invalid(StorePlatform.GooglePlay, request.ProductId == null ? "Subscription has no line items" : "Product id does not match the subscription");

                var state = subscription.SubscriptionState;
                var active = state is "SUBSCRIPTION_STATE_ACTIVE" or "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" ||
                    (state == "SUBSCRIPTION_STATE_CANCELED" && line.ExpiryTime > now);

                return new VerifiedPurchase
                {
                    IsValid = true,
                    IsActive = active,
                    Platform = StorePlatform.GooglePlay,
                    Kind = PurchaseKind.Subscription,
                    ProductId = line.ProductId,
                    TransactionId = line.LatestSuccessfulOrderId ?? subscription.LatestOrderId,
                    OriginalTransactionId = token,
                    AccountToken = InAppPurchaseUtils.ParseGuid(subscription.ExternalAccountIdentifiers?.ObfuscatedExternalAccountId),
                    PurchaseDate = subscription.StartTime,
                    ExpiresAt = line.ExpiryTime,
                    Environment = subscription.IsTestPurchase ? StoreEnvironment.Sandbox : StoreEnvironment.Production,
                    IsAcknowledged = subscription.IsAcknowledged,
                    GoogleSubscription = subscription
                };
            }

            GoogleProductPurchase? product;
            try
            {
                product = await google.GetProductAsync(token, cancellationToken).ConfigureAwait(false);
            }
            catch (GooglePlayApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                product = null;
            }

            if (product == null)
                return VerifiedPurchase.Invalid(StorePlatform.GooglePlay, "Purchase token was not found for this package");

            var item = request.ProductId == null
                ? product.ProductLineItem?.FirstOrDefault()
                : product.ProductLineItem?.FirstOrDefault(x => x.ProductId == request.ProductId);

            if (item == null)
                return VerifiedPurchase.Invalid(StorePlatform.GooglePlay, "Product id does not match the purchase");

            return new VerifiedPurchase
            {
                IsValid = true,
                IsActive = product.PurchaseState == "PURCHASED",
                Platform = StorePlatform.GooglePlay,
                Kind = PurchaseKind.OneTime,
                ProductId = item.ProductId,
                TransactionId = product.OrderId,
                OriginalTransactionId = token,
                AccountToken = InAppPurchaseUtils.ParseGuid(product.ObfuscatedExternalAccountId),
                PurchaseDate = product.PurchaseCompletionTime,
                Environment = product.IsTestPurchase ? StoreEnvironment.Sandbox : StoreEnvironment.Production,
                IsAcknowledged = product.IsAcknowledged,
                Quantity = item.ProductOfferDetails?.Quantity,
                GoogleProduct = product
            };
        }
    }
}
