using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.LiveActivities;
using Shiny.Net.Http;

namespace Shiny;


/// <summary>
/// Adds the iOS Live Activity renderer to <c>Shiny.Net.Http</c>'s transfer progress.
/// </summary>
public static class HttpTransferLiveActivitiesServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="LiveActivityTransferRenderer"/> so background transfers show up on the Lock
    /// Screen and in the Dynamic Island. Call it alongside <c>AddHttpTransfers&lt;TDelegate&gt;()</c>;
    /// <c>AddTransferProgress()</c> and <c>AddLiveActivities()</c> are called for you if you have not
    /// already called them yourself.
    /// </summary>
    /// <remarks>
    /// Everything about <em>what</em> is shown - fields, coalescing, scope, progress projection - is
    /// configured on <c>AddTransferProgress()</c> in <c>Shiny.Net.Http</c>, because Android renders the same
    /// content onto its foreground-service notification. Only the ActivityKit-specific bits are here.
    /// <para>
    /// iOS additionally needs the widget extension from <c>templates/WidgetExtension</c> in your app bundle
    /// and <c>NSSupportsLiveActivities</c> in Info.plist - without them the activity starts and renders
    /// nothing. Off iOS the renderer reports itself unavailable and is never called, so this is safe to
    /// call unconditionally.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional ActivityKit-specific configuration.</param>
    public static IServiceCollection AddHttpTransferLiveActivities(
        this IServiceCollection services,
        Action<LiveActivityRendererOptions>? configure = null
    )
    {
        var options = new LiveActivityRendererOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // AddLiveActivities registers a singleton against every interface it implements, so calling it
        // twice would produce two managers and two sets of ActivityKit observers
        if (services.All(x => x.ServiceType != typeof(ILiveActivityManager)))
            services.AddLiveActivities();

        if (services.All(x => x.ImplementationType != typeof(LiveActivityTransferRenderer)))
            services.AddSingleton<ITransferProgressRenderer, LiveActivityTransferRenderer>();

        if (services.All(x => x.ServiceType != typeof(TransferProgressOptions)))
            services.AddTransferProgress();

        return services;
    }
}
