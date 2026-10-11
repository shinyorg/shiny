---
name: shiny-appstore
description: Generate code using Shiny.AppStore to check the app's store for a newer published version, deep-link to the store and review pages, and request the native in-app review prompt - Apple App Store and Mac App Store, Google Play, Microsoft Store, and Linux Flatpak/Snap, implemented natively without MAUI
auto_invoke: true
triggers:
  - Shiny.AppStore
  - IAppStore
  - AppStoreOptions
  - AppStoreResult
  - AddAppStore
  - AddLinuxAppStore
  - app update check
  - check for updates
  - new version available
  - force update
  - store version
  - open app store
  - rate this app
  - review prompt
  - in-app review
  - RequestReview
  - SKStoreReviewController
  - ReviewManager
  - itunes lookup
  - flatpak
  - snap store
---

# Shiny App Store Skill

You are an expert in Shiny.AppStore, a .NET library that asks the store an app ships through whether a newer
version is published, deep-links to the app's store and review pages, and requests the platform's native
in-app review prompt. Every platform is implemented natively - no MAUI or Essentials - so it works in MAUI,
native, Linux desktop and console apps.

## When to Use This Skill

Invoke this skill when the user wants to:
- Check whether a newer version of their app is in the store (update prompts, forced upgrades)
- Open the app's page in the App Store, Google Play, the Microsoft Store or a Linux software centre
- Ask the user to rate or review the app

## Library Overview

**Documentation**: https://shinylib.net/client/appstore/
**Package**: `Shiny.AppStore`
**Namespaces**: `Shiny.AppStore` (types), `Shiny` (registration)
**Platforms**: iOS, Mac Catalyst, macOS (AppKit), Android, Windows, Linux (Flatpak / Snap). Plain .NET elsewhere registers a store that finds nothing and opens nothing.

It replaces `IAppStore` from `Shiny.Extensions.MauiHosting` / `Shiny.Extensions.MauiHosting.Linux`, which no
longer have it. Never generate `builder.AddAppStore(...)` or `builder.AddLinuxAppStore(...)` on
`MauiAppBuilder` - register on `IServiceCollection`.

## Registration

```csharp
using Shiny;

// everything except the Windows product ID is detected from the running app
builder.Services.AddAppStore();

// or with identifiers (named arguments)
builder.Services.AddAppStore(appleAppId: "1234567890", windowsProductId: "9NBLGGH4NNS1");

// or the full options
builder.Services.AddAppStore(opts =>
{
    opts.AppleAppId = "1234567890";          // numeric App Store ID - needed for deep links before GetCurrent has run
    opts.WindowsProductId = "9NBLGGH4NNS1";  // Microsoft Store Product ID - REQUIRED on Windows
    opts.LinuxAppId = "org.example.MyApp";   // AppStream / Flatpak ID - auto-detected inside a Flatpak or Snap
    opts.CountryCode = "us";                 // iTunes lookup region
});
```

`IAppStore` depends on Shiny's `IPlatform` (for the running app's version). MAUI apps get it from
`UseShiny()`, native apps from the Shiny hosting base classes; a plain .NET host calls
`services.AddShinyCoreServices()`.

## API

```csharp
namespace Shiny.AppStore;

public interface IAppStore
{
    Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default);  // null: no listing / lookup failed / no store
    Task<bool> OpenStore();       // false: store ID unknown or nothing handles the link
    Task<bool> OpenReviewPage();  // review section where the store has one, otherwise the store page
    Task<bool> RequestReview();   // native in-app prompt; true = requested, NOT that the user saw it
}

public record AppStoreResult(
    Version StoreVersion,
    Version CurrentVersion,       // IPlatform.AppVersion
    bool NeedsUpdate,
    string StoreUrl,
    string? ReleaseNotes = null,
    DateTimeOffset? ReleasedAt = null,
    double? AverageRating = null,
    long? RatingCount = null,
    string? MinimumOsVersion = null
);
```

| Platform | Lookup | In-app review |
|----------|--------|---------------|
| iOS / Mac Catalyst | iTunes Search API - every `AppStoreResult` field | StoreKit `AppStore.RequestReview` (16+) / `SKStoreReviewController` |
| macOS (AppKit) | iTunes Search API, `entity=macSoftware`; `macappstore://` links | `AppStore.RequestReview` on the key window (macOS 14+) |
| Android | Google Play page scrape - version and `NeedsUpdate` only | Play in-app review flow (needs a current activity) |
| Windows | Microsoft Store DisplayCatalog - version, description as release notes, release date | `StoreContext.RequestRateAndReviewAppAsync` |
| Linux | `flatpak remote-info` / `snap info` for the installed origin or tracked channel | Opens the software centre page - no in-app prompt exists |
| Other plain .NET | `null` | `false` |

## Code Generation Instructions

1. Add `using Shiny.AppStore;` where `IAppStore` is injected and `using Shiny;` where `AddAppStore` is called.
2. Always handle a `null` from `GetCurrent()` - the store may have no listing (unpublished build, wrong
   region, sideloaded or a Linux tarball install), or the lookup may fail offline.
3. Android detection parses the Play Store HTML and can break when Google changes the page; never make a
   hard force-update gate depend on Android `GetCurrent()` alone.
4. Treat `RequestReview()` returning `true` as "asked", never as "the user reviewed" - Apple and Google throttle
   the prompt silently. Don't call it from a button the user expects to always open something; use
   `OpenReviewPage()` there.
5. Call `GetCurrent()` (or set `AppleAppId`) before `OpenStore()` / `OpenReviewPage()` on Apple platforms - the
   deep link needs the numeric App ID, which `GetCurrent()` caches from the lookup.
6. Configure `WindowsProductId` for Windows; nothing works there without it.
