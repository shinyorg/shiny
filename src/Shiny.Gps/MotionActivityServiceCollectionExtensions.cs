using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;
#if ANDROID
using Android.App;
using Android.Gms.Common;
#endif

namespace Shiny;


public static class MotionActivityServiceCollectionExtensions
{
    #if APPLE

    /// <summary>
    /// Registers motion activity recognition services with the Shiny container
    /// </summary>
    /// <typeparam name="T">The IMotionActivityDelegate to call</typeparam>
    /// <param name="services">The service collection to configure</param>
    public static IServiceCollection AddMotionActivity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IMotionActivityDelegate
        => services.AddMotionActivity(typeof(T));

    /// <summary>
    /// Registers motion activity recognition services with the Shiny container
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="delegateType">The IMotionActivityDelegate to call</param>
    public static IServiceCollection AddMotionActivity(this IServiceCollection services, Type? delegateType = null)
    {
        if (delegateType != null)
        {
            services.AddSingleton(delegateType);
            services.AddSingleton(typeof(IMotionActivityDelegate), sp => sp.GetRequiredService(delegateType));
        }

        services.AddSingleton<MotionActivityManager>();
        services.AddSingleton<IMotionActivityManager>(sp => sp.GetRequiredService<MotionActivityManager>());
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<MotionActivityManager>());
        return services;
    }

    #elif ANDROID

    /// <summary>
    /// Registers motion activity recognition services with the Shiny container
    /// </summary>
    /// <typeparam name="T">The IMotionActivityDelegate to call</typeparam>
    /// <param name="services">The service collection to configure</param>
    public static IServiceCollection AddMotionActivity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IMotionActivityDelegate
        => services.AddMotionActivity(typeof(T));

    /// <summary>
    /// Registers motion activity recognition services with the Shiny container
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="delegateType">The IMotionActivityDelegate to call</param>
    public static IServiceCollection AddMotionActivity(this IServiceCollection services, Type? delegateType = null)
    {
        if (delegateType != null)
        {
            services.AddSingleton(delegateType);
            services.AddSingleton(typeof(IMotionActivityDelegate), sp => sp.GetRequiredService(delegateType));
        }

        var resultCode = GoogleApiAvailability
            .Instance
            .IsGooglePlayServicesAvailable(Application.Context);

        if (resultCode == ConnectionResult.Success)
        {
            services.AddSingleton<MotionActivityManager>();
            services.AddSingleton<IMotionActivityManager>(sp => sp.GetRequiredService<MotionActivityManager>());
            services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<MotionActivityManager>());
        }

        return services;
    }

    #else

    /// <summary>
    /// Motion activity recognition is not supported on this platform
    /// </summary>
    public static IServiceCollection AddMotionActivity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IMotionActivityDelegate
        => services;

    /// <summary>
    /// Motion activity recognition is not supported on this platform
    /// </summary>
    public static IServiceCollection AddMotionActivity(this IServiceCollection services, Type? delegateType = null)
        => services;

    #endif
}
