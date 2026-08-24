# Plan: Shiny in Unity

Status: **proposal** — nothing here is committed work.
Last updated: 2026-08-23

## Summary

Unity cannot consume this repo as it stands, and most modules cannot be ported without
reimplementation. But one module — **Shiny.Net.Discovery** — is within days of working, and a spike
proved it. That module is the whole recommendation. Everything else is either a separate product
(BLE), low value (Locations), or an outright no (Jobs).

## The two constraints

**1. Target framework.** Unity's scripting runtime is Mono/IL2CPP at .NET Standard 2.1. A managed
plugin DLL must be `netstandard2.0`/`netstandard2.1`. Every package in `src/` is `net10.0` +
platform TFMs (`Directory.Build.props`, `BaseTargetFramework`). A `net10.0` assembly will not load
in Unity at all — this is not a degraded experience, it is a hard stop.

**2. Interop model.** Unity Android is a Java app driving JNI through `AndroidJavaObject` /
`AndroidJavaClass`. Unity iOS is IL2CPP calling `[DllImport("__Internal")]` into ObjC plugin
source. Neither ships `Mono.Android.dll` or `Xamarin.iOS.dll`, so `Android.App`, `UIKit`,
`CoreBluetooth` and `CoreLocation` do not exist in the compilation. Every platform backend in this
repo binds to exactly those namespaces.

Constraint 2 is why BLE and Locations are reimplementations rather than ports. Constraint 1 is why
even the pure-managed code needs work — but far less than expected.

## Findings from the spike

Compiled `src/Shiny.Net.Discovery` (minus `Platforms/`) against `netstandard2.1` in a scratch
project. Results, in order of discovery:

| Step | Errors |
|---|---|
| Baseline, sources untouched | 233 |
| + polyfill file, + `System.Threading.Channels` package | 18 |
| + complete `Lock` polyfill (with `EnterScope`) | **57 unique sites / 16 files** |

The 18 was misleading — an unresolved `IParsable<T>` was halting binding and masking downstream
errors. 57 unique sites across 16 files is the real number. All of them are in `Managed/`.

Three further facts that shape the plan:

- **Discovery uses nothing from Shiny.Core.** The `ProjectReference` in
  `Shiny.Net.Discovery.csproj` is vestigial — grepping for Core types found two hits, both false
  positives (a comment in `MulticastLockScope.cs` and a `Uri.Host` in `UpnpDescriptionParser.cs`).
  The netstandard target can simply drop the reference. No Core multi-targeting required.
- **`Microsoft.Extensions.*` 10.0.7 still ships `netstandard2.0`** — verified in the local NuGet
  cache for DependencyInjection, Logging and Http. The dependency chain is not a blocker.
- **The managed stack has exactly one `#if ANDROID`** in the entire `Managed/` tree:
  `MulticastLockScope.cs`, which acquires `WifiManager.MulticastLock`. That is the one piece
  needing an `AndroidJavaObject` equivalent in Unity — a handful of lines.

### The 57 sites, categorised

**~95% mechanical.** Downlevel BCL gaps with well-known equivalents:

| Missing | Sites (approx) | Fix |
|---|---|---|
| `Environment.TickCount64` | 11 | helper indirection (can't extend `Environment`) |
| `Random.Shared` | 7 | helper indirection |
| `ArgumentNullException.ThrowIfNull` | 7 | `Guard.NotNull` helper |
| `CancellationTokenSource.CancelAsync` | 5 | extension-method polyfill, no source edit |
| `Enumerable.Order()` | 6 | extension-method polyfill, no source edit |
| `OperatingSystem.IsWindows/IsAndroid/IsIOS/…` | 13 | helper indirection |
| `StringSplitOptions.TrimEntries` | 2 | source edit |
| `System.Numerics.BitOperations` | 1 | define the type in the polyfill, no source edit |
| `char.IsAsciiLetterOrDigit` | 1 | source edit |
| `HttpContent.ReadAsStreamAsync(ct)` | 1 | source edit |

Some of these polyfill in place with zero source edits (extension methods, `BitOperations`). Others
target sealed/static BCL types that cannot be extended (`Environment`, `Random`,
`ArgumentNullException`, `OperatingSystem`) and need a small helper plus mechanical call-site
substitution.

**The one genuinely non-trivial piece: 2 call sites.** `MulticastSocketSet.cs`:

- line ~529 — `Socket.SendToAsync(ReadOnlyMemory<byte>, SocketFlags, EndPoint, CancellationToken)`
- line ~562 — `Socket.ReceiveMessageFromAsync(Memory<byte>, SocketFlags, EndPoint, CancellationToken)`

Neither overload exists on netstandard2.1. Both are cleanly isolated inside private methods
(`SendSafe` and the receive loop), so the surgery is contained to one file — but this is the hot
path of the entire discovery stack, and `ReceiveMessageFromAsync` is used for its
`PacketInformation.Interface`, which downlevel means a `SocketAsyncEventArgs`-based awaitable
wrapper (`ReceiveMessageFromPacketInfo`). Budget ~150–250 lines and real tests.

## Phase 0 — verify two assumptions before committing (half a day)

Both could invalidate or reshape everything below.

1. **Has Unity's CoreCLR migration shipped?** Unity has had modern-.NET/CoreCLR work in flight for
   some time. If it has landed in a version people actually use, the netstandard2.1 work is
   unnecessary and the whole plan simplifies to "reference the package." Check before writing code.
2. **Can Unity's Roslyn consume C# 14 `extension` blocks?** `MdnsExtensions.cs` uses extension
   members (`extension(MdnsService service) { … }`). The DLL compiles fine under netstandard2.1 —
   Unity's compiler consuming those members from a reference assembly is a separate question.
   Test with a trivial extension-block assembly in a real Unity project. If it fails, emit
   classic static extension methods on the netstandard target via `#if`.

## Phase 1 — `netstandard2.1` target for Shiny.Net.Discovery (~3–5 days)

The whole value of this plan sits here.

1. Add `netstandard2.1` to `TargetFrameworks` in `Shiny.Net.Discovery.csproj`; drop the
   `Shiny.Core` `ProjectReference` on that target (it is unused on every target — consider removing
   outright).
2. Add `System.Threading.Channels` `PackageReference` for the netstandard target.
3. Add `Polyfills.cs`, compiled only downlevel — see appendix; validated in the spike.
4. Introduce a small internal `Compat` helper for the un-extendable statics (`TickCount64`,
   `Random.Shared`, `OperatingSystem.*`, `ThrowIfNull`) and mechanically substitute the ~38 call
   sites.
5. Implement the downlevel socket path in `MulticastSocketSet.cs` behind `#if !NET`, using
   `SocketAsyncEventArgs`. **This is the risk item** — schedule it first, not last.
6. Decide the `GetTxt<T> where T : IParsable<T>` question. Simplest honest answer: gate the
   overload to `NET7_0_OR_GREATER` and document that the netstandard target has string-only TXT
   access. A `TypeConverter`-based downlevel overload is the alternative if API parity matters.
7. Run `tests/` against the new target. The existing test project is the acceptance gate — if the
   managed mDNS/SSDP/WSD suites pass on netstandard2.1, the port is real.

**Definition of done for Phase 1:** package builds and packs with the netstandard target; existing
Discovery tests green on it; a plain netstandard2.1 console app can browse and publish.

This phase also pays off for MonoGame DesktopGL and Godot desktop — see `plans/monogame-support.md`.

## Phase 2 — Unity package + proof (~1 week)

1. Consume the netstandard2.1 DLL in a real Unity project (Unity 6 LTS, IL2CPP, Android + iOS
   builds). Verify SSDP and WS-Discovery browse on device — these are the modules that already run
   the managed path on every platform, so device behaviour should match mobile today.
2. Port `MulticastLockScope` to `AndroidJavaObject`. Without the multicast lock, Android receives
   nothing while sends succeed — the failure mode is a silent empty result set, so this is
   mandatory, not optional.
3. iOS: SSDP and WS-Discovery use raw multicast and therefore need Apple's
   `com.apple.developer.networking.multicast` entitlement. Document it prominently. (mDNS on iOS
   avoids this via NSNetService, but Unity gets the managed responder, so Unity mDNS *does* need
   the entitlement where MAUI does not. Call this out — it is the single most surprising difference.)
4. Ship a sample scene that discovers something real.
5. Decide distribution: UPM git package vs. `.unitypackage` vs. asset store. UPM is the low-effort
   default.

## Phase 3 — Shiny.Net.Wifi, Android only (optional, ~2–3 weeks)

`AndroidWifiManager` wraps `android.net.wifi.WifiManager`, a pure Java API — so it is portable to
`AndroidJavaObject` with no native toolchain, just tedium. The iOS side wraps
NEHotspotConfiguration/CaptiveNetwork and needs an ObjC plugin, and iOS has no scanning API at all
(`AppleWifiManager` reports `WifiCapabilities` accordingly), so iOS value is near zero.

Do this only if someone asks. Android-only Wi-Fi is a narrow pitch.

## Phase 4 — BluetoothLE (separate product decision, months)

Not a port. Android needs a Java `.aar` (`BluetoothGattCallback` via `AndroidJavaProxy` is
workable but slow and awkward); iOS needs a CoreBluetooth ObjC plugin. Shiny's Android BLE alone is
~15 files covering L2CAP, MTU, PHY, pairing and reliable transactions.

Unity BLE assets already exist, so the pitch cannot be "BLE in Unity" — it has to be the API:
`ManagedScan`, the Rx surface, the peripheral abstraction. That is defensible, but it is a product
launch, not a repo change. **Gate on demonstrated demand.**

## Explicitly not doing

**Locations.** BLE-shaped effort plus a worse problem: geofencing needs a `BroadcastReceiver` that
fires when the app is not running, and in Unity the C# runtime is not necessarily loaded then. The
Java plugin would do the real work and Unity would be a downstream consumer of results. Foreground
GPS is tractable; background geofencing is an architectural fight for a niche payoff.

**Jobs — no.** The value of Shiny.Jobs is OS background scheduling (WorkManager, BGTaskScheduler).
A Unity game does not run in the background, and when WorkManager fires there is no guarantee the
Unity player — and therefore the `IJob` implementations — is alive to run it. You would be
scheduling work into a runtime that is not there. The part that *would* port,
`Platforms/Net/JobManager`, is a 30-second `System.Timers.Timer` loop over foreground jobs gated on
`IBattery`/`IConnectivity`; in Unity that is a coroutine, and not worth a dependency.

The only framing where Jobs makes sense is the UaaL track below.

## Alternate track — Unity as a Library

Worth a docs page regardless of whether any of the above happens.

UaaL embeds the Unity player into a native Android/iOS host app. The host is a real .NET
Android/iOS process, so **every Shiny module works completely unmodified** — BLE, Locations, Wifi,
Jobs, Push, all of it — with Unity as a rendering surface you message. It is the only path where
the existing code runs as-is.

Right for enterprise/AR/product-viewer apps. Wrong for a game. UaaL has its own sharp edges (single
player instance, teardown behaviour). This is the correct answer for a real slice of people asking
"does Shiny work in Unity," and answering it costs a docs page rather than an engineering quarter.

## Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Socket rewrite is subtler than estimated | Phase 1 slips | Do it first; existing tests are the gate |
| Unity Roslyn rejects C# 14 extension blocks | API surface churn | Phase 0 test; `#if` to classic extensions |
| Multicast entitlement friction on iOS | Support burden | Document loudly in Phase 2 |
| netstandard2.1 target becomes a maintenance tax | Ongoing drag | Keep it to Discovery; do not spread it |
| Unity CoreCLR lands and obsoletes the work | Wasted effort | Phase 0 check |

## Definition of done (when work actually happens)

Per `CLAUDE.md`, shipping any of this requires:

1. `readme.md` updated
2. `skills/` — no Shiny.Net.Discovery skill exists yet; one would need creating, or the Unity
   guidance folded into an existing skill
3. Docs site (`~/Desktop/dev/documentation`) — feature page under the discovery module, plus a
   release note in that module's `release-notes.mdx`

A netstandard2.1 target is a shipped-library change and does warrant a release note. A Unity sample
on its own does not.

## Appendix — validated polyfill

Compiled clean in the spike. Requires `System.Threading.Channels` as a `PackageReference` on the
downlevel target.

```csharp
#if !NET7_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute
    {
        public string FeatureName { get; } = featureName;
        public bool IsOptional { get; init; }
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct |
        AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute {}
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute {}

    [AttributeUsage(
        AttributeTargets.Parameter | AttributeTargets.Field |
        AttributeTargets.Property, AllowMultiple = true)]
    internal sealed class StringSyntaxAttribute(string syntax) : Attribute
    {
        public string Syntax { get; } = syntax;
    }
}
#endif

#if !NET9_0_OR_GREATER
namespace System.Threading
{
    /// <summary>
    /// Degrades System.Threading.Lock to a plain Monitor target downlevel. Roslyn recognises the
    /// type by name and requires the EnterScope pattern, so it must be implemented, not empty.
    /// </summary>
    internal sealed class Lock
    {
        public void Enter() => Monitor.Enter(this);
        public void Exit() => Monitor.Exit(this);
        public Scope EnterScope() { Monitor.Enter(this); return new Scope(this); }

        public ref struct Scope(Lock owner)
        {
            readonly Lock owner = owner;
            public void Dispose() => Monitor.Exit(this.owner);
        }
    }
}
#endif
```
