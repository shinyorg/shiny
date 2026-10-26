# Plan: Age signals (Texas SB 2420 and similar app store laws)

Status: **implemented, uncommitted (2026-10-26)** - `Shiny.AgeSignals` for iOS, macOS and Android, with no Mac Catalyst
target (decided). The gates are `(int gate1, int? gate2, int? gate3)`, and the Android side ships against Play 0.0.4 now.
Verified: 71 unit tests; the device-test suite on the iOS 27.0 simulator (eligibility round-trips through the Swift shim -
`IsEligible = False`, all three regulatory flags parsed) and on a Play Store emulator (Pixel 9a, API 37, no account:
`requestAgeSignalsAccess` reaches Play's AgeSignalsService and returns NOT_SHARED -> `Declined`); full `Build.slnf` with
zero IL warnings; the nupkg carries the AAR and the iOS/macOS framework sidecars. **Not exercised:** the request sheet and the
significant-update sheet (headless simulator), a SHARED Play answer (needs a Play account in a covered region), and a macOS
runtime run (build only). AppDeviceBridge: not done.
Last updated: 2026-10-26

## Changes from the proposal (verified against the real SDKs)

- **Play 0.0.4 is not the API in the table below.** `userStatus` is gone. The flow is `requestAgeSignalsAccess(activity)`,
  which returns SHARED / NOT_SHARED / VERIFICATION_REQUIRED, then `checkAgeSignals()` returns `ageLower` / `ageUpper`,
  `ageRangeSource` (TIER_A-D), `significantChangeStatus` + `significantChangeApprovalDate`, and `installId`. So `AgeSignal`
  has `Source` (an ordered `AgeRangeSource`, with `IdVerified` for TIER_D) and `SignificantChange` in place of
  `Verification` / `IsSupervised` / `Approval` / `MostRecentApproval`, and `AgeSignalStatus.VerificationRequired` is new.
  `NotApplicable` was dropped - eligibility is its own call.
- Apple's significant-update sheet has a UIKit overload, `showSignificantUpdateAcknowledgment(in: UIWindowScene, ...)`, so
  no SwiftUI hosting is needed. Apple's declaration crosses the shim as the Swift case name, so the 26.2 `*Checked` cases
  and 26.5's `confirmed` map without `#available` switches.
- **Missing `com.apple.developer.declared-age-range` entitlement = the call never returns** (the Family extension logs
  "has required entitlement : false" and never replies). Documented everywhere; the device-test app and the sample's
  simulator builds carry it.
- Android binds the AAR with `AndroidMavenLibrary Repository="Google" VerifyDependencies="false"` (the verifier crashes on
  central package management, XAJDV7004); `Transforms/Metadata.xml` trims it to the 7 types used and makes them internal.


## Summary

Add `Shiny.AgeSignals`, one interface over the two store age APIs:

- **Apple Declared Age Range**: `AgeRangeService` (iOS / macOS 26+).
- **Google Play Age Signals**: `com.google.android.play:age-signals`.

Texas SB 2420 is in force: the Fifth Circuit stayed the injunctions on 2026-06-04 and SCOTUS declined to vacate the stay
on 2026-07-06. Utah, Louisiana and California (AB 1043, from 2027) have passed laws of the same shape. Under these laws
the **stores** verify age and consent, and the **app** must read the signal the store gives it and act on it.

Why it belongs in Shiny rather than in each app:

- **Neither native API is reachable from .NET today.** DeclaredAgeRange is Swift-only: the SDK has only a
  `.swiftinterface`, no headers, and nothing for it in `Microsoft.iOS.Ref` 26.0 or 27.1 (checked 2026-10-26). The Play
  AAR has no NuGet binding.
- **The two models differ.** Apple takes up to three app-chosen age gates and shows a sheet. Google takes no input and
  returns fixed bands plus a supervision/approval status. Apps want one answer: is this user at least N, and does a
  parent need to approve?
- **Shiny already does this pattern.** `native/ShinyStoreKit` is a `@_cdecl` Swift shim with JSON callbacks (trim/AOT
  safe), and `Shiny.GameCenter` calls unbound Play APIs through JNI. A second small shim is cheap.

## Native surface (verified against the Xcode SDK's swiftinterface, 2026-10-26)

| | Apple `AgeRangeService.shared` | Google `AgeSignalsManager` |
|---|---|---|
| Request | `requestAgeRange(ageGates: a, b?, c?, in: UIViewController)` (26.0); may show a sheet | `checkAgeSignals(AgeSignalsRequest)` → `Task<AgeSignalsResult>`; no UI |
| Result | `.declinedSharing` / `.sharing(AgeRange)` | `userStatus`, `ageLower`, `ageUpper`, `mostRecentApprovalDate`, `installId` |
| Range | `lowerBound?`, `upperBound?` bounded by the app's gates | fixed bands (0-12, 13-15, 16-17, 18+), **to verify** |
| How it was established | `AgeRangeDeclaration`: `selfDeclared`, `guardianDeclared`, `confirmed` (26.5; the 26.2 `*Checked*` cases are deprecated into it) | `VERIFIED`, `SUPERVISED`, `SUPERVISED_APPROVAL_PENDING`, `SUPERVISED_APPROVAL_DENIED`, `UNKNOWN` |
| Parental controls | `ParentalControls.communicationLimits` | implied by `SUPERVISED*` |
| Applies to this user? | `isEligibleForAgeFeatures` (26.2) | errors / `UNKNOWN` outside covered regions, **to verify** |
| Regulatory flags | `requiredRegulatoryFeatures` (26.4): `declaredAgeRangeRequired`, `significantAppChangeRequiresParentalConsent`, `significantAppChangeRequiresAdultNotification` | none in-app; significant changes are declared in Play Console |
| Significant update UI | `showSignificantUpdateAcknowledgment(updateDescription:)`, SwiftUI environment action, iOS 26.4, not macOS | none |
| Errors | `notAvailable`, `invalidRequest`, `invalidAccount`, `declinedOnboarding`, `network` | `AgeSignalsException` error codes |
| Test rig | sandbox account / Settings developer overrides, **to verify** | `FakeAgeSignalsManager` ships in the AAR |

## API

```csharp
namespace Shiny.AgeSignals;

public interface IAgeSignals
{
    /// <summary>
    /// What the store says applies to this user/account, with no UI. On platforms with no store age API
    /// (Windows, Linux, plain .NET, Blazor) IsSupported is false and everything else is false.
    /// </summary>
    Task<AgeSignalsEligibility> GetEligibility(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the store for the user's age range. Apple may present a sheet; Android never shows UI.
    /// ageGates (1-3 ascending ages, e.g. 13, 16, 18) are passed to Apple as-is; on Android the fixed
    /// Play bands are returned and IsAtLeast is evaluated against them.
    /// </summary>
    Task<AgeSignal> RequestAgeRange(int[] ageGates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows the platform's significant-update acknowledgment (iOS 26.4+). Returns false where the platform
    /// has none (Android: declare it in Play Console instead; macOS; non-store platforms).
    /// </summary>
    Task<bool> ShowSignificantUpdate(string updateDescription, CancellationToken cancellationToken = default);
}

public record AgeSignalsEligibility(
    bool IsSupported,                                   // platform has a store age API
    bool IsEligible,                                    // Apple isEligibleForAgeFeatures / Play covered region
    bool AgeRangeRequired,                              // Apple declaredAgeRangeRequired
    bool SignificantChangeRequiresParentalConsent,
    bool SignificantChangeRequiresAdultNotification
);

public enum AgeSignalStatus
{
    NotSupported,   // no store age API on this platform / OS version
    NotApplicable,  // the store says the user is not covered (not eligible / not in a regulated region)
    Declined,       // Apple declinedSharing
    Shared
}

public enum AgeVerification { Unknown, SelfDeclared, GuardianDeclared, Confirmed }   // Play VERIFIED → Confirmed
public enum ParentalApproval { None, Approved, Pending, Denied }                     // Play SUPERVISED* states

public record AgeSignal(
    AgeSignalStatus Status,
    int? LowerBound,
    int? UpperBound,
    AgeVerification Verification,
    bool IsSupervised,
    ParentalApproval Approval,
    bool CommunicationLimits,
    DateTimeOffset? MostRecentApproval,  // Android only
    string? InstallId                    // Android only: the supervised install id for server-side revocation
)
{
    /// True only when the store positively says the user is at least `age`. Null when it can't tell
    /// (not shared, or the range straddles the age).
    public bool? IsAtLeast(int age) => ...;
    public bool? IsAdult => IsAtLeast(18);
}
```

Registration: `services.AddAgeSignals()`, registered with `AddSingletonAsImplementedInterfaces`. On iOS the presenting
view controller comes from the platform's current top controller, the same way `Shiny.AppStore` anchors the review prompt.

### Decisions baked in

- **No caching or persistence.** The answer can change: consent can be revoked, or a birthday crosses a band. Apps
  should ask at the moments the law cares about (first launch, before a gated feature, after an update). We document
  this rather than hide it.
- **`IsAtLeast` returns `bool?`**, so "unknown" can't be mistaken for "no".
- **No throwing for "not covered" or "declined".** Those are statuses. Only `invalidRequest` (bad gates) throws, as
  `ArgumentException`. Network and account errors throw `AgeSignalsException` with a mapped reason.
- **Separate package, not part of `Shiny.AppStore`.** The xcframework and AAR would weigh down every AppStore consumer
  who only wants update checks.

## Implementation

- `src/Shiny.AgeSignals`: `net10.0` (stub, `NotSupported`), `-ios`, `-maccatalyst` (**verify the Catalyst SDK
  exposes the framework**), `-macos`, `-android`. No Windows/Linux TFM; the base TFM covers them.
- **Apple**: `native/ShinyAgeRange/` is a `@_cdecl` Swift shim built by `build.sh` into an xcframework, copied from
  `native/ShinyStoreKit`, with the same `NativeReference` + `CreateBindingResourcePackage` packaging. It exposes three
  calls (eligibility, request, significant update) returning JSON through `[UnmanagedCallersOnly]` callbacks. It wraps
  the 26.2/26.4/26.5 members in `if #available` and weak-links the framework so iOS < 26 returns `NotSupported`.
- **Android**: `AndroidMavenLibrary com.google.android.play:age-signals` (0.0.4 is the latest on Maven, still 0.x)
  with `Bind="false"` and JNI calls, like `PlayGamesJni.cs`. Or bind it if the AAR is small and binds cleanly; try that
  first.
- **Mapping logic in shared code** (`Infrastructure/AgeSignalMapper.cs`): JSON → `AgeSignal` and `IsAtLeast`, unit
  tested on `net10.0` in `tests/Shiny.AgeSignals.Tests` like `GameCenterManager`.
- **Server, optional phase 2**: Apple sends an App Store Server Notification when a parent revokes consent, and Google
  revokes through the `installId`. `Shiny.Mobile.InAppPurchases.Server` already verifies Apple notifications, so
  surfacing the consent-revoked type there is the natural home. **Verify the notification type names before building.**

## Required follow-ups (per CLAUDE.md)

- readme, `skills/shiny-agesignals/` (new), docs `client/agesignals/` + sidebar + `libraryCatalog.ts`, release note
  under a new `#### Age Signals` heading.
- AppDeviceBridge: **yes**. No web standard exists and a hosted web app in a WebView has the same legal obligation.
- Device tests: the Android `FakeAgeSignalsManager` can be swapped in; Apple needs a sandbox account, so expect a skip
  on the headless simulator.

## Open questions

1. Is the Play AAR 0.x stable enough to ship against, or do we gate the Android TFM until 1.0?
2. Mac Catalyst availability of DeclaredAgeRange.
3. Should `RequestAgeRange` take `params int[]`, or a fixed `(int gate1, int? gate2 = null, int? gate3 = null)` that
   mirrors Apple's three-gate limit and makes the limit a compile-time check? I lean toward the latter.
