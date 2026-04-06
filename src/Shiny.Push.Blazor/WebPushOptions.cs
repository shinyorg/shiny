namespace Shiny.Push.Blazor;


public class WebPushOptions
{
    /// <summary>
    /// The VAPID public key (URL-safe base64) used to authenticate the push subscription with the push service.
    /// </summary>
    public required string VapidPublicKey { get; set; }

    /// <summary>
    /// Path to the service worker file that handles push events. Defaults to the bundled Shiny push service worker.
    /// Override this if you embed Shiny push handling in your own service worker via importScripts().
    /// </summary>
    public string ServiceWorkerPath { get; set; } = "./_content/Shiny.Push.Blazor/push-sw.js";
}
