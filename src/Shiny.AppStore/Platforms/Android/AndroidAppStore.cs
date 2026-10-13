using System.Text.RegularExpressions;
using Android.Content;
using Xamarin.Google.Android.Play.Core.Review;

namespace Shiny.AppStore;


/// <summary>
/// Google Play. There is no public Play API for an app's published version, so <see cref="GetCurrent"/>
/// reads it out of the store page; the in-app review prompt is the Play in-app review flow.
/// </summary>
public partial class AndroidAppStore(AndroidPlatform platform, AppStoreOptions options) : IAppStore
{
    readonly HttpClient http = new();

    // Two regex strategies because Google's HTML changes frequently. The JSON-LD pattern is more stable
    // but isn't always present; the [[["x.y.z"]]] AF_initDataCallback pattern is the legacy fallback.
    [GeneratedRegex("""\[\[\["(?<v>\d+(?:\.\d+){1,3})"\]\]""", RegexOptions.Singleline)]
    private static partial Regex GooglePlayVersionRegex();

    [GeneratedRegex(""""softwareVersion"\s*:\s*"(?<v>\d+(?:\.\d+){1,3})"""", RegexOptions.Singleline)]
    private static partial Regex GooglePlayJsonLdVersionRegex();


    string PackageName => options.AndroidPackageName ?? platform.ApplicationIdentifier!;


    public async Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default)
    {
        var pkg = this.PackageName;
        if (String.IsNullOrWhiteSpace(pkg))
            return null;

        var url = $"https://play.google.com/store/apps/details?id={Uri.EscapeDataString(pkg)}&hl=en&gl={Uri.EscapeDataString(options.CountryCode)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Google serves a different (often heavier) page to mobile user agents — pin a desktop UA for stable scraping.
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ShinyAppStore/1.0)");

        using var response = await this.http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var versionMatch = GooglePlayJsonLdVersionRegex().Match(html);
        if (!versionMatch.Success)
            versionMatch = GooglePlayVersionRegex().Match(html);

        if (!versionMatch.Success)
            return null;

        if (!Version.TryParse(versionMatch.Groups["v"].Value, out var storeVersion))
            return null;

        var current = platform.AppVersion;
        return new AppStoreResult(
            storeVersion,
            current,
            storeVersion > current,
            $"https://play.google.com/store/apps/details?id={pkg}"
        );
    }


    public Task<bool> OpenStore() => Task.FromResult(this.Open($"market://details?id={this.PackageName}"));

    // Play Store opens the listing on the reviews tab when the user scrolls — there is no separate review URL.
    public Task<bool> OpenReviewPage() => this.OpenStore();


    public async Task<bool> RequestReview()
    {
        var activity = platform.CurrentActivity;
        if (activity == null)
            return false;

        try
        {
            var manager = ReviewManagerFactory.Create(activity);

            // Play Core surfaces results through GMS Task completion listeners rather than awaitables,
            // so bridge each flow to a TaskCompletionSource.
            var requestTcs = new TaskCompletionSource<Android.Gms.Tasks.Task>();
            manager.RequestReviewFlow().AddOnCompleteListener(new ReviewFlowListener(requestTcs));
            var requestFlow = await requestTcs.Task.ConfigureAwait(false);

            if (!requestFlow.IsSuccessful || requestFlow.Result is not ReviewInfo reviewInfo)
                return false;

            var launchTcs = new TaskCompletionSource<Android.Gms.Tasks.Task>();
            manager.LaunchReviewFlow(activity, reviewInfo).AddOnCompleteListener(new ReviewFlowListener(launchTcs));
            var launchFlow = await launchTcs.Task.ConfigureAwait(false);

            // The OS may suppress the actual sheet (quota); a successful flow is the best signal available.
            return launchFlow.IsSuccessful;
        }
        catch (Exception)
        {
            return false;
        }
    }


    bool Open(string uri)
    {
        try
        {
            var intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(uri));
            intent.AddFlags(ActivityFlags.NewTask);
            platform.AppContext.StartActivity(intent);
            return true;
        }
        catch (ActivityNotFoundException)
        {
            // no Play Store on the device (an emulator image without Google Play, a de-Googled phone)
            return false;
        }
    }


    sealed class ReviewFlowListener(TaskCompletionSource<Android.Gms.Tasks.Task> tcs)
        : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
    {
        public void OnComplete(Android.Gms.Tasks.Task task) => tcs.TrySetResult(task);
    }
}
