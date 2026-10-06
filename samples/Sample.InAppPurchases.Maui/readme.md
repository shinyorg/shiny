# Shiny.Mobile.InAppPurchases — MAUI Sample

A complete in-app purchase flow for iOS (StoreKit 2) and Android (Google Play Billing 9):

| Product id | Type | Demonstrates |
|---|---|---|
| `coins_100` | Consumable | Buying the same thing repeatedly — finished with `consume: true` |
| `remove_ads` | Non-consumable | Permanent unlock, restore on reinstall / new device |
| `premium_monthly` | Auto-renewable subscription | Renewals, expiry, cancellation, managing the subscription |

Plus pending purchases (Ask to Buy / slow payment methods), out-of-band updates through `IPurchaseDelegate`,
crash-safe recovery of unfinished purchases, and server verification against [`samples/Sample.InAppPurchases.Server`](../Sample.InAppPurchases.Server).

- [How the sample works](#how-the-sample-works)
- [1. Configure the sample](#1-configure-the-sample)
- [2. Apple — App Store Connect](#2-apple--app-store-connect)
  - [One-time setup](#apple-one-time-setup) · [Testing](#apple-testing) · [Production](#apple-production)
- [3. Google Play](#3-google-play)
  - [One-time setup](#google-one-time-setup) · [Testing](#google-testing) · [Production](#google-production)
- [4. The backend](#4-the-backend)
- [5. Test plan](#5-test-plan)
- [Troubleshooting](#troubleshooting)

---

## How the sample works

```
PurchaseAsync ─┐
               ├─> EntitlementService.ProcessAsync ─> POST /iap/verify ─> grant ─> FinishPurchaseAsync
IPurchaseDelegate ┘        (pending? stop)            (Sample.InAppPurchases.Server)
```

| File | Role |
|---|---|
| `SampleConfig.cs` | Product ids, which ones are consumable, and the server URL. **The only file you need to edit.** |
| `EntitlementService.cs` | Verify → grant → finish, once per transaction. Also recovers unfinished purchases at startup. |
| `PurchaseApi.cs` | Calls the server's verification endpoint. With no server configured it runs in **local test mode**. |
| `SamplePurchaseDelegate.cs` | Out-of-band updates (approvals, renewals, refunds) go through the same `EntitlementService`. |
| `MainPage.cs` | Product list, entitlement summary, Restore Purchases, Manage Subscriptions. |

Three rules the sample follows that your app should too:

1. **Never grant a pending purchase.** It comes back through `IPurchaseDelegate` when it clears.
2. **Finish only after the grant is durable.** If the app dies in between, the purchase is redelivered on the next launch
   (`GetUnfinishedPurchasesAsync`). Google Play **refunds purchases that aren't acknowledged within 3 days** (minutes for
   license testers).
3. **Verify on a server.** Anything on the device can be tampered with. `Purchase.VerificationData` is an Apple-signed JWS
   or a Google purchase token that only your server can check.

---

## 1. Configure the sample

1. **Application id** — in `Sample.InAppPurchases.Maui.csproj` change `<ApplicationId>net.shinylib.paysample</ApplicationId>` to *your*
   bundle id / package name. It must exactly match the app in App Store Connect and the Play Console, or no products load.
2. **Product ids** — create products with the ids in `SampleConfig.cs` (or change the constants to match yours).
3. **Server** — leave `SampleConfig.ServerUrl = null` to start in **local test mode** (orange banner). Once
   [the backend](#4-the-backend) is running, set it to the server's public HTTPS URL (green banner).

> Local test mode trusts the device. It exists so you can see the purchase sheets before your server is ready.
> Never ship with `ServerUrl = null`.

---

## 2. Apple — App Store Connect

<a id="apple-one-time-setup"></a>
### One-time setup (needed for testing *and* production)

1. **Apple Developer Program** membership (paid).
2. **Agreements, Tax, and Banking** — the Account Holder must accept the **Paid Apps** agreement, and bank and tax info
   must be complete. Until the agreement is *Active*, `GetProductsAsync` silently returns nothing.
3. **Identifier** — in [Certificates, Identifiers & Profiles](https://developer.apple.com/account/resources/identifiers/list),
   register an explicit App ID with your bundle id. *In-App Purchase* is enabled by default. No entitlements file is needed.
4. **App record** — App Store Connect → **Apps → +** → New App, using that bundle id.
5. **Products** — in your app, go to **Monetization**:
   - **In-App Purchases → +** → *Consumable*, product id `coins_100`.
   - **In-App Purchases → +** → *Non-Consumable*, product id `remove_ads`.
   - **Subscriptions** → create a subscription group (e.g. *Premium*) → **+** → product id `premium_monthly`,
     duration 1 month. Optionally add an **Introductory Offer** (e.g. 1 week free) to see free-trial pricing phases.
   - For each product, add a **price** and at least one **localization** (display name + description).
     Sandbox needs only reference name, product id, localized name and price. Production also needs a review
     screenshot and review notes (*Ready to Submit*).

> Product changes can take **up to an hour** to reach the sandbox.

<a id="apple-testing"></a>
### Testing (Sandbox)

Development-signed builds and TestFlight builds always use the **sandbox**, which never charges real money.

**Create sandbox testers.** In App Store Connect → **Users and Access → Sandbox → +**, create a Sandbox Apple Account. Use an
email address that isn't already an Apple Account; plus-addressing like `you+sandbox1@example.com` works. Create a few, because
purchase history is per tester.

**Run on a physical device.** Sandbox testing on the Simulator is unreliable, and StoreKit configuration files
(`.storekit`) only work when launching from an Xcode scheme, so they're not available to `dotnet build`.

1. Enable **Developer Mode** on the device (Settings → Privacy & Security → Developer Mode).
2. Make sure a development provisioning profile exists for your bundle id (automatic provisioning in your IDE is fine),
   then deploy:
   ```bash
   dotnet build samples/Sample.InAppPurchases.Maui -f net10.0-ios -t:Run -p:RuntimeIdentifier=ios-arm64
   ```
3. Tap a product. The first purchase attempt prompts you to sign in; use the **Sandbox Apple Account**.
   You don't need to sign out of your real Apple Account. The sheet shows **`[Environment: Sandbox]`**. If it doesn't,
   you're not in the sandbox.
4. After that first attempt, the tester appears under **Settings → Developer → Sandbox Apple Account**.

**TestFlight.** Upload a build (see [Production](#apple-production) for building an `.ipa`) and install it from TestFlight.
Purchases are sandbox purchases. To use the sandbox controls below with a TestFlight build, sign out of
**Settings → [your name] → Media & Purchases**, then sign in under **Settings → Developer → Sandbox Apple Account**.
Consider a dedicated test device for this.

**Sandbox controls.** Open them from **Settings → Developer → Sandbox Apple Account → Manage**, or from the tester in
App Store Connect:

| Control | Use it to test |
|---|---|
| **Subscription Renewal Rate** | Default is *every 5 minutes*: a 1-month subscription renews every 5 min, 1-year every hour. There are also 3-minute, 30-minute and hourly presets. Subscriptions renew **up to 12 times**, then expire. |
| **Interrupted Purchases** (App Store Connect → tester → *Interrupt Purchases for This Tester*) | The purchase needs extra user action, so `PurchaseAsync` returns **Pending** and the completed purchase then arrives through `IPurchaseDelegate`. |
| **Clear Purchase History** | Buy `remove_ads` again, and become eligible for introductory offers again. Sign out and back in afterwards to clear the device cache. |
| **Country or Region** (App Store Connect only) | Localized prices and storefront availability. Sign out and back in on the device afterwards. |

**Ask to Buy** — create a **Sandbox Test Family** in App Store Connect and purchase as a child member. The organizer
approves or declines, and the sample receives the result through the delegate.

**Server notifications in sandbox** — App Store Connect → your app → **App Information → App Store Server Notifications →
Sandbox Server URL** → `https://<your-server>/iap/apple`, **Version 2**. Sandbox purchases and renewals then reach
`Sample.InAppPurchases.Server` with `Environment = Sandbox`.

<a id="apple-production"></a>
### Production

1. **Distribution signing** — create an *Apple Distribution* certificate and an **App Store** provisioning profile for
   your bundle id.
2. **Build the archive / `.ipa`:**
   ```bash
   dotnet publish samples/Sample.InAppPurchases.Maui -f net10.0-ios -c Release \
     -p:ArchiveOnBuild=true \
     -p:RuntimeIdentifier=ios-arm64 \
     -p:CodesignKey="Apple Distribution: Your Company (TEAMID)" \
     -p:CodesignProvision="Your App Store Profile Name"
   ```
   The `.ipa` lands in `bin/Release/net10.0-ios/ios-arm64/publish/`. Upload it with the **Transporter** app. Bump
   `ApplicationDisplayVersion` / `ApplicationVersion` for every upload.
3. **Submit your first in-app purchases with an app version.** On the version page, under
   **In-App Purchases and Subscriptions**, select the products (they must be *Ready to Submit*). Later products can be
   submitted on their own.
4. **Production server notifications** — **App Information → App Store Server Notifications → Production Server URL** →
   `https://<your-server>/iap/apple`, **Version 2**.
5. **Server credentials** (see [the backend](#4-the-backend)):
   - **Apple ID** of the app (App Information → *Apple ID*, a number). This is required to accept production notifications.
   - **In-App Purchase key** — **Users and Access → Integrations → In-App Purchase → +**. Note the **Issuer ID** and **Key ID**,
     and download the `.p8`. **It can only be downloaded once.**
6. **Release checklist** — `SampleConfig.ServerUrl` points at your production server, `AccountToken` is your real user id,
   and nothing grants a `Pending` purchase.

> Local Xcode note: the .NET iOS workload pins an Xcode version. If the build stops with an Xcode version error, install the
> matching Xcode, or pass `-p:ValidateXcodeVersion=false` for local experiments.

---

## 3. Google Play

<a id="google-one-time-setup"></a>
### One-time setup (needed for testing *and* production)

1. **Google Play Console** developer account, plus a **payments profile** (Settings → *Payments profile*) so you can sell.
2. **Create the app** with the same package name as `ApplicationId`.
3. **Create an upload key** (keep it and its password safe):
   ```bash
   keytool -genkeypair -v -keystore upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
   ```
4. **Build a signed App Bundle:**
   ```bash
   export UPLOAD_KEY_PASSWORD=...
   dotnet publish samples/Sample.InAppPurchases.Maui -f net10.0-android -c Release \
     -p:AndroidPackageFormat=aab \
     -p:AndroidKeyStore=true \
     -p:AndroidSigningKeyStore=$PWD/upload.keystore \
     -p:AndroidSigningKeyAlias=upload \
     -p:AndroidSigningKeyPass=env:UPLOAD_KEY_PASSWORD \
     -p:AndroidSigningStorePass=env:UPLOAD_KEY_PASSWORD
   ```
   Upload `bin/Release/net10.0-android/publish/*-Signed.aab`. Increase `ApplicationVersion` (the version code) for every upload.
5. **Upload it to the Internal testing track** (Testing → Internal testing → Create new release) and enroll in
   **Play App Signing** when asked. The Play Console won't let you create products until it has seen a build that uses
   Play Billing. The library adds the `com.android.vending.BILLING` permission for you.
6. **Products** — **Monetize with Play → Products**:
   - **One-time products → Create** → `coins_100`, and again for `remove_ads`. Add a purchase option with a price, then
     **Activate**. Play doesn't know which one is consumable; the sample decides that in `SampleConfig.IsConsumable`.
   - **Subscriptions → Create** → `premium_monthly` → add a **base plan** (auto-renewing, monthly, with a price) →
     **Activate**. Optionally add an **offer** (e.g. a free trial) to the base plan. Offers show up as extra entries in
     `StoreProduct.SubscriptionOffers`.

<a id="google-testing"></a>
### Testing

**License testers are the key.** Add tester Google accounts under **Play Console → Settings → License testing**. For those
accounts:

- **Sideloaded debug builds work.** You can `dotnet build -t:Run` straight to a device with debug signing, as long as the
  **package name matches** the Play Console app. You don't need to upload every build.
- Purchases use **test payment methods**, so nobody is charged:

  | Test instrument | Use it to test |
  |---|---|
  | Test instrument, always approves | Happy path |
  | Test instrument, always declines | Failed payment → `InAppPurchaseException` |
  | Slow test card, approves after a few minutes | `PurchaseResultStatus.Pending`, then the grant arrives via `IPurchaseDelegate` |
  | Slow test card, declines after a few minutes | Pending that never grants (restart the app and confirm nothing was granted) |
  | Test card, approves then charges back | Chargeback → server `Refunded`/`Revoked` event |

- **Subscriptions renew fast** (at most **6 renewals**): 1 week or 1 month → 5 min · 3 months → 10 min · 6 months → 15 min ·
  1 year → 30 min. Free trials last 3 min, grace period 5 min, account hold 10 min.
- ⚠️ **Unacknowledged tester purchases are refunded after a few minutes**, not 3 days. Forgetting `FinishPurchaseAsync`
  shows up very quickly.

**Run it:**

1. Use a physical device, or an emulator created from a **Google Play** system image (not "Google APIs"). Sign in to the
   Play Store with a license tester account. On devices with several accounts, the purchase uses the account that installed the app.
2. Deploy: `dotnet build samples/Sample.InAppPurchases.Maui -f net10.0-android -t:Run`
3. Tap a product. The Play sheet shows **"Test card, always approves"**, which confirms you're testing.

**Internal / closed testing tracks** — use these for QA builds installed from Play. It can take a few hours for a new
release to reach testers. Testers who aren't license testers **are charged real money**. Apps that are still drafts or
only on internal testing have spend limits.

**Play Billing Lab** ([Play Store](https://play.google.com/store/apps/details?id=com.google.android.apps.play.billingtestcompanion))
— signed in as the license tester, it lets you change the Play country, re-test free trials with the same account, and
push a subscription into payment-declined states. Configurations expire after 2 hours.

**Real-time developer notifications while testing** — set up Pub/Sub as described under [Production](#google-production).
Tester purchases trigger real notifications, and **Monetization setup → Send test notification** sends a `Test` event.

<a id="google-production"></a>
### Production

1. **Publish** to the production track (closed/open testing first, if you like).
2. **Real-time developer notifications (RTDN):**
   1. In [Google Cloud](https://console.cloud.google.com/), create a project (or reuse one) and a **Pub/Sub topic**, e.g. `play-billing`.
   2. On the topic, grant `google-play-developer-notifications@system.gserviceaccount.com` the **Pub/Sub Publisher** role.
   3. Play Console → **Monetize with Play → Monetization setup** → enter `projects/<project-id>/topics/play-billing` →
      **Send test notification**.
   4. Create a **push subscription** on the topic with endpoint `https://<your-server>/iap/google`. Enable
      **authentication**, pick a service account, and set an **audience** (e.g. the endpoint URL). Give the server the
      same service account email and audience so it can validate the OIDC token Pub/Sub sends.
3. **Google Play Developer API** — notifications only say *something changed*, so the server looks up the details:
   1. In Google Cloud, enable the **Google Play Android Developer API**.
   2. Create a **service account** and download a **JSON key**.
   3. Play Console → **Users and permissions → Invite new users** → the service account's email → grant **View financial data**
      and **Manage orders and subscriptions**. It can take a while (sometimes up to a day) before API calls stop returning 401/403.
4. **Release checklist** — `SampleConfig.ServerUrl` points at production, `AccountToken` is your real user id, every purchase
   is finished after granting, and you've run one purchase with an account that **isn't** a license tester (refund it from
   **Order management**).

---

## 4. The backend

`samples/Sample.InAppPurchases.Server` hosts the three endpoints the stores and the app talk to:

| Endpoint | Called by | Configure in |
|---|---|---|
| `POST /iap/apple` | Apple (App Store Server Notifications V2) | App Store Connect → App Information (Production + Sandbox URL) |
| `POST /iap/google` | Google Cloud Pub/Sub push (RTDN) | Pub/Sub push subscription |
| `POST /iap/verify` | This app (`PurchaseApi.cs`) | `SampleConfig.ServerUrl` |

The stores need a **public HTTPS** URL, and so does a physical device. Android also blocks plain `http://` by default.
During development, run the server (it listens on `http://localhost:5000` and `https://localhost:5001`) and expose it with a tunnel:

```bash
dotnet run --project samples/Sample.InAppPurchases.Server
devtunnel host -p 5000 --allow-anonymous      # or: ngrok http 5000
```

Put the tunnel URL in `SampleConfig.ServerUrl`, and use `<tunnel>/iap/apple` and `<tunnel>/iap/google` in the stores'
notification settings. `Sample.InAppPurchases.Server` binds the `InAppPurchases` section of `appsettings.json`:

| Setting | Where it comes from | Needed for |
|---|---|---|
| `InAppPurchases:Apple:BundleId` | Your `ApplicationId` | Everything Apple |
| `InAppPurchases:Apple:AppAppleId` | App Store Connect → App Information → Apple ID (numeric) | Accepting **production** notifications |
| `InAppPurchases:Apple:AllowSandbox` | `true` while testing | Sandbox / TestFlight notifications and verification |
| `InAppPurchases:Apple:IssuerId`, `KeyId`, `PrivateKey` | Users and Access → Integrations → In-App Purchase (`.p8` contents) | Optional: App Store Server API lookups and test notifications |
| `InAppPurchases:Google:PackageName` | Your `ApplicationId` | Everything Google |
| `InAppPurchases:Google:ServiceAccountJson` | Google Cloud service account key, invited in the Play Console | **Required** to verify Google purchases and fetch notification details |
| `InAppPurchases:Google:PubSubAudience`, `PubSubServiceAccountEmail` | The push subscription's authentication settings | Authenticating RTDN pushes |

Apple purchases verify **offline** (the JWS is checked against Apple's root certificate), so `/iap/verify` works for iOS
with just the bundle id. Google purchases can only be verified through the Play Developer API, so they need `ServiceAccountJson`.

Keep secrets out of source control:

```bash
cd samples/Sample.InAppPurchases.Server
dotnet user-secrets set "InAppPurchases:Apple:PrivateKey" "$(cat SubscriptionKey_XXXXXXXXXX.p8)"
dotnet user-secrets set "InAppPurchases:Google:ServiceAccountJson" "$(cat service-account.json)"
```

Remove the `Apple` or `Google` section entirely to turn off that store's endpoint. `Sample.InAppPurchases.Server` also exposes
`POST /iap/apple/test?sandbox=true`, which asks Apple to send a TEST notification to your configured URL (this needs the
Server API key). Every event is logged by `LoggingPurchaseEventHandler`. That class is where your real app updates its
database.

> In a real app, protect `/iap/verify` with your authentication (`.RequireAuthorization()`) and grant the entitlement to
> the **authenticated user** on the server. Treat the app's own copy of entitlements as a cache.

---

## 5. Test plan

Run each scenario on **both** platforms before shipping:

| # | Scenario | Expected |
|---|---|---|
| 1 | Buy `coins_100` twice | +100 coins each time. The second purchase works because the first was consumed. |
| 2 | Buy `remove_ads`, delete the app, reinstall, tap **Restore Purchases** | *Ads removed: yes* |
| 3 | Buy `remove_ads` again | iOS: the sheet says you already own it. Android: `AlreadyOwned`. |
| 4 | Subscribe to `premium_monthly` and wait for renewals | *Premium: active*. `Sample.InAppPurchases.Server` logs `Renewed` events. |
| 5 | Cancel via **Manage Subscriptions** and wait for expiry | Server logs `AutoRenewDisabled`, then `Expired`. After a relaunch, *Premium: no*. |
| 6 | Cancel the purchase sheet | "Cancelled", nothing granted |
| 7 | Pending: iOS interrupted purchase / Ask to Buy · Android slow test card | "Waiting for approval…", then granted automatically once it clears (via the delegate) |
| 8 | Kill the app right after paying, before the grant | On relaunch the purchase is recovered and granted exactly once |
| 9 | Refund: Android **Order management → Refund** · iOS via [reportaproblem.apple.com](https://reportaproblem.apple.com) (production) | Server logs `Refunded`/`Revoked`. Entitlement removed. |
| 10 | Airplane mode, then purchase | `InAppPurchaseException` with `ErrorCode = Network` |
| 11 | Server down (set a bad `ServerUrl`) | Purchase isn't finished. It's retried on the next launch. |

---

## Troubleshooting

**No products load**
- The bundle id / package name doesn't match the store app exactly.
- **Apple:** the Paid Apps agreement isn't active, a product is missing a price or localization, the change was made less
  than an hour ago, or you're on a Simulator.
- **Google:** the product or base plan isn't **Active**, the device isn't signed in with a **license tester** (or the app
  wasn't installed from a testing track), or you're on an emulator without the Play Store. Clearing the Play Store app's
  cache often helps after catalog changes.

**`[Environment: Sandbox]` doesn't appear (iOS)** — the build is signed for distribution, or you signed in with a real
Apple Account. Use a development-signed build and a Sandbox Apple Account.

**`DeveloperError` on Android** — usually a subscription without an active base plan, or a stale offer token.

**Purchases are refunded on Android** — the purchase was never acknowledged. Make sure `FinishPurchaseAsync` runs after granting.
License tester purchases are refunded within minutes.

**Server rejects Apple notifications in production** — the app's Apple ID isn't configured, or the bundle id doesn't match.
