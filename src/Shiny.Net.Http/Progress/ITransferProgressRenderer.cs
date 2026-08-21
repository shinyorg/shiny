using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


/// <summary>
/// Draws transfer progress on whatever surface a platform offers - an iOS Live Activity, the Android
/// foreground-service notification, or something an app supplies itself.
/// </summary>
/// <remarks>
/// <see cref="TransferProgressManager"/> owns all the logic worth sharing (aggregation, coalescing, scope,
/// lifecycle) and a renderer owns nothing but the drawing, so the two platforms cannot drift in what they
/// say. Register implementations in DI; every available one is driven.
/// </remarks>
public interface ITransferProgressRenderer
{
    /// <summary>
    /// Whether this renderer can draw in this process right now - OS version, user permission, required
    /// app configuration. False renderers are skipped entirely and never receive calls.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Creates or updates the surface for a key. Called only for updates that survive coalescing, so it
    /// should draw unconditionally rather than doing its own throttling.
    /// </summary>
    /// <param name="key">Identifies the surface - a transfer identifier, or the summary key.</param>
    /// <param name="content">The complete content to draw.</param>
    Task Show(string key, TransferProgressContent content);

    /// <summary>
    /// Retires the surface for a key, optionally leaving a final state on screen until
    /// <paramref name="dismissAt"/>.
    /// </summary>
    /// <param name="key">The surface to retire.</param>
    /// <param name="content">The final content to show while it lingers.</param>
    /// <param name="dismissAt">When it should disappear. In the past means immediately.</param>
    Task Hide(string key, TransferProgressContent content, DateTimeOffset dismissAt);

    /// <summary>
    /// Retires anything this renderer left behind that no longer corresponds to a live transfer. Called
    /// once at startup, because surfaces outlive the process - iOS relaunches the app in the background to
    /// finish a transfer, and an activity from a previous launch is still on the Lock Screen.
    /// </summary>
    /// <param name="activeKeys">The keys that should still exist.</param>
    Task Reconcile(IReadOnlyCollection<string> activeKeys);
}
