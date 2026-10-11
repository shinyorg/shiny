using System.Globalization;
using System.Text.Json;
using Foundation;
using StoreKit;
#if MACOS
using AppKit;
#else
using UIKit;
#endif

namespace Shiny.AppStore;


/// <summary>
/// The Apple App Store (iOS, Mac Catalyst) or the Mac App Store (macOS) - the iTunes lookup API for the
/// published version and StoreKit for the in-app review prompt.
/// </summary>
public class AppleAppStore(IPlatform platform, AppStoreOptions options) : IAppStore
{
#if MACOS
    // A macOS (AppKit) build is a Mac App Store product, so the lookup has to be scoped to macSoftware
    // or iTunes answers with the iOS app that shares the bundle identifier.
    const string LookupEntity = "&entity=macSoftware";

    // macappstore: opens the Mac App Store app; itms-apps: is the iOS/Mac Catalyst scheme.
    const string StoreUrlFormat = "macappstore://apps.apple.com/app/id{0}";
#else
    const string LookupEntity = "";
    const string StoreUrlFormat = "itms-apps://itunes.apple.com/app/id{0}";
#endif

    readonly HttpClient http = new();


    public async Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default)
    {
        var bundleId = options.AppleBundleId ?? NSBundle.MainBundle.BundleIdentifier;
        if (String.IsNullOrWhiteSpace(bundleId))
            return null;

        var url = $"https://itunes.apple.com/lookup?bundleId={Uri.EscapeDataString(bundleId)}&country={Uri.EscapeDataString(options.CountryCode)}{LookupEntity}";
        using var response = await this.http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return null;

        var entry = results[0];
        if (!TryGetVersion(entry, "version", out var storeVersion))
            return null;

        // Cache the discovered trackId so OpenStore/OpenReviewPage work without the caller wiring AppleAppId.
        if (entry.TryGetProperty("trackId", out var trackId) && trackId.ValueKind == JsonValueKind.Number)
            options.AppleAppId ??= trackId.GetInt64().ToString(CultureInfo.InvariantCulture);

        // CFBundleShortVersionString - what users see in the store
        var current = platform.AppVersion;
        return new AppStoreResult(
            storeVersion,
            current,
            storeVersion > current,
            entry.TryGetProperty("trackViewUrl", out var trackUrl) ? trackUrl.GetString() ?? String.Empty : String.Empty,
            entry.TryGetProperty("releaseNotes", out var notes) ? notes.GetString() : null,
            entry.TryGetProperty("currentVersionReleaseDate", out var date) && date.TryGetDateTimeOffset(out var dto) ? dto : null,
            entry.TryGetProperty("averageUserRating", out var rating) && rating.ValueKind == JsonValueKind.Number ? rating.GetDouble() : null,
            entry.TryGetProperty("userRatingCount", out var count) && count.ValueKind == JsonValueKind.Number ? count.GetInt64() : null,
            entry.TryGetProperty("minimumOsVersion", out var minOs) ? minOs.GetString() : null
        );
    }


    public Task<bool> OpenStore()
    {
        // Need an actual App ID for the deep link — callers should configure it or call GetCurrent first.
        var appId = options.AppleAppId;
        if (String.IsNullOrWhiteSpace(appId))
            return Task.FromResult(false);

        return this.Open(String.Format(CultureInfo.InvariantCulture, StoreUrlFormat, appId));
    }


    public Task<bool> OpenReviewPage()
    {
        var appId = options.AppleAppId;
        if (String.IsNullOrWhiteSpace(appId))
            return Task.FromResult(false);

        return this.Open(String.Format(CultureInfo.InvariantCulture, StoreUrlFormat, appId) + "?action=write-review");
    }


#if MACOS
    public Task<bool> RequestReview()
    {
        // AppStore.RequestReview(NSViewController) is the only supported in-app prompt on AppKit -
        // SKStoreReviewController.RequestReview() was deprecated in macOS 14. There is nothing to
        // anchor the prompt to before the first window exists, hence the false result.
        if (!OperatingSystem.IsMacOSVersionAtLeast(14))
            return Task.FromResult(false);

        return platform.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                var app = NSApplication.SharedApplication;
                var controller =
                    app.KeyWindow?.ContentViewController ??
                    app.MainWindow?.ContentViewController;

                if (controller == null)
                    return false;

                StoreKit.AppStore.RequestReview(controller);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
    }


    Task<bool> Open(string url) => platform.InvokeOnMainThreadAsync(
        () => NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(url))
    );
#else
    public Task<bool> RequestReview() => platform.InvokeOnMainThreadAsync(() =>
    {
        try
        {
            // RequestReview(UIWindowScene) is the iOS 14+ API; pick the foreground-active scene
            // so the prompt anchors to the visible window in multi-scene apps.
            var scene = UIApplication.SharedApplication.ConnectedScenes
                .OfType<UIWindowScene>()
                .FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive)
                ?? UIApplication.SharedApplication.ConnectedScenes
                    .OfType<UIWindowScene>()
                    .FirstOrDefault();

            if (scene == null)
                return false;

            // AppStore.RequestReview is the iOS/Mac Catalyst 16+ replacement; SKStoreReviewController
            // was obsoleted on 18+. Fall back to the older API only where the new one isn't available.
            if (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16))
                StoreKit.AppStore.RequestReview(scene);
            else
                SKStoreReviewController.RequestReview(scene);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    });


    async Task<bool> Open(string url)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        platform.InvokeOnMainThread(() =>
            UIApplication.SharedApplication.OpenUrl(
                new NSUrl(url),
                new UIApplicationOpenUrlOptions(),
                success => tcs.TrySetResult(success)
            )
        );
        return await tcs.Task.ConfigureAwait(false);
    }
#endif


    static bool TryGetVersion(JsonElement element, string propertyName, out Version version)
    {
        version = new Version(0, 0);
        if (!element.TryGetProperty(propertyName, out var prop))
            return false;

        var str = prop.GetString();
        return !String.IsNullOrWhiteSpace(str) && Version.TryParse(str, out version!);
    }
}
