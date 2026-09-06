# Live Activity widget extension template

iOS renders a Live Activity from a **widget extension**, and WidgetKit requires that extension to be
Swift/SwiftUI — there is no way around it from C#. This folder is the extension, written once so you
don't have to: it renders whatever `LiveActivityContent` your .NET code or your server sends, so most
apps add it unchanged and never open Xcode again.

The Swift here is compile-checked in CI (the `ShinyLiveActivityWidgetTemplate` target in
`native/ShinyLiveActivities/project.yml`).

## Files

| File | Purpose |
|---|---|
| `ShinyLiveActivityWidget.swift` | The widget: Lock Screen view + Dynamic Island, driven by the content state. |
| `ShinyActivityAttributes.swift` | The shared activity type. **Must be byte-identical to the one the library uses** — it's how ActivityKit matches your widget to the activity. |
| `Info.plist` | Marks the target as a WidgetKit extension. |

## Adding it to a .NET MAUI / .NET for iOS app

1. **Create the extension in Xcode.** New project → iOS → Widget Extension. Name it e.g.
   `MyAppLiveActivity`, tick *Include Live Activity*, and set its bundle id to
   `<your-app-bundle-id>.MyAppLiveActivity`. Its deployment target must be **iOS 16.2 or later**.

2. **Replace the generated Swift** with `ShinyLiveActivityWidget.swift` and `ShinyActivityAttributes.swift`
   from this folder. Delete the template's own attributes/bundle files so there is exactly one `@main`.

3. **Wire it into the .NET build.** In your app's `.csproj`:

   ```xml
   <ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
     <XcodeProject Include="../ios/MyAppLiveActivity/MyAppLiveActivity.xcodeproj">
       <SchemeName>MyAppLiveActivity</SchemeName>
       <Kind>AppExtension</Kind>
     </XcodeProject>
   </ItemGroup>
   ```

   The .NET iOS SDK builds the Xcode project and embeds the resulting `.appex` in `PlugIns/`.

4. **Declare Live Activity support** in the app's `Info.plist`:

   ```xml
   <key>NSSupportsLiveActivities</key>
   <true/>
   <!-- only if you update more than a handful of times an hour -->
   <key>NSSupportsLiveActivitiesFrequentUpdates</key>
   <true/>
   ```

5. **Start one** from C#:

   ```csharp
   await manager.Start(new LiveActivityRequest
   {
       Content = new LiveActivityContent { Title = "Order placed", ShortStatus = "12 min" }
   });
   ```

## Customizing

Everything below the top of `ShinyLiveActivityWidget.swift` is ordinary SwiftUI — restyle it freely. To
render different layouts per activity type, branch on `context.attributes.kind` (set from
`LiveActivityRequest.Kind` in C#) and read your own values out of `context.state.data`.

If you change `ShinyActivityAttributes.swift`, change the copy in
`native/ShinyLiveActivities/ShinyLiveActivities/` too, and mirror the fields in
`LiveActivityContentSchema` on the .NET side — those three are one contract, and a mismatch shows up as
an activity that silently refuses to update.
