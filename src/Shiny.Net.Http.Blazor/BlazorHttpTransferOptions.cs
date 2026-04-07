namespace Shiny.Net.Http.Blazor;


public class BlazorHttpTransferOptions
{
    /// <summary>
    /// The URL of the Service Worker script that handles background HTTP transfers.
    /// Defaults to the bundled <c>./_content/Shiny.Net.Http.Blazor/http-transfer-sw.js</c>.
    /// If you serve your own service worker, point this at it (and importScripts the
    /// Shiny SW handlers from your own SW file).
    /// </summary>
    public string ServiceWorkerPath { get; set; } = "./_content/Shiny.Net.Http.Blazor/http-transfer-sw.js";
}
