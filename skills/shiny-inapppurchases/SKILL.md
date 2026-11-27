---
name: shiny-inapppurchases
description: Generate code using Shiny.Mobile.InAppPurchases (in-app purchases over StoreKit 2 on iOS and Google Play Billing 9 on Android) and Shiny.Mobile.InAppPurchases.Server (ASP.NET Core purchase verification, App Store Server Notifications V2 and Google Play Real-time Developer Notifications)
auto_invoke: true
triggers:
  - in-app purchase
  - in app purchase
  - iap
  - storekit
  - storekit 2
  - google play billing
  - play billing
  - billing client
  - subscriptions
  - consumable
  - non-consumable
  - app store server notifications
  - real-time developer notifications
  - rtdn
  - purchase verification
  - receipt validation
  - Shiny.Mobile.InAppPurchases
  - IInAppPurchaseManager
  - IPurchaseDelegate
  - AddInAppPurchases
  - StoreProduct
  - SubscriptionOffer
  - PricingPhase
  - Purchase
  - PurchaseResult
  - PurchaseResultStatus
  - PurchaseOptions
  - PurchaseState
  - IsVerified
  - VerificationError
  - unverified transaction
  - SubscriptionReplacement
  - InAppPurchaseException
  - InAppPurchaseErrorCode
  - Shiny.Mobile.InAppPurchases.Server
  - AddInAppPurchaseServer
  - MapInAppPurchaseWebhooks
  - MapInAppPurchaseVerification
  - IPurchaseEventHandler
  - PurchaseEvent
  - PurchaseEventType
  - IPurchaseVerifier
  - VerifiedPurchase
  - IAppleStoreClient
  - IGooglePlayClient
  - IPurchaseEventDeduplicator
  - IsConsumed
  - replay protection
  - consumptionState
---

# Shiny.Mobile.InAppPurchases

In-app purchases (consumables, non-consumables, auto-renewable subscriptions) for iOS and Android, plus an ASP.NET Core
backend. This is **not** Apple Pay / Google Pay card processing: those go through a payment processor, and Apple/Google
send no notifications for them.

| Package | Targets | Use in |
|---|---|---|
| `Shiny.Mobile.InAppPurchases` | `net11.0-ios` (15+), `net11.0-android` (API 26+), `net11.0` (contracts only) | The app (any Shiny host, MAUI not required) |
| `Shiny.Mobile.InAppPurchases.Server` | `net11.0` + ASP.NET Core | Your backend |

Namespaces: `Shiny.InAppPurchases` (client) and `Shiny.InAppPurchases.Server` (server); the registration extensions (`AddInAppPurchases`, `AddInAppPurchaseServer`, `MapInAppPurchaseWebhooks`, `MapInAppPurchaseVerification`) live in `Shiny`.

## Rules — always follow

1. **Never grant a `Pending` purchase.** It is redelivered through `IPurchaseDelegate` when it resolves.
2. **Verify, grant, then finish.** `PurchaseAsync` never finishes anything. Call `FinishPurchaseAsync` only after the entitlement is
   stored. Google Play refunds purchases not acknowledged within **3 days** (minutes for license testers).
3. **Consumable vs non-consumable is decided at finish time**: `FinishPurchaseAsync(purchase, consume: true)` for things that can be
   bought again. Google Play does not model consumables, and `ProductType` is only `OneTime` / `Subscription`.
4. **Verify on the server** with `Purchase.VerificationData` (Apple signed JWS / Google purchase token). Don't trust the device.
5. **Never grant an unverified purchase.** On Apple, a transaction that fails StoreKit's on-device verification arrives
   like any other (from `PurchaseAsync` as `Success`, from `GetUnfinishedPurchasesAsync`, `GetEntitlementsAsync` and
   `IPurchaseDelegate`) with `Purchase.IsVerified == false` and the reason in `VerificationError`. Log it and finish it
   without granting, or send it to the server, which rejects the JWS - but always finish it, or StoreKit redelivers it on
   every launch. Google Play purchases are always `IsVerified == true` (Play verification is server-side only).
6. **Handle updates idempotently**, keyed on `TransactionId` on the client and `NotificationId` / `TransactionId` on the server.
7. Always set `PurchaseOptions.AccountToken` (a `Guid`, your user id). It comes back as Apple `appAccountToken` /
   Google `obfuscatedAccountId`, both on-device and in server notifications.
8. Libraries use Shiny.Core only. Do not suggest MAUI Essentials APIs inside library code.
9. **Replay protection on the server.** A verified purchase can be sent again (a retry, or a captured token/JWS replayed),
   and a finished Apple consumable or a consumed Google token still verifies with `IsActive == true`. Grant at most once
   per `VerifiedPurchase.TransactionId` (Apple `transactionId`, Google `orderId`) - store it with a unique constraint and
   treat a repeat as "already granted". On Google one-time products also refuse a token with `IsConsumed == true` (it was
   already granted and finished). `IsConsumed` is null on Apple (not reported) and for subscriptions.

## Client registration

```csharp
using Shiny;

builder.Services.AddInAppPurchases();                         // manager only
builder.Services.AddInAppPurchases<MyPurchaseDelegate>();     // + out-of-band updates (recommended)
```

The platform listener (StoreKit `Transaction.updates`, Play `PurchasesUpdatedListener` plus a launch-time recovery query)
starts as an `IShinyStartupTask`, so updates that arrive while no page is open are not lost. On a MAUI host this needs
`.UseShiny()`.

## IInAppPurchaseManager

```csharp
public interface IInAppPurchaseManager
{
    StorePlatform Platform { get; }                              // AppStore | GooglePlay
    Task<bool> CanMakePaymentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StoreProduct>> GetProductsAsync(IEnumerable<string> productIds, CancellationToken ct = default);
    Task<PurchaseResult> PurchaseAsync(string productId, PurchaseOptions? options = null, CancellationToken ct = default);
    Task<IReadOnlyList<Purchase>> GetEntitlementsAsync(CancellationToken ct = default);          // owned non-consumables + active subs
    Task<IReadOnlyList<Purchase>> GetUnfinishedPurchasesAsync(CancellationToken ct = default);   // paid but never finished
    Task FinishPurchaseAsync(Purchase purchase, bool consume, CancellationToken ct = default);
    Task<IReadOnlyList<Purchase>> RestorePurchasesAsync(CancellationToken ct = default);        // iOS: AppStore.sync (may prompt) - user-initiated only
    Task ShowManageSubscriptionsAsync(string? productId = null, CancellationToken ct = default);
    event EventHandler<Purchase>? PurchaseUpdated;
}
```

`GetProductsAsync` omits unknown ids instead of throwing. Genuine failures throw `InAppPurchaseException` with `ErrorCode`:
`StoreUnavailable`, `NotAllowed`, `ProductNotFound`, `ProductUnavailable`, `Network`, `DeveloperError`,
`VerificationFailed`, `NoUserInterface`, `InvalidState`, `Unknown` (plus `NativeErrorCode`). A user cancelling is **not** an exception.

### Purchasing

```csharp
public class StoreViewModel(IInAppPurchaseManager purchases, MyApi api)
{
    public async Task Buy(StoreProduct product)
    {
        try
        {
            var result = await purchases.PurchaseAsync(product.Id, new PurchaseOptions
            {
                AccountToken = currentUser.Id
            });

            switch (result.Status)
            {
                case PurchaseResultStatus.Success when !result.Purchase!.IsVerified:
                    // Apple: failed on-device verification - never grant. Finish so StoreKit stops redelivering it.
                    logger.LogWarning("Unverified purchase {Id}: {Error}", result.Purchase.TransactionId, result.Purchase.VerificationError);
                    await purchases.FinishPurchaseAsync(result.Purchase, consume: product.Id == "coins_100");
                    break;

                case PurchaseResultStatus.Success:
                    await api.VerifyAndGrant(result.Purchase!.VerificationData, result.Purchase.ProductId);
                    await purchases.FinishPurchaseAsync(result.Purchase!, consume: product.Id == "coins_100");
                    break;

                case PurchaseResultStatus.Pending:       // Ask to Buy / slow payment - do NOT grant
                case PurchaseResultStatus.Cancelled:
                    break;

                case PurchaseResultStatus.AlreadyOwned:  // Google Play only - offer Restore Purchases
                    break;
            }
        }
        catch (InAppPurchaseException ex) when (ex.ErrorCode == InAppPurchaseErrorCode.Network)
        {
            // show retry
        }
    }
}
```

### Out-of-band updates

```csharp
public class MyPurchaseDelegate(IInAppPurchaseManager purchases, MyApi api) : IPurchaseDelegate
{
    public async Task OnPurchaseUpdated(Purchase purchase)
    {
        switch (purchase.State)
        {
            case PurchaseState.Purchased when !purchase.IsVerified && !purchase.IsFinished:
                // Apple only - log, never grant, finish so it is not redelivered
                await purchases.FinishPurchaseAsync(purchase, consume: IsConsumable(purchase.ProductId));
                break;

            case PurchaseState.Purchased when !purchase.IsFinished:
                await api.VerifyAndGrant(purchase.VerificationData, purchase.ProductId);
                await purchases.FinishPurchaseAsync(purchase, consume: IsConsumable(purchase.ProductId));
                break;

            case PurchaseState.Revoked:   // Apple refund / Family Sharing removed
                await api.Revoke(purchase.OriginalTransactionId);
                break;
        }
    }
}
```

Delegates are resolved lazily, so they may inject `IInAppPurchaseManager`. At app start, also call
`GetUnfinishedPurchasesAsync()` and run each purchase through the same verify → grant → finish path.

### Subscriptions & offers

- `StoreProduct.SubscriptionOffers`:
  - **Google:** one entry per base plan and per eligible offer. Pass `SubscriptionOffer.OfferToken` as
    `PurchaseOptions.OfferToken` (defaults to the first base plan).
  - **Apple:** the introductory offer (only if eligible, applied automatically) plus promotional offers (ids only; signed
    promotional offer purchases are not supported).
- `PricingPhase` has `BillingPeriod` (ISO 8601 `P1W`/`P1M`/`P1Y`), `BillingCycleCount` (0 = recurring), and `PaymentMode`
  (`Recurring`, `FreeTrial`, `PayAsYouGo`, `PayUpFront`).
- Google upgrades/downgrades:
  `PurchaseOptions.Replacement = new SubscriptionReplacement(oldProductId, oldPurchaseToken, SubscriptionReplacementMode.ChargeProratedPrice)`.
  Apple handles changes within a subscription group and ignores `Replacement`.
- `PurchaseOptions.Quantity`: Apple consumables only (1–10).

### Purchase record mapping

| Property | Apple | Google |
|---|---|---|
| `TransactionId` | transaction id (new per renewal) | order id (purchase token while pending) |
| `OriginalTransactionId` | original transaction id | purchase token |
| `VerificationData` | signed JWS | purchase token |
| `ExpirationDate` | set for subscriptions | always null (ask the server) |
| `IsFinished` | finished | acknowledged |
| `Environment` | Production / Sandbox / Xcode | Unknown |
| `Signature` | null | RSA signature of `OriginalJson` |
| `IsVerified` / `VerificationError` | false + reason when StoreKit verification fails | always true / null |

## Server

```csharp
using Shiny;
using Shiny.InAppPurchases.Server;

builder.Services
    .AddInAppPurchaseServer(options => builder.Configuration.GetSection("InAppPurchases").Bind(options))
    .AddPurchaseEventHandler<MyPurchaseEventHandler>()   // scoped, multiple allowed, run in order
    .UseDeduplicator<MyDistributedDeduplicator>();       // optional - default is in-memory per process

app.MapInAppPurchaseWebhooks("/iap");                                  // POST /iap/apple (ASSN V2), POST /iap/google (Pub/Sub push)
app.MapInAppPurchaseVerification("/iap/verify").RequireAuthorization();  // body: { platform, verificationData, productId }
```

Options (`InAppPurchaseServerOptions`):
- **`Apple`** (`AppleStoreOptions`): `BundleId` (required), `AppAppleId` (required for Production notifications), `AllowSandbox` (default true),
  `UseSandboxServerApi`, `EnableOnlineRevocationCheck`, `IssuerId` + `KeyId` + `PrivateKey` (.p8 PEM; all three or none).
- **`Google`** (`GooglePlayOptions`): `PackageName` (required), `ServiceAccountJson` (needed to verify purchases and fetch notification details),
  `RequirePubSubAuthentication` (default true), `PubSubAudience`, `PubSubServiceAccountEmail`.
- **General:** a null store section disables that endpoint. `DeduplicationWindow` (7 days), `DeduplicationCapacity`. Validated at startup.

```csharp
public class MyPurchaseEventHandler(MyDb db) : IPurchaseEventHandler
{
    public async Task HandleAsync(PurchaseEvent e, CancellationToken ct)
    {
        switch (e.Type)
        {
            case PurchaseEventType.Purchased:
            case PurchaseEventType.Renewed:
            case PurchaseEventType.Recovered:
            case PurchaseEventType.Restarted:
                await db.Grant(e.AccountToken, e.ProductId, e.OriginalTransactionId, e.ExpiresAt, ct);
                break;
            case PurchaseEventType.Expired:
            case PurchaseEventType.Refunded:
            case PurchaseEventType.Revoked:
                await db.Revoke(e.OriginalTransactionId, ct);
                break;
        }
    }
}
```

`PurchaseEvent` properties:
- **Identity & type:** `NotificationId`, `Platform`, `Type`, `RawType` (e.g. `SUBSCRIBED/INITIAL_BUY`, `SUBSCRIPTION_PURCHASED`).
- **Purchase:** `ProductId`, `TransactionId`, `OriginalTransactionId` (Apple original id / Google purchase token), `AccountToken`, `Environment`,
  `OccurredAt`, `ExpiresAt`, `IsAutoRenewing`, `Quantity`.
- **Raw store detail:** `Apple` (verified notification, transaction, renewal info), `Google` (notification plus the fetched
  subscription or product).

`PurchaseEventType`: `Purchased`, `Renewed`, `RenewalFailed`, `GracePeriodStarted`, `GracePeriodExpired`,
`AutoRenewDisabled`, `AutoRenewEnabled`, `PlanChanged`, `Expired`, `Refunded`, `RefundDeclined`, `RefundReversed`, `Revoked`,
`Paused`, `OnHold`, `Recovered`, `Restarted`, `PriceChange`, `PendingCanceled`, `ConsumptionRequest`, `OfferRedeemed`, `Test`, `Other`.

Status codes:
- `200`: processed or duplicate.
- `401`: bad JWS chain or OIDC token.
- `400`: malformed body, or wrong bundle / package / environment.
- `502`: Google API failure (retried).
- `500`: a handler threw. Not de-duplicated, so the store retries.
- `404`: that store isn't configured.

Direct access:
- `IPurchaseVerifier.VerifyAsync(new PurchaseVerificationRequest { Platform, VerificationData, ProductId })` returns
  `VerifiedPurchase` (`IsValid`, `Error`, `IsActive`, `Kind`, ids, `AccountToken`, `ExpiresAt`, `Environment`, `IsAcknowledged`,
  `IsConsumed` - Google one-time products only, from `consumptionState`; null on Apple and for subscriptions).
  Apple verifies offline, Google calls the Play Developer API.
- `IAppleStoreClient`: `VerifyNotification`, `VerifyTransaction`, `VerifyRenewalInfo`, `GetTransactionAsync`,
  `GetSubscriptionStatusesAsync`, `RequestTestNotificationAsync(sandbox)`.
- `IGooglePlayClient`: `GetSubscriptionAsync`, `GetProductAsync` (null when not found), `AcknowledgeSubscriptionAsync`,
  `AcknowledgeProductAsync`, `ConsumeProductAsync`.

Always grant to the **authenticated user** in your verify endpoint, never to an id taken from the request body.

```csharp
var result = await verifier.VerifyAsync(new PurchaseVerificationRequest { Platform = dto.Platform, VerificationData = dto.VerificationData, ProductId = dto.ProductId });
if (!result.IsValid || !result.IsActive)
    return Results.BadRequest(result.Error);

if (await db.HasGrant(result.Platform, result.TransactionId!))   // Apple transactionId / Google orderId
    return Results.Ok();                                          // already granted - idempotent, not an error

if (result.IsConsumed == true)                                    // Google: consumed token we have no record of
    return Results.Conflict("Purchase was already consumed");

await db.Grant(user.GetUserId(), result.Platform, result.TransactionId!, result.ProductId, result.Quantity ?? 1);
```

## Store setup & testing pointers

- **Apple products don't load:** the Paid Apps agreement is not active, the bundle id doesn't match, a price or localization is missing, or the change is less than an hour old.
- **Apple testing:** development-signed or TestFlight builds on a physical device with a Sandbox Apple Account (Settings → Developer).
  `.storekit` files are Xcode-only.
- **Google testing:** license testers (Play Console → Settings → License testing) can sideload debug builds when the package name matches.
  Slow test cards exercise `Pending`. Subscriptions renew every ~5 minutes.
- **Server notifications:** App Store Connect → App Information → App Store Server Notifications (Version 2) → `/iap/apple`.
  Google: RTDN Pub/Sub topic (grant `google-play-developer-notifications@system.gserviceaccount.com` Publisher) → authenticated push subscription → `/iap/google`.
- **Server testing without a device:** with `InAppPurchases__Google__RequirePubSubAuthentication=false`, POST a Pub/Sub envelope
  (`{"message":{"data":"<base64 DeveloperNotification>","messageId":"..."}}`) to `/iap/google`; a `testNotification` needs no Play API call.
  Apple JWS can't be forged - use `RequestTestNotificationAsync(sandbox: true)` or real sandbox purchases. Apple `Xcode`/`LocalTesting`
  transactions are rejected. Unit test `IPurchaseEventHandler` by constructing a `PurchaseEvent` (required: `NotificationId`, `Platform`, `Type`, `RawType`).
- Step-by-step testing (mobile + server, both stores): https://shinylib.net/client/inapppurchases/testing
- Full walkthrough, production and testing: `samples/Sample.InAppPurchases.Maui/readme.md` in https://github.com/shinyorg/shiny
