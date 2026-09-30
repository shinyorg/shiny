# Plan: AppFunctions result views (snippets, entity display, in-app templates)

Status: **proposed** (2026-09-30). Nothing built. Phase 0 is the gate.

## Summary

`Shiny.AppFunctions` v1 returns a value plus dialog text (`ctx.Say`). Assistants can show more than text,
but the two platforms differ a lot:

- **Siri / App Intents can show a custom SwiftUI view** under the reply (`ShowsSnippetView`, iOS 16). From
  iOS 26 the view can be **interactive** (`SnippetIntent`): its buttons run other intents and the snippet
  redraws in place.
- **Gemini / Android AppFunctions has no view surface.** `ExecuteAppFunctionResponse` carries a
  `GenericDocument` (and extras). Gemini decides how to present it. All the app controls is how well the
  data is described.
- **The in-app AI chat** (`Shiny.AppFunctions.Extensions.AI`) is drawn by the app, so it can show any MAUI view.

So "custom views" is really four features. They share one idea: **the result record is the view model**.
Whatever draws the result (SwiftUI in Siri, Gemini's own renderer, a MAUI chat bubble) binds to the same
C# record that the handler returns today.

| # | Option | Platforms | Cost | Recommendation |
|---|---|---|---|---|
| A | Your own SwiftUI snippet view | iOS 16+ | Small | **Build first** |
| B | Richer entity display (subtitle, image) | iOS, Android (data only) | Small | **Build first** |
| C | Result templates in the in-app chat | MAUI (any platform) | Medium, crosses into the `controls` repo | **Build second** |
| D | Declarative snippet templates in C# | iOS view, Android field hints | Medium to large | Only if A proves too awkward |
| E | Interactive snippets (buttons run other functions) | iOS 26+ | Medium | After A (or D) |
| — | Render a MAUI view to a PNG and show it | iOS | — | **Rejected**, see [Rejected](#rejected) |

## SDK verification

From the Xcode 27.0 (27A266a) iPhoneOS 27.0 SDK and the `Microsoft.Android.Ref.36` 36.1.69 ref pack on this
machine, not from documentation.

| Claim | Evidence | Result |
|---|---|---|
| A result can carry a SwiftUI view | `_AppIntents_SwiftUI.swiftinterface:84-103`: `result(value:view:)`, `result(value:opensIntent:dialog:view:)`, … | ✅ It's in the `_AppIntents_SwiftUI` overlay, so the generated file must `import SwiftUI` |
| `ShowsSnippetView` works on our iOS minimum | `AppIntents.swiftinterface:4487`, `@available(iOS 16.0)` | ✅ Matches `ShinyAppFunctionsMinimumOSVersion` (16.0) |
| Interactive snippets | `AppIntents.swiftinterface:3663`: `protocol SnippetIntent`, `static func reload()`, `ShowsSnippetIntent`, `result(value:dialog:snippetIntent:)`, `requestConfirmation(... snippetIntent:)` | ✅ `@available(anyAppleOS 26.0)` |
| Buttons and toggles that run intents | `_AppIntents_SwiftUI.swiftinterface:186,194,237`: `Button(intent:)`, `Toggle(isOn:intent:)` | ✅ iOS 17 |
| Entity display can have a subtitle and image | `AppIntents.swiftinterface:3851`: `DisplayRepresentation(title:subtitle:image:)`. `Image(named:)`, `Image(systemName:)`, `Image(data:)`, `Image(url:)` | ✅ iOS 16 (`displayStyle: .circular` needs iOS 17) |
| Android can return a view | `Mono.Android.dll`, `Android.App.AppFunctions`: only `AppFunctionService`, `AppFunctionManager`, `ExecuteAppFunctionRequest/Response`, `AppFunctionException` | ❌ There's no view type. The response is data only |

**Not yet verified (Phase 0):** what a snippet view can render (AsyncImage, maximum height, dark mode), whether
`Button(intent:)` works inside a *non-`SnippetIntent`* snippet on iOS 17 to 25, and whether a remote `https`
URL works for `DisplayRepresentation.Image(url:)` or only a file URL.

## Constraints that shape every option

1. **The view has to be SwiftUI compiled into the app binary.** MAUI and UIKit views can't appear in a
   snippet, and C# can't create SwiftUI at runtime. So option A needs Swift source that the build compiles,
   and option D needs the generator to *write* that Swift.
2. **Snippet views don't have to be const-extractable.** Only intent metadata does (see the `SwiftEmitter`
   header comment). A view is ordinary Swift that runs in the app process, so writing one is not limited by
   `appintentsmetadataprocessor`.
3. **The result arrives in Swift as JSON.** `ShinyAFReply.value` is the parsed `"value"` of
   `{"value": …, "dialog": …}`. Today object results come back to Shortcuts as JSON text
   (`ResultMapping` → `ReturnsValue<String>`). A view needs a **typed Swift struct**, which the generator can
   emit from the `TypeModel` it already builds.
4. **Apple-only features use separate attributes that Android ignores**, as `[AppShortcut]` does. `[AppFunction]`
   stays cross-platform.
5. **Time budget.** Rendering happens inside the intent's `perform()`, after the C# handler has returned.
   Nothing here adds a round trip to C#, except E's reload, which is another function call.

---

## Option A: your own SwiftUI snippet view

### Usage

```csharp
[AppFunction("create_order", Description = "Creates an order for a customer")]
[AppSnippet("OrderSnippet")]                      // Apple only
public record CreateOrder(CustomerEntity Customer, int Quantity) : IAppFunction<OrderResult>;

public record OrderResult(string Number, double Total, DateTimeOffset Eta, OrderStatus Status);
```

```swift
// Platforms/iOS/Snippets/OrderSnippet.swift (picked up by default, see Build)
import SwiftUI

struct OrderSnippet: View {
    let result: OrderResultValue            // generated from the C# record
    var body: some View {
        VStack(alignment: .leading) {
            Text("Order \(result.number)").font(.headline)
            Text(result.total, format: .currency(code: "USD"))
            Text(result.eta, style: .relative)
        }
        .padding()
    }
}
```

The generated `perform()` changes from

```swift
func perform() async throws -> some IntentResult & ReturnsValue<String> & ProvidesDialog {
    …
    return .result(value: reply.text, dialog: reply.dialog(orDefault: reply.text))
```

to

```swift
func perform() async throws -> some IntentResult & ReturnsValue<String> & ProvidesDialog & ShowsSnippetView {
    …
    let model = try reply.decode(OrderResultValue.self)
    return .result(value: reply.text, dialog: reply.dialog(orDefault: reply.text), view: OrderSnippet(result: model))
```

The Shortcuts return value doesn't change, so existing Shortcuts keep working.

### Generator

- **`[AppSnippet(string viewName)]`**: Apple-only, on the request record. `FunctionModel` gains `SnippetView`.
- **Result structs.** For each object type reachable from a result that has a snippet, emit
  `struct {SimpleName}Value: Decodable` with `CodingKeys` = wire names. Dates decode with `ShinyAF.parseDate`
  through a custom `init(from:)`, because the wire format is ISO 8601 text.
  - Generated `AppEnum`s gain `Decodable` (they're already `String`-backed, so this is free).
  - Arrays → `[T]`. Nullable → optional.
  - **Check:** how the C# result writer serializes an `[AppEntity]` inside a result (whole object or just the
    id). The Swift type has to match. If it's the whole object, emit an `…Value` struct for it too, not the
    `AppEntity`.
- **Runtime:** add `ShinyAFReply.decode<T: Decodable>(_:)` to `swift/ShinyAppFunctions.swift`. It does
  `JSONSerialization.data(withJSONObject: value)` then `JSONDecoder`. A decode failure throws
  `ShinyAFError`, so the user sees an error dialog rather than a blank snippet. The alternative is to fall back
  to text only (open question 1).
- **Diagnostics:**
  - `SHAF014`: `[AppSnippet]` on a function with no result (`IAppFunction`) → warning. It's allowed with
    `.result(dialog:view:)`, but the view would have no data. Either allow it or make it an error (open question 2).
  - `SHAF015`: the view name isn't a valid Swift identifier → error.
  - A missing or mistyped Swift view is **swiftc's** error, surfaced through the `Exec` in
    `ShinyAppFunctionsCompileSwift`. The generator can't see `.swift` files unless we add them as
    `AdditionalFiles`, and that isn't worth doing for v1.

### Build (`Shiny.AppFunctions.targets`)

- New item `ShinyAppFunctionsSwift`, defaulting to `Platforms/iOS/**/*.swift` when the project has any
  `[AppSnippet]`. The item is always honored, so apps can put Swift files anywhere.
- Add it to the `swiftc` inputs, the target's `Inputs`, and `SwiftFileList` (the metadata processor reads that
  list).
- `Frameworks="AppIntents Foundation"` on the `NativeReference` becomes `AppIntents Foundation SwiftUI`.
- **Check:** does adding SwiftUI to the static lib change the const-extraction protocol list, or add link time
  or binary size worth documenting? Measure in Phase 0.

### Android

Nothing to render. `[AppSnippet]` is ignored, as `[AppShortcut]` is.

---

## Option B: richer entity display

### Usage

```csharp
[AppEntity("customer", Title = "Customer",
    DisplayProperty = nameof(Name),
    SubtitleProperty = nameof(City),          // new
    ImageProperty = nameof(AvatarUrl),        // new: string URL or asset name
    SystemImage = "person.crop.circle")]      // new: SF Symbol fallback, Apple only
public record CustomerEntity(string Id, string Name, string City, string? AvatarUrl);
```

### Changes

- **Attribute + model:** `SubtitleProperty`, `ImageProperty`, `SystemImage` on `AppEntityAttribute` /
  `EntityModel`. Diagnostic `SHAF008` (display property not found) is extended to the new properties.
- **Wire:** `AppEntityItem(Id, Title)` → `AppEntityItem(Id, Title, Subtitle?, Image?)`. The same change in
  the generated `QueryEntities` and in Swift's `ShinyAFEntityItem`.
- **Swift:** the generated `AppEntity` stores `displaySubtitle` / `displayImage` and builds
  `DisplayRepresentation(title:subtitle:image:)`. Image handling:
  - `http(s)` URL → `Image(url:)` if Phase 0 shows remote URLs load. Otherwise the C# side downloads to the
    caches directory and passes a file URL. Decide after the spike.
  - Anything else → `Image(named:)` (an asset in the app bundle).
  - null → `Image(systemName: SystemImage)` when set.
- **Android:** the `search_{entity}` functions already return the entity record. Add the subtitle and image
  fields' roles to their descriptions in `app_functions_v2.xml`, so Gemini knows which field is which.
  **Check** whether the Jetpack XSD in `buildTransitive/app_functions_schema.xsd` allows a description on an
  object property. If not, this is iOS only.

This is independent of A and also improves pickers, disambiguation and Shortcuts, not just snippets.

---

## Option C: result templates in the in-app chat

The in-app chat is drawn by the app, so there are no OS limits. The problem is only plumbing:
`AppFunctionAIFunction.InvokeCoreAsync` returns a `JsonObject` for the model, and nothing typed reaches the UI.

### Changes in this repo (`Shiny.AppFunctions` + `.Extensions.AI`)

- **Typed result read-back.** The generator emits a per-function `ReadResult(string json) → object?` on the
  descriptor, using the same reflection-free reader as the request binding. It's trim and AOT safe.
- **Hand the typed result to the UI:**
  - The `FunctionResultContent` still goes to the model as JSON, unchanged.
  - `AppFunctionAIFunction` also raises an event, `IAppFunctionAIToolBuilder.OnResult(...)` or an
    `IAppFunctionResultObserver` resolved from DI, with `(functionId, typed result, dialog)`.
  - Alternatively, it attaches the typed result to `FunctionResultContent.AdditionalProperties` so a chat
    UI that already walks the message list can find it. **Preferred:** it needs no extra wiring and survives
    chat history replay (open question 3).

### Changes in the `controls` repo (`ShinyChatView`)

- A template selector for tool results, keyed by result CLR type:
  `chat.ResultTemplates.Add<OrderResult>(new DataTemplate(typeof(OrderResultCard)))`. A type with no
  template falls back to today's text bubble.
- Blazor controls: the same thing with `RenderFragment<T>`.

**Cross-repo:** ship the `Extensions.AI` side first, since it's usable without the controls change through the
event. The controls change follows in the next controls release.

---

## Option D: declarative snippet templates in C#

Only build this if A turns out to be a burden: people who don't want to write Swift, or a lot of near-identical
views.

### Usage

```csharp
[AppFunction("create_order")]
[AppSnippet(Template = SnippetTemplate.Card)]           // instead of a view name
public record CreateOrder(...) : IAppFunction<OrderResult>;

public record OrderResult(
    [property: SnippetTitle] string Number,
    [property: SnippetField("Total", Format = "C")] double Total,
    [property: SnippetField("Arrives", Format = "relative")] DateTimeOffset Eta,
    [property: SnippetBadge] OrderStatus Status,
    [property: SnippetImage] string? PhotoUrl,
    [property: SnippetProgress] double? Progress);        // 0..1
```

- **The generator writes the SwiftUI view.** A small fixed set of layouts (`Card`, `List`, `Stat`), filled
  from the marked properties. Output is covered by snapshot tests, like the rest of the Swift.
- **Android:** the same markers become descriptions on the result properties in `app_functions_v2.xml`
  (depends on the same XSD check as B). Gemini gets labeled fields and still draws them itself.
- **Chat (C):** a default MAUI card template built from the same markers, so a result has a reasonable bubble
  without writing a `DataTemplate`.

This works like Adaptive Cards: portable and limited. A and D can coexist: a view name wins over a template.

---

## Option E: interactive snippets (iOS 26+)

### Usage

```csharp
[AppFunction("get_order")]
[AppSnippet("OrderSnippet", Interactive = true)]
public record GetOrder(string Number) : IAppFunction<OrderResult>;

[AppFunction("cancel_order")]
public record CancelOrder(string Number) : IAppFunction;
```

```swift
struct OrderSnippet: View {
    let result: OrderResultValue
    var body: some View {
        …
        Button("Cancel", intent: CancelOrderIntent(number: result.number))   // generated intent
    }
}
```

### How it maps

- `Interactive = true` generates a `GetOrderSnippetIntent: SnippetIntent`. It keeps the **request** arguments
  as its `@Parameter`s. Its `perform()` runs the function again through `ShinyAF.invoke` and returns
  `.result(view: OrderSnippet(result:))`.
- The function's own intent returns `.result(value:dialog:snippetIntent: GetOrderSnippetIntent(...))` on
  iOS 26, and falls back to A's static `view:` below 26 (`if #available`).
- After a button's intent completes, the system reloads the snippet (`SnippetIntent.reload()`), which runs the
  function again and shows fresh data. **This means the function must be safe to run repeatedly (a read)**, so
  `Interactive` only makes sense on get/query functions.
  - `SHAF016`: warning when `Interactive` is on a function with no result.
  - We can't detect side effects, so the documentation has to say this clearly.
- The buttons' target intents are the ordinary generated `…Intent` types, so apps call them from their own
  Swift (A). With D, add `[SnippetAction("cancel_order", Bind = nameof(Number))]` to a result property. The
  generator emits the `Button(intent:)` and **checks that the binding fills every required parameter of the
  target** (`SHAF017`).
- **Confirmation:** `requestConfirmation(... snippetIntent:)` can later back an
  `AppFunctionGate.Confirm(...)` in `IAppFunctionDelegate`. That's out of scope here; note it for a follow-up.

**Check in Phase 0:** do the snippet intent's `@Parameter`s need to be const-extractable or discoverable?
Setting `isDiscoverable = false` keeps them out of Shortcuts.

---

## Rejected

**Rendering a MAUI view to a PNG and showing it in the snippet as `Image(data:)`.** It would reuse XAML, but:

- It runs during background launches, when there's no window and the time budget is tight. It would render
  off-screen with MAUI handlers during a cold start.
- It's a fixed bitmap, so it ignores dark mode, Dynamic Type and VoiceOver, and it's blurry at other scales.
- It isn't interactive, so it blocks E.

A's Swift escape hatch plus D's templates cover the same need without these problems.

## Build order

### Phase 0: spike (gate)

Hand-edit the generated Swift in the sample (`samples/Sample.Maui`, `OrderFunctions`). No generator changes.

- A: `ShowsSnippetView` with a hand-written view and a hand-written `Decodable`. It shows in Siri **and** in
  the Shortcuts run result, including from a cold start.
  - Record the height limit, whether `AsyncImage` loads, and dark mode behavior.
  - Record the binary size change from linking SwiftUI.
- B: entity with `subtitle` and `image`. Try a remote `https` URL, a file URL and `Image(named:)`.
- E (iOS 26 simulator): a `SnippetIntent` with a `Button(intent:)` that calls a second C# function, and a
  reload that shows the changed data.
- Also try `Button(intent:)` inside a plain `ShowsSnippetView` on iOS 17 to 25. If it works, E could have a
  lower iOS floor.
- Android: confirm whether the XSD allows property descriptions (for B and D).

**Go/no-go:** if the static snippet doesn't render from a cold start, stop, because nothing else is worth
building.

### Phase 1: A + B (generator, runtime, targets)

- `[AppSnippet]`, the result `…Value` structs, `ShinyAFReply.decode`, the `ShinyAppFunctionsSwift` item,
  SwiftUI linking, and `SHAF014`/`SHAF015`.
- Entity subtitle and image end to end on both platforms (Android as descriptions, if the XSD allows them).
- Snapshot tests for the new Swift and for the `perform()` variants (no result, value + view, dialog + view).
- Sample: an order snippet, and customers with avatars.
- **Done when:** the sample's order snippet and customer picker images show on the simulator, and Android is
  unchanged except for the XML descriptions.

### Phase 2: C (in-app chat)

- `ReadResult` on descriptors, the typed result on `FunctionResultContent.AdditionalProperties` and/or the
  observer.
- Tests in `AIToolTests` that the typed result round-trips for every value kind.
- Then the `controls` repo: `ShinyChatView` result templates, MAUI and Blazor.
- **Done when:** the sample's AI assistant page shows an `OrderResultCard` for `create_order`.

### Phase 3: E (interactive, iOS 26)

- `Interactive`, the generated `SnippetIntent`, the `#available` fallback, and `SHAF016`.
- **Done when:** in the sample, "cancel" from the order snippet runs `cancel_order` in C# and the snippet
  reloads with the cancelled status.

### Phase 4: D (only if needed)

- Decide after Phases 1 to 3 have been used in a real app.

### With each phase (CLAUDE.md "Required updates")

- `readme.md`, `skills/shiny-appfunctions` (SKILL.md + `reference/api-reference.md`, and new trigger keywords:
  snippet, `AppSnippet`, SnippetIntent, entity image), the `client/appfunctions` docs page, and a release
  note under `#### App Functions` in `client/release-notes.mdx`.

## Open questions

1. **A snippet decode failure:** throw an error dialog, or fall back to text only? Leaning toward fallback
   plus a log, because a result that arrived but can't be drawn is still a successful call.
2. **`[AppSnippet]` on a function with no result:** allow it as a static "done" view, or make it an error?
3. **Chat plumbing:** `AdditionalProperties` only, the observer only, or both? It depends on whether
   `ShinyChatView` sees `FunctionResultContent` at all today.
4. **Default location for the Swift files:** `Platforms/iOS/**/*.swift` vs a dedicated `AppFunctions/Swift/`
   folder. MAUI single-project already ignores `.swift` files in `Platforms/iOS`, so the first shouldn't
   conflict. Confirm.
