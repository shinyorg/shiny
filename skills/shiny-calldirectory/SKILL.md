---
name: shiny-calldirectory
description: Call blocking and caller identification on iOS and Android with Shiny.Mobile.CallDirectory - a CallKit Call Directory extension built during dotnet build (no Xcode project) and an Android CallScreeningService under the call screening role, both fed from one SQLite list the app owns.
auto_invoke: true
triggers:
  - call directory
  - call blocking
  - block calls
  - caller ID
  - caller identification
  - spam calls
  - CallKit
  - CXCallDirectoryManager
  - CXCallDirectoryProvider
  - CallScreeningService
  - ShinyCallScreeningService
  - call screening role
  - ROLE_CALL_SCREENING
  - RoleManager
  - caller ID & spam app
  - BlockUnlistedCalls
  - ShowCallerIdNotification
  - DefaultCountryCode
  - Call Blocking & Identification
  - Shiny.Mobile.CallDirectory
  - Shiny.CallDirectory
  - ICallDirectoryManager
  - CallDirectoryManager
  - NoOpCallDirectoryManager
  - CallDirectoryEntry
  - CallDirectoryEntryType
  - CallDirectoryReloadResult
  - CallDirectoryOptions
  - AddCallDirectory
  - PhoneNumbers.ToCallDirectory
  - ShinyCallDirectoryExtension
  - ShinyCallDirectoryAppGroup
  - ShinyCallDirectoryBundleId
  - ShinyCallDirectoryDisplayName
  - ShinyCallDirectoryCodesignProvision
  - ShinyCallDirectoryCodesignEntitlements
  - ShinyCallDirectoryAddAppEntitlement
  - ShinyCallDirectoryMinimumOSVersion
---

# Shiny Call Directory

`Shiny.Mobile.CallDirectory` blocks calls, or shows the app's own caller ID label for them, from a list of phone
numbers the app keeps. One API, two platform mechanisms:

- **iOS** - the CallKit **Call Directory**. iOS reads the list through a Call Directory app extension that the
  package builds during `dotnet build` (no Xcode project, Swift or Info.plist work).
- **Android 10+** - the **call screening role**. `ShinyCallScreeningService` (declared in the package, merged into
  the manifest) checks every incoming call against the list.

## When to Use This Skill

- The user wants to block phone numbers or label incoming calls (spam lists, company directories, "this is your
  courier calling").
- The user mentions CallKit's Call Directory, `CXCallDirectoryManager`, or Settings > Phone > Call Blocking &
  Identification.

It is **not** VoIP calling (CallKit `CXProvider`) and not call screening of live audio.

## Platform support - read this first

| Platform | Implementation |
|----------|----------------|
| iOS 15+ | `CallDirectoryManager` - blocking + identification through the bundled extension |
| Android 10+ (API 29) | `CallDirectoryManager` - blocking, allow list, optional block-unlisted, caller ID notification, through `ShinyCallScreeningService` while the app holds the call screening role |
| Android 8-9 | `CallDirectoryManager` with `IsSupported` false (no call screening role) |
| Everything else (Mac Catalyst, macOS, Windows, Linux, Blazor) | `NoOpCallDirectoryManager` - `IsSupported` is false, every call is a safe no-op |

- Never generate `#if IOS` around calls - branch on `IsSupported` only to hide the UI.
- On the **iOS simulator** `Reload` returns `Failed`. Blocking and caller ID can only be verified on a device; the
  store methods (`Set`, `Remove`, `Get`, `Count`) work on the simulator.

## Packages and namespaces

| Item | Value |
|------|-------|
| NuGet | `Shiny.Mobile.CallDirectory` |
| Namespace | `Shiny.CallDirectory` (types); `Shiny` (`AddCallDirectory`) |
| Depends on | `Shiny.Core` - works in any Shiny host (MAUI `UseShiny()` or `Shiny.Hosting.Native`) |

The package name carries `Mobile` (like `Shiny.Mobile.LiveActivities`); the namespace does not. Generate
`using Shiny.CallDirectory;`, never `using Shiny.Mobile.CallDirectory;`.

## iOS project file (required)

```xml
<PropertyGroup>
    <ShinyCallDirectoryExtension>true</ShinyCallDirectoryExtension>
    <ShinyCallDirectoryAppGroup>group.com.mycompany.myapp</ShinyCallDirectoryAppGroup>
</PropertyGroup>
```

`ShinyCallDirectoryAppGroup` is required - the build errors without it. The build then:
- compiles the extension into `PlugIns/ShinyCallDirectory.appex`
- adds the app group entitlement to the app and the extension
- stamps the group into the extension's Info.plist
- signs the extension on device builds

| Property | Default | Purpose |
|----------|---------|---------|
| `ShinyCallDirectoryExtension` | false | Build the extension (iOS app projects only) |
| `ShinyCallDirectoryAppGroup` | required | App group shared by the app and the extension |
| `ShinyCallDirectoryBundleId` | `$(ApplicationId).CallDirectory` | Extension bundle id |
| `ShinyCallDirectoryDisplayName` | `$(ApplicationTitle)` | Name in Settings > Apps > Phone > Call Blocking & Identification |
| `ShinyCallDirectoryCodesignProvision` | best match | Extension provisioning profile (name or UUID) |
| `ShinyCallDirectoryCodesignEntitlements` | generated | Custom extension entitlements (must include the app group) |
| `ShinyCallDirectoryAddAppEntitlement` | true | `false` when the app group is already in the app's Entitlements.plist |
| `ShinyCallDirectoryMinimumOSVersion` | `SupportedOSPlatformVersion` (min 15.0) | Extension deployment target |

**Device builds need two provisioning profiles**, both with the App Groups capability and the group: one for the
app and one for the extension's bundle id. The extension is only built on macOS. Off a Mac the build warns,
`IsSupported` is false and `Reload` returns `ExtensionNotFound`.

## Android setup

Nothing in the project file: the service ships in the package. Add `POST_NOTIFICATIONS` (Android 13+) only if
caller ID notifications should show. The role request's result comes back through Shiny's activity result
forwarding - automatic with Shiny.Hosting.Maui; with Shiny.Hosting.Native the activity must derive from
`ShinyAndroidActivity`.

## Registration

```csharp
using Shiny;

builder.Services.AddCallDirectory();
builder.Services.AddCallDirectory(o =>
{
    // iOS - rarely needed, both default to what the build stamped into the extension
    o.AppGroup = "group.com.mycompany.myapp";
    o.ExtensionBundleId = "com.mycompany.myapp.CallDirectory";

    // Android
    o.DefaultCountryCode = 1;            // for incoming numbers in national format
    o.BlockUnlistedCalls = false;        // true = only listed Allow/Identify numbers ring (withheld numbers blocked too)
    o.ShowCallerIdNotification = true;   // Identify entries post a notification with the label while ringing
});
```

## Access

```csharp
var access = await directory.GetCurrentAccess();   // Available | Disabled | Unknown | NotSupported
if (access != AccessState.Available)
    access = await directory.RequestAccess();
```

- **iOS:** no prompt. The user enables the extension in Settings > Apps > Phone > Call Blocking & Identification.
  `RequestAccess` opens Settings (best effort) and returns the *current* state without waiting - re-check
  `GetCurrentAccess()` when the app resumes. Until enabled, `Reload` returns `ExtensionDisabled`.
- **Android:** `RequestAccess` shows the system "default caller ID & spam app" dialog for the call screening role
  and returns `Available` or `Denied`. Android stops showing it after repeated refusals (the user can still choose
  the app in Settings > Apps > Default apps). Only one app holds the role; until it does, `Reload` returns
  `ExtensionDisabled` and calls are not screened.

## Managing the list

Numbers are `long`: country code + digits, no `+` or formatting (`18005550100`). Always convert user input with
`PhoneNumbers.ToCallDirectory(text, defaultCountryCode = 1)`. It throws `FormatException` for input with no digits
or more than 15 digits.

```csharp
using Shiny.CallDirectory;

public class BlockListService(ICallDirectoryManager directory)
{
    public async Task SyncFromServer(IEnumerable<ServerNumber> numbers)
    {
        // whole list from a server: one transaction, next load is a full replace
        await directory.ReplaceAll(numbers.Select(n => new CallDirectoryEntry(
            n.Number,
            n.Label,
            n.Spam ? CallDirectoryEntryType.Block : CallDirectoryEntryType.Identify
        )));
        await this.Apply();
    }

    public async Task Block(string typed)
    {
        await directory.Set([new(PhoneNumbers.ToCallDirectory(typed), null, CallDirectoryEntryType.Block)]);
        await this.Apply();
    }

    async Task Apply()
    {
        var result = await directory.Reload();
        // Success | NotSupported | ExtensionDisabled | ExtensionNotFound | CurrentlyLoading
        // | MaximumEntriesExceeded | LoadingInterrupted | Failed
    }
}
```

| Member | Behaviour |
|--------|-----------|
| `Set(entries)` | Add or replace; unchanged entries are not re-sent to iOS |
| `Remove(numbers)` | Remove; unknown numbers are ignored |
| `ReplaceAll(entries)` | Replace everything in one transaction; forces a full load |
| `Clear()` | Remove everything |
| `Get(number)` / `Count()` | Read the stored list |
| `Reload()` | iOS: load the extension and apply the list - **edits are invisible to iOS until this**. Android: edits apply immediately; only reports whether screening is active |

`CallDirectoryEntryType`:
- `Block` blocks the number (Android: rejected silently, still in the call log).
- `Identify` shows `Label` as the caller ID (Android: a notification while it rings - apps cannot change the
  dialer's caller ID; no label = it just rings).
- `Allow` always rings on Android, even with `BlockUnlistedCalls`. iOS has no allow list, so it is loaded as
  `Identify`.

## Code generation rules

1. Batch edits and call `Reload()` **once** after them. Each reload launches the extension, and `CurrentlyLoading`
   means one is already running - retry later rather than in a tight loop.
2. Use `ReplaceAll` for a server sync, `Set`/`Remove` for user edits.
3. Normalize every user-typed number with `PhoneNumbers.ToCallDirectory`. Never store formatted strings or `+`.
4. Handle `ExtensionDisabled` by calling `RequestAccess()` (iOS: Settings; Android: role dialog), not by retrying.
5. Never generate a Call Directory extension project, Swift code or Info.plist keys - the build makes them.
6. Inject `ICallDirectoryManager`. `CallDirectoryManager.DatabasePath` (platform class) exists for diagnostics only.
7. Always call `Reload()` after edits even though Android doesn't need it - the same code must work on iOS.
8. Never declare `ShinyCallScreeningService` or the call screening intent filter in the app's manifest - the
   package does.

## How it works (for debugging)

**Android:** `ShinyCallScreeningService.OnScreenCall` normalizes the caller (`DefaultCountryCode`), looks it up in
`shiny_calldirectory.db` (app files dir, no change log) and responds within Android's 5 second limit. Outgoing
calls are never touched; any exception allows the call.

**iOS:**

- **Storage.** The list is `shiny_calldirectory.db` in the app group container. The app is the only writer; the
  extension only reads.
- **SQLite.** Both sides use the system SQLite (no SQLitePCLRaw), so the package is trim/AOT safe.
- **Change tracking.**
  - Every `Set`/`Remove` is logged with the number's previous type, so a reload sends only changes since the last
    load iOS accepted.
  - `ReplaceAll`/`Clear` bump a generation, which forces a full load.
  - The extension records what iOS accepted in the app group's `UserDefaults`.
  - The app trims the change log on the next `Reload`.
- **No open connections.** Each store call opens, works in one transaction and closes. A suspended app holding a
  lock on a shared-container file is killed by iOS (`0xdead10cc`).
- **Diagnosing failures.** A failed reload logs the CallKit error and the extension's own last error, so check the
  app's logs when `Reload` returns `Failed`.
