---
name: shiny-agesignals
description: Generate code using Shiny.AgeSignals to read the user's age range as the app store shares it - Apple Declared Age Range (iOS / macOS 26+) and Google Play Age Signals (Android) - for app store accountability laws (Texas SB 2420, Utah, Louisiana, California AB 1043) and age-gated features
auto_invoke: true
triggers:
  - Shiny.AgeSignals
  - IAgeSignals
  - AgeSignal
  - AgeSignalStatus
  - AgeRangeSource
  - AgeSignalsEligibility
  - AgeSignalsException
  - SignificantChangeStatus
  - AddAgeSignals
  - RequestAgeRange
  - ShowSignificantUpdate
  - age verification
  - age gate
  - age range
  - age signals
  - Declared Age Range
  - DeclaredAgeRange
  - AgeRangeService
  - Play Age Signals
  - AgeSignalsManager
  - Texas SB 2420
  - SB 2420
  - App Store Accountability Act
  - AB 1043
  - parental consent
  - significant change
---

# Shiny Age Signals Skill

You are an expert in Shiny.AgeSignals, a .NET library that reads the user's age range as the app store shares it,
through one `IAgeSignals` over Apple Declared Age Range and Google Play Age Signals.

## When to Use This Skill

Invoke this skill when the user wants to:
- Comply with an app store accountability law (Texas SB 2420 and the Utah / Louisiana / California equivalents)
- Gate content or features by age band ("is this user at least 16?")
- Show Apple's significant-update acknowledgment, or read Play's guardian approval of significant changes

## Library Overview

**Documentation**: https://shinylib.net/client/agesignals/
**Package**: `Shiny.AgeSignals`
**Namespaces**: `Shiny.AgeSignals` (types), `Shiny` (registration)
**Platforms**: iOS 26+ and macOS 26+ (Declared Age Range through a bundled Swift shim), Android (Play Age Signals 0.0.4,
bound inside the package). iOS / macOS before 26, **Mac Catalyst**, Windows, Linux and Blazor report `NotSupported`.

## Registration

```csharp
using Shiny;

builder.Services.AddAgeSignals();
```

### Required setup

- **Apple: the `com.apple.developer.declared-age-range` entitlement (boolean `true`), with the Declared Age Range
  capability enabled on the App ID.** Without it Apple's call **never returns** - the request hangs instead of failing.
  Always generate the entitlement alongside any Apple code:

  ```xml
  <!-- csproj -->
  <ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
      <CustomEntitlements Include="com.apple.developer.declared-age-range" Type="Boolean" Value="true" Visible="false" />
  </ItemGroup>
  ```
- **Android:** the app must be installed from Google Play (otherwise `AgeSignalsErrorReason.AppNotOwned`). Custom
  minimum ages (up to three, at least two years apart) are set in **Play Console**, not at runtime.

## API

```csharp
namespace Shiny.AgeSignals;

public interface IAgeSignals
{
    Task<AgeSignalsEligibility> GetEligibility(CancellationToken cancellationToken = default);   // no UI
    Task<AgeSignal> RequestAgeRange(int gate1, int? gate2 = null, int? gate3 = null, CancellationToken cancellationToken = default);
    Task<bool> ShowSignificantUpdate(string updateDescription, CancellationToken cancellationToken = default); // iOS 26.4+, else false
}

public record AgeSignalsEligibility(
    bool IsSupported,
    bool? IsEligible = null,                                  // Apple 26.2+ isEligibleForAgeFeatures; null on Android
    bool? AgeRangeRequired = null,                            // Apple 26.4+ regulatory flags; null on Android
    bool? SignificantChangeRequiresParentalConsent = null,
    bool? SignificantChangeRequiresAdultNotification = null
);

public enum AgeSignalStatus { NotSupported, Declined, VerificationRequired, Shared }
public enum AgeRangeSource { Unknown, SelfDeclared, GuardianDeclared, Confirmed, IdVerified }   // weakest first
public enum SignificantChangeStatus { Approved, Pending, Declined }                           // Google Play

public record AgeSignal(
    AgeSignalStatus Status,
    int? LowerBound = null,                 // inclusive; Apple leaves the lowest band's lower bound null
    int? UpperBound = null,                 // inclusive; null for the top band (e.g. 18+)
    AgeRangeSource Source = AgeRangeSource.Unknown,
    bool CommunicationLimits = false,       // Apple
    SignificantChangeStatus? SignificantChange = null,           // Google Play
    DateTimeOffset? SignificantChangeApprovedThrough = null,     // Google Play
    string? InstallId = null                                     // Google Play, supervised installs
)
{
    public bool? IsAtLeast(int age);   // null = the store can't tell
    public bool? IsAdult { get; }      // IsAtLeast(18)
}

public class AgeSignalsException : Exception
{
    public AgeSignalsErrorReason Reason { get; }   // NotAvailable, InvalidAccount, Network, Transient, StoreUnavailable, AppNotOwned, NoUserInterface, Unknown
    public int? NativeCode { get; }                // Play AgeSignalsErrorCode
}
```

| | Apple Declared Age Range | Google Play Age Signals |
|---|---|---|
| UI | `RequestAgeRange` may present a sheet | Play's prompt only outside the US law states; there users verify in the Play Store |
| Age gates | the `gate1..3` arguments | ignored - bands are 0-12 / 13-15 / 16-17 / 18+ or the Play Console custom ages |
| `Source` | `selfDeclared` → SelfDeclared, `guardianDeclared` → GuardianDeclared, `confirmed` (and the 26.2 `*Checked` cases) → Confirmed | TIER_A → SelfDeclared, TIER_B → GuardianDeclared, TIER_C → Confirmed, TIER_D → IdVerified |
| `VerificationRequired` | never | user is in a mandatory jurisdiction with no verified age - send them to the Play Store |
| Significant changes | `ShowSignificantUpdate` (iOS 26.4+) + the eligibility flags | declare in Play Console, read `SignificantChange` / `SignificantChangeApprovedThrough` |
| Declined onboarding | `Status = Declined` | - |

## Code Generation Instructions

1. Add `using Shiny.AgeSignals;` where `IAgeSignals` is injected and `using Shiny;` where `AddAgeSignals()` is called.
2. **Always emit the Apple entitlement** (see Required setup) when targeting iOS or macOS - a missing entitlement hangs
   forever, it does not throw.
3. Gates are positive and ascending, at most three: `RequestAgeRange(13, 16, 18)`. Never pass an array.
4. Decide with `IsAtLeast(age)` / `IsAdult` and treat **`null` as "unknown"**, never as `false` or `true` - fall back to
   the app's most protective experience. Don't compute from `LowerBound` alone.
5. Switch on `Status` before reading bounds. Handle `VerificationRequired` (tell the user to open the Play Store to
   verify) separately from `Declined`.
6. When assurance matters, compare `Source`: `signal.Source >= AgeRangeSource.Confirmed`. Self-declared is the weakest.
7. Never cache an `AgeSignal` across launches - consent can be revoked and birthdays cross bands. Ask at first launch,
   before a gated feature, and after an update.
8. Catch `AgeSignalsException` and branch on `Reason` (`Network` / `Transient` → retry later; `StoreUnavailable` /
   `AppNotOwned` → treat as unknown age). `ArgumentOutOfRangeException` means bad gates.
9. Send `InstallId` to your server for supervised Android installs - Play uses it to notify you of revoked approvals.
10. `GetEligibility()` shows no UI and is safe to call at startup; `RequestAgeRange` may present UI, so call it from a
    user-visible screen with a window / activity available (otherwise `NoUserInterface`).

```csharp
public class AgeGate(IAgeSignals ageSignals)
{
    public async Task<bool> CanUseChat()
    {
        try
        {
            var signal = await ageSignals.RequestAgeRange(13, 16, 18);
            return signal.Status switch
            {
                AgeSignalStatus.NotSupported => true,                      // no store age API - app's own policy applies
                AgeSignalStatus.Shared => signal.IsAtLeast(16) == true,    // null (unknown) => no
                _ => false                                                 // Declined / VerificationRequired
            };
        }
        catch (AgeSignalsException)
        {
            return false;   // unknown age - most protective experience
        }
    }
}
```
