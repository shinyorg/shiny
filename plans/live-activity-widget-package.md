# Plan: ship the Live Activity widget extension as a NuGet package

Status: **proposed** — nothing built. Supersedes the manual Xcode steps in
`templates/WidgetExtension/README.md`.
Last updated: 2026-09-07

## Summary

The question that started this: *can the iOS Live Activity widget be a .NET for iOS library the user
references, instead of Xcode/Swift work they do by hand?*

**The rendering half cannot be C#** — see [Why the widget stays Swift](#why-the-widget-stays-swift).
That is a hard Apple constraint, not a gap in the .NET iOS SDK.

**The packaging half can.** Today a consumer has to create an Xcode widget extension by hand, copy
two Swift files into it, set a bundle id, and wire it into their `.csproj` — four manual steps, none
of which are app-specific. All of it can move into a `Shiny.Mobile.LiveActivities.Widget` package
whose targets generate and embed the extension at build time, so the consumer's experience becomes
*add a package reference*. Apps that want custom SwiftUI eject with one property.

There is also an [incidental finding](#incidental-finding-the-documented-setup-looks-wrong): the
`<XcodeProject Kind="AppExtension">` step the template README documents is **not a thing the .NET iOS
SDK supports**. If that is confirmed, the currently documented setup does not work, which raises this
from a nice-to-have to a fix.

---

## Why the widget stays Swift

Four independent blockers. Any one is fatal; all four hold.

| Claim | Evidence | Result |
|---|---|---|
| WidgetKit's entry point has no ObjC surface | `@main struct X: WidgetBundle { var body: some Widget }` — a Swift protocol with an associated type and an opaque return, plus a result builder | ✅ not expressible in `ApiDefinition.cs`; bindings only reach the ObjC runtime |
| `ActivityConfiguration` is generic over `ActivityAttributes` | `ActivityConfiguration(for: ShinyActivityAttributes.self)` | ✅ Swift generics over a protocol with associated types — no ObjC projection |
| .NET app extensions need a principal class | `Xamarin.Shared.Sdk.targets:56` — an appex project is `OutputType=Library` + `IsAppExtension`, loaded via `NSExtensionPrincipalClass` | ✅ WidgetKit extensions declare no principal class; the system loads the Swift `@main` symbol |
| A widget extension cannot host the .NET runtime | Widget extensions are launched out-of-process, on the system's schedule, under a hard memory cap (~30 MB) | ✅ starting Mono/CoreCLR to draw a lock-screen row is jetsam bait |

This is not "not yet". Apple would have to add an Objective-C widget entry point for any of it to
change. The library's split — C# owns the *state*, SwiftUI owns the *rendering* — is already the
right shape (`ILiveActivityManager.cs:1-20`); this plan only removes the manual labour around it.

## Incidental finding: the documented setup looks wrong

`templates/WidgetExtension/README.md` step 3 tells consumers to write:

```xml
<XcodeProject Include="../ios/MyAppLiveActivity/MyAppLiveActivity.xcodeproj">
  <SchemeName>MyAppLiveActivity</SchemeName>
  <Kind>AppExtension</Kind>
</XcodeProject>
```

The SDK does not support that. `Microsoft.MaciOS.Sdk.Xcode.targets` archives the scheme, runs
`CreateXcFramework` over the archives, and emits a **`NativeReference`**:

- `Microsoft.MaciOS.Sdk.Xcode.targets:25-31` — the `XcodeProject` item definition; `Kind` defaults to
  `Framework`.
- `Microsoft.MaciOS.Sdk.Xcode.targets:103-135` — `CreateXcFramework` then
  `<NativeReference … Kind="%(Kind)" />`. `Kind` is *NativeReference* kind (`Framework`/`Static`/
  `Dynamic`), not an extension kind. `AppExtension` is not a value the SDK understands.

Building an app-extension scheme through that path produces an `.xcarchive` that
`xcodebuild -create-xcframework` should reject. **Verify in Phase 0 before writing any code** — if it
somehow works today, this plan gets simpler; if it doesn't, the README is shipping instructions that
cannot have been run end to end.

## The mechanism that does work

An appex is embedded through a **project reference marked `IsAppExtension`**:

- `Xamarin.Shared.targets:2700-2706` — `_SeparateAppExtensionReferences` pulls every
  `<ProjectReference IsAppExtension="true" />` out of the normal reference list.
- `Xamarin.Shared.targets:2683-2698` / `_ResolveAppExtensionReferences` — it then calls
  **`BuildAndGetAppExtensionBundlePath`** on each and collects the returned bundle path.
- `Xamarin.Shared.Sdk.targets:286` — `_CopyAppExtensionsToBundle` puts the result in `PlugIns/`.

The referenced project is only required to answer `BuildAndGetAppExtensionBundlePath` with a path to
an `.appex`. **It does not have to be a C# extension project.** That is the seam: ship an MSBuild
project whose implementation of that target runs `xcodebuild` over the bundled Swift and returns the
built `.appex`, and the SDK's existing embed + sign pipeline takes it from there.

## Design

### Package: `Shiny.Mobile.LiveActivities.Widget`

| Content | Role |
|---|---|
| `widget/ShinyLiveActivityWidget.swift`, `widget/ShinyActivityAttributes.swift`, `widget/Info.plist` | today's `templates/WidgetExtension/`, unchanged |
| `widget/ShinyLiveActivityWidget.xcodeproj` | pre-generated by `xcodegen` **at our build time** from `native/ShinyLiveActivities/project.yml`, so consumers never need xcodegen |
| `widget/ShinyLiveActivityWidget.proj` | the shim MSBuild project: implements `BuildAndGetAppExtensionBundlePath` over `xcodebuild` |
| `buildTransitive/Shiny.Mobile.LiveActivities.Widget.targets` | injects the `ProjectReference`, writes the generated `.xcconfig`, patches the app `Info.plist` |

Packaging convention follows `Shiny.Core` (`Shiny.Core.csproj:16` — `Pack=True PackagePath=buildTransitive`).

### Build flow

1. Targets condition on `GetTargetPlatformIdentifier(...) == 'ios'` and on `IsAppExtension != true`
   (so the extension project doesn't reference itself).
2. Write `$(IntermediateOutputPath)ShinyLiveActivityWidget.xcconfig` from the consumer's own
   properties:

   ```
   PRODUCT_BUNDLE_IDENTIFIER = $(ApplicationId).ShinyLiveActivity
   MARKETING_VERSION         = $(ApplicationDisplayVersion)
   CURRENT_PROJECT_VERSION   = $(ApplicationVersion)
   IPHONEOS_DEPLOYMENT_TARGET = 16.2
   DEVELOPMENT_TEAM          = $(_DevelopmentTeam)
   ```

   The `.xcodeproj` reads it, so one checked-in Xcode project serves every consumer. **The generated
   file is an input to the appex build** — it must land in `FileWrites` and be part of the incremental
   inputs, or a bundle-id change won't rebuild.
3. Inject `<ProjectReference Include="…/ShinyLiveActivityWidget.proj" IsAppExtension="true" />`.
4. Add `NSSupportsLiveActivities` to the app's `Info.plist` if absent (as an MSBuild plist merge, not
   by editing the user's file on disk).

### Escape hatch

`<ShinyLiveActivityWidget>None</ShinyLiveActivityWidget>` suppresses step 3 entirely, and a
`dotnet new shinyliveactivitywidget` template drops the same Swift into the consumer's repo for them
to own. Ejecting must be a copy, never a fork of the package's project file — the bundle-id/xcconfig
plumbing should stay identical either way.

### What the package cannot hide

- **Xcode on the build machine.** The `.appex` can't be prebuilt into the nupkg: its bundle id must be
  a child of the consumer's app id and it must be signed with their identity. (Rewriting
  `CFBundleIdentifier` in a prebuilt appex and re-signing is *technically* possible, but it forecloses
  SwiftUI customization — the one thing people actually want to change. Rejected.) An iOS build needs
  a Mac regardless.
- **A second App ID and provisioning profile** for the extension. Automatic signing handles it;
  manual signing means one more profile to manage.

---

## Phase 0 — spike

Half a day, throwaway MAUI app + the existing template. **Everything else is contingent on this.**

1. Does `<XcodeProject Kind="AppExtension">` work at all? (Expected: no — see
   [above](#incidental-finding-the-documented-setup-looks-wrong).) Record the exact failure.
2. Does a hand-written `.proj` answering `BuildAndGetAppExtensionBundlePath` with an `xcodebuild`-built
   `.appex` get embedded in `PlugIns/`? Assert on the built `.app` layout.
3. **Is the embedded appex codesigned by the SDK, or does it arrive unsigned?** This is the highest-risk
   unknown — `_CopyAppExtensionsToBundle` copies a bundle that a *.NET* extension project would already
   have signed. If the SDK does not sign it, the shim project must, and it needs the identity and
   entitlements the SDK computed.
4. Does device install + a real Live Activity render, on both simulator and device?
5. Does an `.xcconfig`-driven `PRODUCT_BUNDLE_IDENTIFIER` survive the archive, and does changing
   `$(ApplicationId)` correctly force a rebuild?

Outcome: confirm the mechanism, or fall back to `Exec xcodebuild` + manual copy into `PlugIns/` +
explicit `codesign` (uglier, fully under our control).

## Phase 1 — the package

- New `src/Shiny.Mobile.LiveActivities.Widget/` (targets/props + packed Swift; no compiled assembly).
- Move `templates/WidgetExtension/*` into it; keep the `ShinyLiveActivityWidgetTemplate` compile-check
  target in `native/ShinyLiveActivities/project.yml` pointed at the new location so the Swift still
  can't rot.
- Generate the `.xcodeproj` in CI (`xcodegen generate`) and **commit it** — consumers must not need
  xcodegen.
- `Shiny.Mobile.LiveActivities` does **not** depend on this package. Server-push-only apps and Android
  apps must not pull an Xcode build into their loop.

## Phase 2 — docs, skill, template

Per `CLAUDE.md`, none of this is done until:

- `readme.md` — the iOS setup section becomes "add the package".
- `skills/shiny-liveactivities/` — setup steps replaced; add the package name and the
  `ShinyLiveActivityWidget` / eject property to the trigger keywords.
- Docs site `src/content/docs/liveactivities/*.mdx` — rewrite the widget setup page; keep the manual
  Xcode path as a documented "I want my own widget" section rather than deleting it.
- `release-notes.mdx` — a **feature** note for the package, and a **fix** note if Phase 0 confirms the
  old `Kind=AppExtension` instructions never worked.

## Phase 3 — later, only if wanted

Multiple widgets per app (`LiveActivityRequest.Kind` already selects a layout, `Models.cs:117`)
and the shelved interactivity work. Note the interaction: Phase 3 of
[`live-activity-interactivity.md`](./live-activity-interactivity.md) concluded the widget must **link**
`ShinyLiveActivities.framework` rather than copy Swift. A package that owns the `.xcodeproj` is
exactly where that link gets configured once — this plan makes that plan cheaper, and neither blocks
the other.

---

## Decisions taken

| Decision | Choice | Why |
|---|---|---|
| Widget in C# | **No** | Four independent Apple constraints; see the table above |
| Prebuilt `.appex` in the nupkg | **No** | Bundle id + signing are consumer-specific, and it would kill SwiftUI customization |
| Separate package | **Yes** | Android-only and push-only consumers must not inherit an Xcode build |
| Ship a committed `.xcodeproj` | **Yes** | Requiring `xcodegen` on consumer machines defeats the point |

## Open decisions

1. **Does the SDK sign the embedded appex?** Phase 0 item 3. Shapes whether the shim project needs
   signing logic at all.
2. **Extension bundle id suffix.** `.ShinyLiveActivity` is descriptive but permanent — changing it
   later orphans a provisioning profile. Overridable via a property either way.
3. **Windows/Linux CI for consumers.** The targets must no-op cleanly (with a clear warning, not a
   hard error) when the iOS TFM is built on a non-Mac, matching what the SDK already does
   (`Microsoft.MaciOS.Sdk.Xcode.targets:34` — every Xcode target is gated on `IsOSPlatform('osx')`).
