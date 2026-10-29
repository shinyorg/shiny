---
name: shiny-biometrics
description: Generate code using Shiny.Biometrics to verify the device owner with Touch ID, Face ID and Optic ID (iOS, Mac Catalyst, macOS), Android BiometricPrompt (fingerprint, face, iris) and Windows Hello, with an optional passcode / PIN fallback
auto_invoke: true
triggers:
  - Shiny.Biometrics
  - IBiometrics
  - BiometricRequest
  - BiometricResult
  - BiometricResultStatus
  - BiometricAvailability
  - BiometricStatus
  - BiometricType
  - BiometricStrength
  - AddBiometrics
  - biometrics
  - biometric authentication
  - biometric login
  - Touch ID
  - TouchID
  - Face ID
  - FaceID
  - Optic ID
  - fingerprint
  - face unlock
  - LocalAuthentication
  - LAContext
  - BiometricPrompt
  - BiometricManager
  - Windows Hello
  - UserConsentVerifier
  - NSFaceIDUsageDescription
---

# Shiny Biometrics Skill

You are an expert in Shiny.Biometrics, a .NET library that verifies the device owner with the platform's own biometric
prompt through one `IBiometrics`.

## When to Use This Skill

Invoke this skill when the user wants to:
- Unlock an app, a screen or a sensitive action with Touch ID / Face ID / fingerprint / Windows Hello
- Check whether biometrics are available, enrolled or locked out before offering "Sign in with Face ID"
- Fall back to the device passcode / PIN when biometrics fail

## Library Overview

**Documentation**: https://shinylib.net/client/biometrics/
**Package**: `Shiny.Biometrics`
**Namespaces**: `Shiny.Biometrics` (types), `Shiny` (registration)
**Platforms**: iOS, Mac Catalyst and macOS (LocalAuthentication - Touch ID, Face ID, Optic ID; a paired Apple Watch on
macOS with the passcode policy), Android (AndroidX BiometricPrompt), Windows (Windows Hello / `UserConsentVerifier`).
Linux, Blazor WebAssembly and plain .NET report `NotSupported`.

## Registration

```csharp
using Shiny;

builder.Services.AddBiometrics();
```

### Required setup

- **iOS: `NSFaceIDUsageDescription` in Info.plist.** Face ID devices terminate an app that prompts without it, so
  `Authenticate` throws `InvalidOperationException` first. Always emit it alongside iOS code:

  ```xml
  <key>NSFaceIDUsageDescription</key>
  <string>Unlock your account with Face ID</string>
  ```
- **Android:** nothing to declare - `USE_BIOMETRIC` / `USE_FINGERPRINT` merge in from the AndroidX manifest. The prompt
  is shown from the current activity, which must be a `FragmentActivity` (`MauiAppCompatActivity` /
  `AppCompatActivity` are).
- **Windows:** nothing to declare. The prompt is parented to the process main window.

## API

```csharp
namespace Shiny.Biometrics;

public interface IBiometrics
{
    // no UI
    Task<BiometricAvailability> GetAvailability(BiometricStrength strength = BiometricStrength.Strong, CancellationToken cancellationToken = default);

    // shows the platform prompt; failures come back as a status, they are not thrown
    Task<BiometricResult> Authenticate(BiometricRequest request, CancellationToken cancellationToken = default);
}

public enum BiometricStrength { Strong, Weak }                                     // Android class 3 / class 2; ignored elsewhere
[Flags] public enum BiometricType { None = 0, Fingerprint = 1, Face = 2, Iris = 4, Unknown = 8 }
public enum BiometricStatus { Available, NotSupported, NotEnrolled, LockedOut, PasscodeNotSet, Unavailable, SecurityUpdateRequired }

public record BiometricAvailability(BiometricStatus Status, BiometricType Types = BiometricType.None, bool IsDeviceCredentialSet = false)
{
    public bool IsAvailable { get; }   // Status == Available
}

public class BiometricRequest
{
    public required string Reason { get; init; }       // shown on every platform; must not be empty
    public string? Title { get; init; }                // Android title (defaults to Reason)
    public string? Subtitle { get; init; }             // Android
    public string? CancelText { get; init; }           // Apple + Android
    public string? FallbackText { get; init; }         // Apple: null = "Enter Password", "" = hidden
    public bool AllowDeviceCredential { get; init; }   // passcode / PIN / pattern fallback
    public BiometricStrength Strength { get; init; } = BiometricStrength.Strong;   // Android
    public bool ConfirmationRequired { get; init; } = true;                        // Android, passive (face) biometrics
}

public enum BiometricResultStatus { Success, Cancelled, Fallback, Failed, LockedOut, NotEnrolled, PasscodeNotSet, NotAvailable, Timeout }

public record BiometricResult(BiometricResultStatus Status, string? ErrorMessage = null)
{
    public bool IsSuccess { get; }
}
```

| | Apple | Android | Windows Hello |
|---|---|---|---|
| `Types` | exactly one of Fingerprint (Touch ID) / Face (Face ID) / Iris (Optic ID); `Unknown` for a newer kind | every sensor the device declares | `Unknown` |
| `AllowDeviceCredential` | `deviceOwnerAuthentication` (passcode; Apple Watch on macOS) | `DEVICE_CREDENTIAL` (no cancel button is shown) | PIN is **always** offered |
| `Fallback` result | user tapped `FallbackText` (biometrics-only) | never | never |
| `Strength` | ignored (all strong) | `Strong` → `BIOMETRIC_STRONG`; with a credential on API 28-29 it is relaxed to weak (Android rejects that combination) | ignored |
| Title / Subtitle | system | yes | system |

## Code Generation Instructions

1. Add `using Shiny.Biometrics;` where `IBiometrics` is injected and `using Shiny;` where `AddBiometrics()` is called.
2. **Always emit `NSFaceIDUsageDescription`** when the app targets iOS.
3. Call `GetAvailability()` before showing biometric UI (it has no UI): hide the option for `NotSupported`, offer
   "Set up Face ID / fingerprint in Settings" for `NotEnrolled`, and explain `LockedOut` (the passcode unlocks it).
   Use `Types` to pick the label/icon ("Face ID" vs "Touch ID" vs "Fingerprint"); on Windows say "Windows Hello".
4. Always set `Reason` - an empty reason throws `ArgumentException`.
5. Switch on `result.Status`, don't just test `IsSuccess`: `Cancelled` is the user's choice (do nothing), `Fallback`
   means show your own login, `LockedOut` / `NotEnrolled` / `PasscodeNotSet` need a message.
6. Never claim biometric-only on Windows - Hello accepts its PIN and does not report which was used.
7. The result is a **local yes/no** - use it to gate UI or unlock locally stored data. Never send "biometric passed" to
   a server as proof of identity; a server needs its own credential (token, passkey).
8. Pass a `CancellationToken` to dismiss the prompt programmatically (e.g. when the app backgrounds); cancelling throws
   `OperationCanceledException`.
9. Call `Authenticate` from a visible screen: Android throws `InvalidOperationException` when there is no current
   `FragmentActivity`.

```csharp
public class UnlockViewModel(IBiometrics biometrics)
{
    public async Task<bool> Unlock()
    {
        var availability = await biometrics.GetAvailability();
        if (!availability.IsAvailable && !availability.IsDeviceCredentialSet)
            return false;   // nothing to verify with - fall back to the app's own login

        var result = await biometrics.Authenticate(new BiometricRequest
        {
            Reason = "Unlock your vault",
            Title = "Unlock",                 // Android
            AllowDeviceCredential = true      // passcode / PIN if biometrics fail or aren't enrolled
        });

        return result.Status switch
        {
            BiometricResultStatus.Success => true,
            BiometricResultStatus.Cancelled => false,
            _ => false   // LockedOut / NotEnrolled / PasscodeNotSet / ... - show result.ErrorMessage
        };
    }
}
```
