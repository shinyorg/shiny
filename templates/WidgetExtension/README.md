# Live Activity widget extension

iOS renders a Live Activity from a **widget extension**, and WidgetKit requires that extension to be
Swift/SwiftUI — there is no way around it from C#. This folder is that widget, written once so you don't have
to: it renders whatever `LiveActivityContent` your .NET code or your server sends.

**You don't copy these files anywhere.** `Shiny.Mobile.LiveActivities` packs them into
`buildTransitive/swift/`, and its build targets compile them into your app when you set:

```xml
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <ShinyLiveActivityWidget>true</ShinyLiveActivityWidget>
</PropertyGroup>
```

That builds `PlugIns/ShinyLiveActivity.appex` with `swiftc` (no Xcode project), infers and embeds its
provisioning profile on device builds, signs it with the app's identity, and adds `NSSupportsLiveActivities`
to the app's Info.plist. Signing details and every property:
https://shinylib.net/client/liveactivities/widget/

## Files

| File | Purpose |
|---|---|
| `ShinyLiveActivityWidget.swift` | The stock widget: Lock Screen view + Dynamic Island, driven by the content state. Compiled unless the app supplies its own. |
| `ShinyActivityAttributes.swift` | The shared activity type. **Always** compiled into the extension, and byte-identical to `native/ShinyLiveActivities/ShinyLiveActivities/ShinyActivityAttributes.swift` — it's how ActivityKit matches the widget to the activity the app starts. |
| `Info.plist` | Used only by the `ShinyLiveActivityWidgetTemplate` target in `native/ShinyLiveActivities/project.yml`, which compile-checks this Swift in CI. The .NET build writes its own. |

## Your own layout

Copy `ShinyLiveActivityWidget.swift` into your app as a starting point (not `ShinyActivityAttributes.swift`),
restyle it, and point the build at it:

```xml
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <ShinyLiveActivityWidgetSource Include="Platforms/iOS/LiveActivity/*.swift" />
</ItemGroup>
```

Branch on `context.attributes.kind` (from `LiveActivityRequest.Kind`) for per-activity layouts, and read your
own values from `context.state.data` and `context.attributes.values`.

## Changing the contract

If you change `ShinyActivityAttributes.swift`, change the copy in `native/ShinyLiveActivities/ShinyLiveActivities/`
too, and mirror the fields in `LiveActivityContentSchema` on the .NET side — those three, plus the
`content-state` a server pushes, are one contract, and a mismatch shows up as an activity that silently refuses
to update.
