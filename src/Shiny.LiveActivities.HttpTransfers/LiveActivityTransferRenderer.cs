using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;
using Shiny.Net.Http;

namespace Shiny.LiveActivities;


/// <summary>
/// Draws <c>Shiny.Net.Http</c> transfer progress as an iOS Live Activity.
/// </summary>
/// <remarks>
/// A renderer and nothing more: <c>TransferProgressManager</c> in <c>Shiny.Net.Http</c> owns aggregation,
/// coalescing, scope and lifetime, so iOS and Android cannot drift in what they say. All this type does is
/// map <see cref="TransferProgressContent"/> onto ActivityKit and remember which activity belongs to which
/// key.
/// <para>
/// That map is persisted, because activities outlive the process. iOS relaunches the app in the background
/// when a background <c>NSURLSession</c> finishes, and the activity started before the app was killed is
/// still on the Lock Screen waiting to be moved to its final state.
/// </para>
/// </remarks>
public class LiveActivityTransferRenderer(
    ILiveActivityManager activities,
    LiveActivityRendererOptions options,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store,
    ILogger<LiveActivityTransferRenderer> logger
) : ITransferProgressRenderer
{
    /// <summary>The key the activity map is persisted under.</summary>
    public const string ActivityMapStoreKey = "Shiny.LiveActivities.HttpTransfers.Map";

    readonly Dictionary<string, string> map = new();
    bool loaded;


    /// <summary>
    /// iOS only, and only where ActivityKit exists. Android is deliberately excluded: <c>Shiny.Net.Http</c>
    /// must already post a foreground-service notification there, and its own renderer draws progress onto
    /// that rather than adding a second notification.
    /// </summary>
    public bool IsAvailable => OperatingSystem.IsIOS() && activities.IsSupported;


    /// <inheritdoc />
    public async Task Show(string key, TransferProgressContent content)
    {
        this.EnsureLoaded();
        var live = ToLiveActivityContent(content);

        if (this.map.TryGetValue(key, out var activityId))
        {
            var alert = content.Alert is { } a ? new LiveActivityAlert(a.Title, a.Body) : null;
            await activities.Update(activityId, live, alert).ConfigureAwait(false);
            return;
        }

        // ActivityKit refuses to start an activity while the app is in the background. That is expected
        // rather than exceptional here - a transfer can be queued from a background wake - so it is logged
        // quietly and retried on the next update, by which point the app may be visible again.
        try
        {
            var activity = await activities
                .Start(new LiveActivityRequest
                {
                    Content = live,
                    Kind = options.Kind,
                    Attributes = BuildAttributes(key, content),
                    RequestPushToken = options.RequestPushToken
                })
                .ConfigureAwait(false);

            this.map[key] = activity.Id;
            this.Persist();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not start a live activity for '{Key}' - retrying on the next update", key);
        }
    }


    /// <inheritdoc />
    public async Task Hide(string key, TransferProgressContent content, DateTimeOffset dismissAt)
    {
        this.EnsureLoaded();
        if (!this.map.TryGetValue(key, out var activityId))
            return;

        var live = ToLiveActivityContent(content);

        if (content.Alert is { } alert)
        {
            await activities
                .Update(activityId, live, new LiveActivityAlert(alert.Title, alert.Body))
                .ConfigureAwait(false);
        }

        await activities.End(activityId, live, dismissAt).ConfigureAwait(false);
        this.map.Remove(key);
        this.Persist();
    }


    /// <inheritdoc />
    public async Task Reconcile(IReadOnlyCollection<string> activeKeys)
    {
        this.EnsureLoaded();
        if (this.map.Count == 0)
            return;

        // an id the system no longer knows about is a dead map entry, not an activity to end
        var known = activities.GetAll().Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var orphan in this.map.Where(x => !known.Contains(x.Value)).Select(x => x.Key).ToList())
            this.map.Remove(orphan);

        foreach (var stale in this.map.Keys.Where(x => !activeKeys.Contains(x)).ToList())
        {
            logger.LogDebug("Ending an orphaned transfer activity for '{Key}'", stale);
            await activities.End(this.map[stale], dismissAt: DateTimeOffset.UtcNow).ConfigureAwait(false);
            this.map.Remove(stale);
        }
        this.Persist();
    }


    /// <summary>Maps the platform-neutral content onto an ActivityKit content state.</summary>
    /// <param name="content">The content to map.</param>
    public static LiveActivityContent ToLiveActivityContent(TransferProgressContent content) => new()
    {
        Title = content.Title,
        Body = content.Body,
        ShortStatus = content.ShortStatus,
        StaleDate = content.StaleDate,
        RelevanceScore = content.RelevanceScore,
        Data = content.Data,
        Progress = content.Progress is not { } bar
            ? null
            : new LiveActivityProgress
            {
                // the range form is carried through untouched - it is the whole point on iOS, where it
                // keeps the bar moving while the app is suspended and no callbacks are arriving
                Value = bar.Value,
                Start = bar.Start,
                End = bar.End,
                Indeterminate = bar.Indeterminate
            }
    };


    static Dictionary<string, string> BuildAttributes(string key, TransferProgressContent content)
    {
        var attributes = new Dictionary<string, string>(2) { ["key"] = key };

        if (content.Data.TryGetValue("direction", out var direction))
            attributes["direction"] = direction;

        return attributes;
    }


    void EnsureLoaded()
    {
        if (this.loaded)
            return;

        this.loaded = true;
        var json = store.Get<string>(ActivityMapStoreKey);
        if (String.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var stored = JsonSerializer.Deserialize(json, LiveActivityRendererJsonContext.Default.DictionaryStringString);
            if (stored != null)
            {
                foreach (var kvp in stored)
                    this.map[kvp.Key] = kvp.Value;
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Discarding a corrupt live activity map");
            store.Remove(ActivityMapStoreKey);
        }
    }


    void Persist()
    {
        if (this.map.Count == 0)
        {
            store.Remove(ActivityMapStoreKey);
            return;
        }

        var json = JsonSerializer.Serialize(this.map, LiveActivityRendererJsonContext.Default.DictionaryStringString);
        store.Set(ActivityMapStoreKey, json);
    }
}
