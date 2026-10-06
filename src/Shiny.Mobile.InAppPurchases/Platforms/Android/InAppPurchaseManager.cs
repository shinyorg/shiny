using System.Collections.Concurrent;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Android.BillingClient.Api;

namespace Shiny.InAppPurchases;


public class InAppPurchaseManager(
    AndroidPlatform platform,
    IServiceProvider services,
    ILogger<InAppPurchaseManager> logger
) : IInAppPurchaseManager, IShinyStartupTask
{
    static readonly TimeSpan ActivityWaitTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);

    readonly SemaphoreSlim connectionLock = new(1, 1);
    readonly SemaphoreSlim purchaseLock = new(1, 1);
    readonly ConcurrentDictionary<string, Api.ProductDetails> productCache = new();
    readonly object inFlightSync = new();

    Api.BillingClient? client;
    InFlightPurchase? inFlight;


    public StorePlatform Platform => StorePlatform.GooglePlay;
    public event EventHandler<Purchase>? PurchaseUpdated;


    public void Start()
    {
        // Google's recommended recovery: purchases that completed while the app was not running (or crashed before
        // acknowledging) are only discoverable by querying - surface them to the delegates at launch
        _ = Task.Run(async () =>
        {
            try
            {
                var unfinished = await this.GetUnfinishedPurchasesAsync().ConfigureAwait(false);
                foreach (var purchase in unfinished)
                    await this.Dispatch(purchase).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Unable to check for unfinished purchases at startup");
            }
        });
    }


    public async Task<bool> CanMakePaymentsAsync(CancellationToken cancelToken = default)
    {
        try
        {
            await this.GetConnectedClient(cancelToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Google Play Billing is not available");
            return false;
        }
    }


    public async Task<IReadOnlyList<StoreProduct>> GetProductsAsync(IEnumerable<string> productIds, CancellationToken cancelToken = default)
    {
        var ids = productIds.Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var details = await this.QueryProductDetails(ids, cancelToken).ConfigureAwait(false);
        return details
            .Select(ToStoreProduct)
            .OfType<StoreProduct>()
            .ToList();
    }


    public async Task<PurchaseResult> PurchaseAsync(string productId, PurchaseOptions? options = null, CancellationToken cancelToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        options ??= new PurchaseOptions();

        await this.purchaseLock.WaitAsync(cancelToken).ConfigureAwait(false);
        try
        {
            var billing = await this.GetConnectedClient(cancelToken).ConfigureAwait(false);
            var details = await this.GetProductDetails(productId, cancelToken).ConfigureAwait(false);
            var activity = await this.GetActivity(cancelToken).ConfigureAwait(false);
            var flowParams = BuildFlowParams(details, options);

            var pending = new InFlightPurchase(productId);
            lock (this.inFlightSync)
                this.inFlight = pending;

            try
            {
                using var reg = cancelToken.Register(() => pending.Completion.TrySetCanceled(cancelToken));

                var launchResult = await platform
                    .InvokeOnMainThreadAsync(() => billing.LaunchBillingFlow(activity, flowParams), cancelToken)
                    .ConfigureAwait(false);

                if (launchResult.ResponseCode != Api.BillingResponseCode.Ok)
                    return this.ToFailedResult(launchResult);

                var (result, purchases) = await pending.Completion.Task.ConfigureAwait(false);
                if (result.ResponseCode != Api.BillingResponseCode.Ok)
                    return this.ToFailedResult(result);

                var purchase = purchases
                    .SelectMany(ToPurchases)
                    .FirstOrDefault(x => x.ProductId == productId);

                if (purchase == null)
                    throw new InAppPurchaseException(InAppPurchaseErrorCode.Unknown, $"Google Play reported success but returned no purchase for '{productId}'");

                return purchase.State == PurchaseState.Pending
                    ? new PurchaseResult(PurchaseResultStatus.Pending, purchase)
                    : new PurchaseResult(PurchaseResultStatus.Success, purchase);
            }
            finally
            {
                lock (this.inFlightSync)
                {
                    if (this.inFlight == pending)
                        this.inFlight = null;
                }
            }
        }
        finally
        {
            this.purchaseLock.Release();
        }
    }


    public async Task<IReadOnlyList<Purchase>> GetEntitlementsAsync(CancellationToken cancelToken = default)
    {
        var purchases = await this.QueryAllPurchases(cancelToken).ConfigureAwait(false);
        return purchases.Where(x => x.State == PurchaseState.Purchased).ToList();
    }


    public async Task<IReadOnlyList<Purchase>> GetUnfinishedPurchasesAsync(CancellationToken cancelToken = default)
    {
        var purchases = await this.QueryAllPurchases(cancelToken).ConfigureAwait(false);
        return purchases.Where(x => x.State == PurchaseState.Purchased && !x.IsFinished).ToList();
    }


    public Task<IReadOnlyList<Purchase>> RestorePurchasesAsync(CancellationToken cancelToken = default)
        => this.GetEntitlementsAsync(cancelToken);


    public async Task FinishPurchaseAsync(Purchase purchase, bool consume, CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchase);
        if (purchase.Platform != StorePlatform.GooglePlay)
            throw new InAppPurchaseException(InAppPurchaseErrorCode.DeveloperError, "This purchase did not come from Google Play");

        if (purchase.State != PurchaseState.Purchased)
            throw new InAppPurchaseException(InAppPurchaseErrorCode.InvalidState, $"Only purchased (not {purchase.State}) purchases can be finished");

        var billing = await this.GetConnectedClient(cancelToken).ConfigureAwait(false);
        if (consume)
        {
            var consumeParams = Api.ConsumeParams
                .NewBuilder()
                .SetPurchaseToken(purchase.VerificationData)
                .Build();

            var result = await billing.ConsumeAsync(consumeParams).WaitAsync(cancelToken).ConfigureAwait(false);
            var code = result.BillingResult.ResponseCode;

            // consuming twice reports ITEM_NOT_OWNED - finishing is idempotent
            if (code == Api.BillingResponseCode.ItemNotOwned)
                logger.LogDebug("Purchase {Token} was already consumed", purchase.VerificationData);
            else
                ThrowIfFailed(result.BillingResult, "consume purchase");
        }
        else if (!purchase.IsFinished)
        {
            var ackParams = Api.AcknowledgePurchaseParams
                .NewBuilder()
                .SetPurchaseToken(purchase.VerificationData)
                .Build();

            var result = await billing.AcknowledgePurchaseAsync(ackParams).WaitAsync(cancelToken).ConfigureAwait(false);
            ThrowIfFailed(result, "acknowledge purchase");
        }
    }


    public Task ShowManageSubscriptionsAsync(string? productId = null, CancellationToken cancelToken = default)
    {
        var url = "https://play.google.com/store/account/subscriptions";
        if (!String.IsNullOrWhiteSpace(productId))
            url += $"?sku={Uri.EscapeDataString(productId)}&package={Uri.EscapeDataString(platform.AppContext.PackageName!)}";

        var intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(url));
        intent.AddFlags(ActivityFlags.NewTask);
        platform.AppContext.StartActivity(intent);
        return Task.CompletedTask;
    }


    async Task<Api.BillingClient> GetConnectedClient(CancellationToken cancelToken)
    {
        await this.connectionLock.WaitAsync(cancelToken).ConfigureAwait(false);
        try
        {
            this.client ??= this.CreateClient();
            if (this.client.IsReady)
                return this.client;

            Api.BillingResult? result;
            try
            {
                // devices without the Play Store (or a wedged billing service) may never call back
                result = await this.client
                    .StartConnectionAsync(() => logger.LogDebug("Google Play Billing service disconnected"))
                    .WaitAsync(ConnectTimeout, cancelToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                throw new InAppPurchaseException(InAppPurchaseErrorCode.StoreUnavailable, "Timed out connecting to Google Play Billing", ex);
            }

            if (result == null)
                throw new InAppPurchaseException(InAppPurchaseErrorCode.StoreUnavailable, "Google Play Billing service disconnected during setup");

            ThrowIfFailed(result, "connect to Google Play Billing");
            return this.client;
        }
        finally
        {
            this.connectionLock.Release();
        }
    }


    Api.BillingClient CreateClient()
    {
        var pendingParams = Api.PendingPurchasesParams
            .NewBuilder()
            .EnableOneTimeProducts()
            .EnablePrepaidPlans()
            .Build();

        var builder = Api.BillingClient
            .NewBuilder(platform.AppContext)
            .EnablePendingPurchases(pendingParams)
            .EnableAutoServiceReconnection();

        builder.SetListener(this.OnPurchasesUpdated);
        return builder.Build();
    }


    void OnPurchasesUpdated(Api.BillingResult result, IList<Api.Purchase>? purchases)
    {
        var list = purchases?.ToList() ?? [];
        InFlightPurchase? pending;
        lock (this.inFlightSync)
            pending = this.inFlight;

        var belongsToInFlight = pending != null && (
            result.ResponseCode != Api.BillingResponseCode.Ok ||
            list.Any(x => x.Products.Contains(pending.ProductId))
        );

        if (belongsToInFlight && pending!.Completion.TrySetResult((result, list)))
        {
            // other products delivered alongside the awaited one are still out-of-band updates
            list = list.Where(x => !x.Products.Contains(pending.ProductId)).ToList();
        }
        else if (result.ResponseCode != Api.BillingResponseCode.Ok)
        {
            logger.LogWarning("Purchase update failed: {Code} {Message}", result.ResponseCode, result.DebugMessage);
            return;
        }

        if (list.Count == 0)
            return;

        _ = Task.Run(async () =>
        {
            foreach (var purchase in list.SelectMany(ToPurchases))
                await this.Dispatch(purchase).ConfigureAwait(false);
        });
    }


    async Task Dispatch(Purchase purchase)
    {
        try
        {
            this.PurchaseUpdated?.Invoke(this, purchase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PurchaseUpdated handler failed for {ProductId}", purchase.ProductId);
        }

        foreach (var del in services.GetServices<IPurchaseDelegate>())
        {
            try
            {
                await del.OnPurchaseUpdated(purchase).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Delegate} failed for purchase {ProductId}", del.GetType().Name, purchase.ProductId);
            }
        }
    }


    async Task<global::Android.App.Activity> GetActivity(CancellationToken cancelToken)
    {
        if (platform.CurrentActivity is { IsFinishing: false } current)
            return current;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        cts.CancelAfter(ActivityWaitTimeout);
        try
        {
            var changed = await platform.WaitForActivity(cancellationToken: cts.Token).ConfigureAwait(false);
            return changed.Activity;
        }
        catch (OperationCanceledException) when (!cancelToken.IsCancellationRequested)
        {
            throw new InAppPurchaseException(InAppPurchaseErrorCode.NoUserInterface, "No foreground activity is available to present the Google Play purchase sheet");
        }
    }


    async Task<Api.ProductDetails> GetProductDetails(string productId, CancellationToken cancelToken)
    {
        if (this.productCache.TryGetValue(productId, out var cached))
            return cached;

        var details = await this.QueryProductDetails([productId], cancelToken).ConfigureAwait(false);
        return details.FirstOrDefault(x => x.ProductId == productId)
            ?? throw new InAppPurchaseException(InAppPurchaseErrorCode.ProductNotFound, $"Product '{productId}' was not found in Google Play");
    }


    async Task<List<Api.ProductDetails>> QueryProductDetails(IReadOnlyList<string> productIds, CancellationToken cancelToken)
    {
        var billing = await this.GetConnectedClient(cancelToken).ConfigureAwait(false);

        // a single query may only contain one product type
        var results = new List<Api.ProductDetails>();
        foreach (var type in new[] { Api.BillingClient.ProductType.Inapp, Api.BillingClient.ProductType.Subs })
        {
            var products = productIds
                .Select(id => Api.QueryProductDetailsParams.Product
                    .NewBuilder()
                    .SetProductId(id)
                    .SetProductType(type)
                    .Build()
                )
                .ToList();

            var queryParams = Api.QueryProductDetailsParams
                .NewBuilder()
                .SetProductList(products)
                .Build();

            var response = await billing.QueryProductDetailsAsync(queryParams).WaitAsync(cancelToken).ConfigureAwait(false);
            ThrowIfFailed(response.Result, "query product details");

            foreach (var details in response.ProductDetails ?? [])
            {
                this.productCache[details.ProductId] = details;
                results.Add(details);
            }
        }
        return results;
    }


    async Task<List<Purchase>> QueryAllPurchases(CancellationToken cancelToken)
    {
        var billing = await this.GetConnectedClient(cancelToken).ConfigureAwait(false);
        var results = new List<Purchase>();

        foreach (var type in new[] { Api.BillingClient.ProductType.Inapp, Api.BillingClient.ProductType.Subs })
        {
            var queryParams = Api.QueryPurchasesParams
                .NewBuilder()
                .SetProductType(type)
                .Build();

            var response = await billing.QueryPurchasesAsync(queryParams).WaitAsync(cancelToken).ConfigureAwait(false);
            ThrowIfFailed(response.Result, "query purchases");

            foreach (var purchase in response.Purchases ?? [])
                results.AddRange(ToPurchases(purchase));
        }
        return results;
    }


    static Api.BillingFlowParams BuildFlowParams(Api.ProductDetails details, PurchaseOptions options)
    {
        var productParams = Api.BillingFlowParams.ProductDetailsParams
            .NewBuilder()
            .SetProductDetails(details);

        var offerToken = options.OfferToken ?? GetDefaultOfferToken(details);
        if (offerToken != null)
            productParams.SetOfferToken(offerToken);

        // PBL 8.1+: the replacement mode is set per product (SubscriptionUpdateParams.setSubscriptionReplacementMode is deprecated)
        if (options.Replacement is { } replacement)
        {
            var replacementParams = Api.BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams
                .NewBuilder()
                .SetOldProductId(replacement.OldProductId);

            var mode = ToReplacementMode(replacement.Mode);
            if (mode != null)
                replacementParams.SetReplacementMode(mode.Value);

            productParams.SetSubscriptionProductReplacementParams(replacementParams.Build());
        }

        var builder = Api.BillingFlowParams
            .NewBuilder()
            .SetProductDetailsParamsList([productParams.Build()]);

        if (options.AccountToken is { } accountToken)
            builder.SetObfuscatedAccountId(accountToken.ToString("N"));

        if (options.Replacement is { } old)
        {
            var update = Api.BillingFlowParams.SubscriptionUpdateParams
                .NewBuilder()
                .SetOldPurchaseToken(old.OldPurchaseToken)
                .Build();

            builder.SetSubscriptionUpdateParams(update);
        }
        return builder.Build();
    }


    static string? GetDefaultOfferToken(Api.ProductDetails details)
    {
        if (details.ProductType == Api.BillingClient.ProductType.Subs)
        {
            var offers = details.GetSubscriptionOfferDetails();
            var offer = offers?.FirstOrDefault(x => x.OfferId == null) ?? offers?.FirstOrDefault();
            return offer?.OfferToken ?? throw new InAppPurchaseException(InAppPurchaseErrorCode.ProductUnavailable, $"Subscription '{details.ProductId}' has no purchasable base plan");
        }

        // one-time products with multiple purchase options/offers need an explicit offer token
        if (details.GetOneTimePurchaseOfferDetails() != null)
            return null;

        return details.OneTimePurchaseOfferDetailsList?.FirstOrDefault()?.OfferToken;
    }


    static int? ToReplacementMode(SubscriptionReplacementMode mode) => mode switch
    {
        SubscriptionReplacementMode.WithTimeProration => Api.BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.ReplacementMode.WithTimeProration,
        SubscriptionReplacementMode.ChargeProratedPrice => Api.BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.ReplacementMode.ChargeProratedPrice,
        SubscriptionReplacementMode.ChargeFullPrice => Api.BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.ReplacementMode.ChargeFullPrice,
        SubscriptionReplacementMode.WithoutProration => Api.BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.ReplacementMode.WithoutProration,
        SubscriptionReplacementMode.Deferred => Api.BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.ReplacementMode.Deferred,
        _ => null
    };


    static StoreProduct? ToStoreProduct(Api.ProductDetails details)
    {
        if (details.ProductType == Api.BillingClient.ProductType.Subs)
        {
            var offers = (details.GetSubscriptionOfferDetails() ?? [])
                .Select(ToSubscriptionOffer)
                .ToList();

            if (offers.Count == 0)
                return null;

            var basePlan = offers.FirstOrDefault(x => x.OfferId == null) ?? offers[0];
            var recurring = basePlan.PricingPhases.LastOrDefault(x => x.PaymentMode == SubscriptionPaymentMode.Recurring)
                ?? basePlan.PricingPhases.Last();

            return new StoreProduct(
                details.ProductId,
                ProductType.Subscription,
                details.Name,
                details.Description,
                recurring.DisplayPrice,
                recurring.Price,
                recurring.CurrencyCode,
                offers
            );
        }

        var oneTime = details.GetOneTimePurchaseOfferDetails() ?? details.OneTimePurchaseOfferDetailsList?.FirstOrDefault();
        if (oneTime == null)
            return null;

        return new StoreProduct(
            details.ProductId,
            ProductType.OneTime,
            details.Name,
            details.Description,
            oneTime.FormattedPrice,
            FromMicros(oneTime.PriceAmountMicros),
            oneTime.PriceCurrencyCode,
            []
        );
    }


    static SubscriptionOffer ToSubscriptionOffer(Api.ProductDetails.SubscriptionOfferDetails offer)
    {
        var phases = offer.PricingPhases.PricingPhaseList
            .Select(phase => new PricingPhase(
                phase.FormattedPrice,
                FromMicros(phase.PriceAmountMicros),
                phase.PriceCurrencyCode,
                phase.BillingPeriod,
                phase.RecurrenceMode == Api.ProductDetails.RecurrenceMode.InfiniteRecurring ? 0 : Math.Max(phase.BillingCycleCount, 1),
                ToPaymentMode(phase)
            ))
            .ToList();

        return new SubscriptionOffer(
            offer.OfferId,
            offer.BasePlanId,
            offer.OfferToken,
            phases.Any(x => x.PaymentMode != SubscriptionPaymentMode.Recurring),
            phases,
            offer.OfferTags?.ToList() ?? []
        );
    }


    static SubscriptionPaymentMode ToPaymentMode(Api.ProductDetails.PricingPhase phase)
    {
        if (phase.PriceAmountMicros == 0)
            return SubscriptionPaymentMode.FreeTrial;

        return phase.RecurrenceMode switch
        {
            Api.ProductDetails.RecurrenceMode.InfiniteRecurring => SubscriptionPaymentMode.Recurring,
            Api.ProductDetails.RecurrenceMode.FiniteRecurring => SubscriptionPaymentMode.PayAsYouGo,
            _ => SubscriptionPaymentMode.PayUpFront
        };
    }


    static IEnumerable<Purchase> ToPurchases(Api.Purchase purchase)
    {
        var state = purchase.PurchaseState == Api.PurchaseState.Purchased
            ? PurchaseState.Purchased
            : PurchaseState.Pending;

        Guid? accountToken = Guid.TryParse(purchase.AccountIdentifiers?.ObfuscatedAccountId, out var g) ? g : null;

        return purchase.Products.Select(productId => new Purchase(
            StorePlatform.GooglePlay,
            productId,
            purchase.OrderId ?? purchase.PurchaseToken,
            purchase.PurchaseToken,
            state,
            DateTimeOffset.FromUnixTimeMilliseconds(purchase.PurchaseTime),
            null,
            purchase.IsAcknowledged,
            purchase.IsAutoRenewing,
            purchase.Quantity,
            accountToken,
            purchase.PurchaseToken,
            purchase.PackageName,
            StoreEnvironment.Unknown,
            purchase.OriginalJson,
            purchase.Signature
        ));
    }


    PurchaseResult ToFailedResult(Api.BillingResult result) => result.ResponseCode switch
    {
        Api.BillingResponseCode.UserCancelled => new PurchaseResult(PurchaseResultStatus.Cancelled),
        Api.BillingResponseCode.ItemAlreadyOwned => new PurchaseResult(PurchaseResultStatus.AlreadyOwned),
        _ => throw ToException(result, "purchase")
    };


    static void ThrowIfFailed(Api.BillingResult result, string operation)
    {
        if (result.ResponseCode != Api.BillingResponseCode.Ok)
            throw ToException(result, operation);
    }


    static InAppPurchaseException ToException(Api.BillingResult result, string operation)
    {
        var code = result.ResponseCode switch
        {
            Api.BillingResponseCode.BillingUnavailable or
            Api.BillingResponseCode.ServiceUnavailable or
            Api.BillingResponseCode.ServiceDisconnected or
            Api.BillingResponseCode.ServiceTimeout or
            Api.BillingResponseCode.FeatureNotSupported => InAppPurchaseErrorCode.StoreUnavailable,
            Api.BillingResponseCode.ItemUnavailable => InAppPurchaseErrorCode.ProductUnavailable,
            Api.BillingResponseCode.DeveloperError => InAppPurchaseErrorCode.DeveloperError,
            Api.BillingResponseCode.NetworkError => InAppPurchaseErrorCode.Network,
            Api.BillingResponseCode.ItemNotOwned => InAppPurchaseErrorCode.InvalidState,
            _ => InAppPurchaseErrorCode.Unknown
        };
        var message = String.IsNullOrWhiteSpace(result.DebugMessage)
            ? $"Failed to {operation}: {result.ResponseCode}"
            : $"Failed to {operation}: {result.ResponseCode} - {result.DebugMessage}";

        return new InAppPurchaseException(code, message)
        {
            NativeErrorCode = result.ResponseCode.ToString()
        };
    }


    static decimal FromMicros(long micros) => micros / 1_000_000m;


    sealed class InFlightPurchase(string productId)
    {
        public string ProductId { get; } = productId;
        public TaskCompletionSource<(Api.BillingResult Result, List<Api.Purchase> Purchases)> Completion { get; }
            = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
