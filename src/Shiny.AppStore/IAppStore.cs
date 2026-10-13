namespace Shiny.AppStore;


/// <summary>
/// The app's listing in the store it was installed from - whether a newer version is published, and
/// shortcuts to the store page, the review page and the platform's in-app review prompt.
/// </summary>
public interface IAppStore
{
    /// <summary>
    /// Looks the app up in its store. Null when the store has no listing for it, the lookup fails, or
    /// the platform has no store (plain .NET outside Linux).
    /// </summary>
    Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the app's store page. False when the store identifier is not known or nothing handles the link.
    /// </summary>
    Task<bool> OpenStore();

    /// <summary>
    /// Opens the store page on its review section where the store has one, otherwise the store page.
    /// </summary>
    Task<bool> OpenReviewPage();

    /// <summary>
    /// Presents the platform's native in-app review prompt without leaving the app (StoreKit on iOS /
    /// Mac Catalyst / macOS, the Play in-app review flow on Android, StoreContext on Windows). The OS may
    /// silently suppress the prompt based on its own throttling, so a <c>true</c> result means the prompt
    /// was requested - not that the user saw or completed it. Linux software centres have no in-app
    /// prompt, so it falls back to <see cref="OpenReviewPage"/>.
    /// </summary>
    Task<bool> RequestReview();
}


public record AppStoreResult(
    Version StoreVersion,
    Version CurrentVersion,
    bool NeedsUpdate,
    string StoreUrl,
    string? ReleaseNotes = null,
    DateTimeOffset? ReleasedAt = null,
    double? AverageRating = null,
    long? RatingCount = null,
    string? MinimumOsVersion = null
);


public class AppStoreOptions
{
    /// <summary>
    /// Apple numeric App Store ID (e.g. "1234567890"), used by both the iOS App Store and the Mac
    /// App Store. If null, resolved from the iTunes lookup by bundle identifier when calling
    /// <see cref="IAppStore.GetCurrent"/>. Required to deep-link to the store without first calling GetCurrent.
    /// </summary>
    public string? AppleAppId { get; set; }

    /// <summary>
    /// Apple bundle identifier. Falls back to <see cref="IPlatform.ApplicationIdentifier"/> (the running bundle) when null.
    /// </summary>
    public string? AppleBundleId { get; set; }

    /// <summary>
    /// Android package name. Falls back to <see cref="IPlatform.ApplicationIdentifier"/> (the running package) when null.
    /// </summary>
    public string? AndroidPackageName { get; set; }

    /// <summary>
    /// Microsoft Store Product ID (e.g. "9NBLGGH4NNS1"). Required on Windows - the package identity name
    /// in <see cref="IPlatform.ApplicationIdentifier"/> is not the Store product ID, so it cannot be backfilled.
    /// </summary>
    public string? WindowsProductId { get; set; }

    /// <summary>
    /// Linux AppStream component / Flatpak application ID (e.g. "net.shinylib.Sample"), used to query
    /// Flatpak or Snap for the published version and to deep-link the desktop software centre.
    /// Falls back to <see cref="IPlatform.ApplicationIdentifier"/> (the Flatpak app ID or Snap name), then the
    /// entry assembly name, when null.
    /// </summary>
    public string? LinuxAppId { get; set; }

    /// <summary>
    /// Two-letter region code for iTunes lookups (default "us").
    /// </summary>
    public string CountryCode { get; set; } = "us";
}
