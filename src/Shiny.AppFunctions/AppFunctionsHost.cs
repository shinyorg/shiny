using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.AppFunctions;

/// <summary>
/// The "host ready" signal. The OS can call in before the app has built its DI container (an iOS cold start from
/// Siri, or an Android service bound early), so the platform layers wait here, with a timeout.
/// </summary>
public static class AppFunctionsHost
{
    static TaskCompletionSource<AppFunctionDispatcher> ready = NewSource();

    public static bool IsReady => ready.Task.IsCompletedSuccessfully;

    /// <summary>How long a platform call waits for the host before failing. 10 seconds by default.</summary>
    public static TimeSpan ReadyTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public static async Task<AppFunctionDispatcher> WaitForDispatcher(CancellationToken cancellationToken = default)
    {
        try
        {
            return await ready.Task.WaitAsync(ReadyTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            throw new AppFunctionException(AppFunctionErrorCode.AppError, "The app did not finish starting in time. Did you call AddAppFunctions()?");
        }
    }

    internal static void SetReady(AppFunctionDispatcher dispatcher) => ready.TrySetResult(dispatcher);

    /// <summary>Tests only.</summary>
    internal static void Reset() => ready = NewSource();

    static TaskCompletionSource<AppFunctionDispatcher> NewSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public static class AppFunctionsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the runtime (dispatcher and the platform bridge) with a registry. Called by the generated
    /// <c>AddAppFunctions()</c>; call it yourself only with a hand-written registry.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddAppFunctionsRuntime<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRegistry>(this IServiceCollection services)
        where TRegistry : class, IAppFunctionRegistry
    {
        services.TryAddSingleton<IAppFunctionRegistry, TRegistry>();
        services.TryAddSingleton<AppFunctionDispatcher>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<Shiny.IShinyStartupTask, AppFunctionsStartupTask>());
        return services;
    }
}

/// <summary>Marks the host ready and connects the platform bridge once Shiny has built the container.</summary>
sealed class AppFunctionsStartupTask(AppFunctionDispatcher dispatcher) : Shiny.IShinyStartupTask
{
#if ANDROID
    // The service is only referenced from AndroidManifest.xml, so without this the trimmer removes it in Release
    // builds and no Java stub is generated ("Didn't find class shiny.appfunctions.ShinyAppFunctionService").
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(ShinyAppFunctionService))]
#endif
    public void Start()
    {
        AppFunctionsHost.SetReady(dispatcher);
#if IOS
        Platforms.iOS.AppleBridge.Register();
#endif
    }
}
