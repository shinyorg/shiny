using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Http;
#if IOS
using Shiny.LiveActivities;
#endif

namespace Shiny;


/// <summary>
/// Registers the transfer progress surface - an iOS Live Activity, the Android foreground-service
/// notification, or your own renderer - driven by one manager on every platform.
/// </summary>
public static class TransferProgressServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TransferProgressManager"/> and whichever renderer this platform ships. Call it
    /// alongside <c>AddHttpTransfers&lt;TDelegate&gt;()</c>; nothing else is required, and no code goes in
    /// your transfer delegate.
    /// </summary>
    /// <remarks>
    /// Android registers <see cref="ForegroundNotificationRenderer"/>, which draws on the notification the
    /// transfer foreground service already posts - so there is one notification, not two. iOS registers
    /// <c>LiveActivityTransferRenderer</c>, which needs the widget extension from
    /// <c>templates/WidgetExtension</c> in your app bundle and <c>NSSupportsLiveActivities</c> in
    /// Info.plist; without them the activity starts and renders nothing. Every other platform has no
    /// surface to draw on, so the manager quietly does nothing - this is safe to call unconditionally, and
    /// you can always register your own <see cref="ITransferProgressRenderer"/>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration - which fields show, how often to update, and how progress is projected.</param>
    public static IServiceCollection AddTransferProgress(
        this IServiceCollection services,
        Action<TransferProgressOptions>? configure = null
    )
    {
        var options = new TransferProgressOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

#if ANDROID
        if (services.All(x => x.ImplementationType != typeof(ForegroundNotificationRenderer)))
            services.AddSingleton<ITransferProgressRenderer, ForegroundNotificationRenderer>();
#elif IOS
        // AddLiveActivities registers a singleton against every interface it implements, so calling it
        // twice would produce two managers and two sets of ActivityKit observers
        if (services.All(x => x.ServiceType != typeof(ILiveActivityManager)))
            services.AddLiveActivities();

        if (services.All(x => x.ImplementationType != typeof(LiveActivityTransferRenderer)))
            services.AddSingleton<ITransferProgressRenderer, LiveActivityTransferRenderer>();
#endif
        services.AddSingletonAsImplementedInterfaces<TransferProgressManager>();
        return services;
    }


    /// <summary>
    /// Registers transfer progress along with a delegate that overrides the text it shows - for
    /// localization, or for wording the built-in strings do not cover.
    /// </summary>
    /// <typeparam name="TDelegate">Your <see cref="ITransferProgressDelegate"/> implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration.</param>
    public static IServiceCollection AddTransferProgress<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(
        this IServiceCollection services,
        Action<TransferProgressOptions>? configure = null
    ) where TDelegate : class, ITransferProgressDelegate
    {
        services.AddSingleton<ITransferProgressDelegate, TDelegate>();
        return services.AddTransferProgress(configure);
    }
}
