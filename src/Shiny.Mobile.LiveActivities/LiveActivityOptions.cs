namespace Shiny.LiveActivities;


/// <summary>
/// Configuration for live activities. Pass a configuration callback to
/// <c>AddLiveActivities(configure)</c>.
/// </summary>
/// <remarks>
/// Everything here is Android-only today, because iOS has nothing equivalent to configure — an
/// ActivityKit activity is rendered by your own widget extension and has no channel, no importance and
/// no app-settable presentation. The options object exists at all so the Android notification channel
/// stops being hard-coded English.
/// </remarks>
public class LiveActivityOptions
{
    /// <summary>
    /// The user-visible name of the Android notification channel live activities are posted to. This is
    /// what appears in Android's per-app notification settings, so it should be localized. Defaults to
    /// "Live Activities".
    /// </summary>
    /// <remarks>
    /// Android lets an app change a channel's name and description after the channel exists, so this is
    /// re-applied on every startup and a translation that arrives later still lands. Importance and sound
    /// are <em>not</em> mutable once the channel is created - the user owns those.
    /// </remarks>
    public string ChannelName { get; set; } = "Live Activities";

    /// <summary>
    /// The description shown under the channel name in Android's notification settings. Localize it.
    /// Defaults to "Ongoing updates such as deliveries, timers and scores". Set null to leave it blank.
    /// </summary>
    public string? ChannelDescription { get; set; } = "Ongoing updates such as deliveries, timers and scores";
}
