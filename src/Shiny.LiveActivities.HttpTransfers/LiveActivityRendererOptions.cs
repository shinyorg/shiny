namespace Shiny.LiveActivities;


/// <summary>
/// The ActivityKit-specific knobs for rendering transfer progress as an iOS Live Activity.
/// </summary>
/// <remarks>
/// Everything platform-neutral - which fields show, how often to update, how progress is projected - lives
/// on <c>TransferProgressOptions</c> in <c>Shiny.Net.Http</c>. Only what genuinely has no Android meaning
/// is here.
/// </remarks>
public class LiveActivityRendererOptions
{
    /// <summary>
    /// The <c>LiveActivityRequest.Kind</c> stamped on the activity, so a widget shipping several layouts
    /// can branch on it. Defaults to <c>shiny.httptransfers</c>.
    /// </summary>
    public string? Kind { get; set; } = "shiny.httptransfers";

    /// <summary>
    /// Ask ActivityKit for a per-activity push token so a server can update the activity directly. Off by
    /// default.
    /// </summary>
    /// <remarks>
    /// Worth turning on for <em>uploads</em>: the receiving server knows how many bytes have actually
    /// landed, so it can push byte-accurate progress through the whole window where the app is suspended
    /// and a background <c>NSURLSession</c> is delivering no callbacks at all. It buys nothing for
    /// downloads, where no server knows how far the device has got. The token arrives on
    /// <see cref="ILiveActivityDelegate.OnPushTokenChanged"/>.
    /// </remarks>
    public bool RequestPushToken { get; set; }
}
