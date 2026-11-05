using System.Diagnostics.CodeAnalysis;
using Shiny.Infrastructure;

namespace Shiny;


/// <summary>
/// Starts Shiny from an Uno app's Android <c>Application.OnCreate</c>.
/// </summary>
public static class AndroidUnoShinyExtensions
{
    /// <summary>
    /// Builds the Uno.Extensions host - and so starts Shiny - from your Android <c>Application.OnCreate</c>. Uno only
    /// calls <c>OnLaunched</c> once an activity starts (and with the Skia renderer only creates the App then), so when
    /// Android starts the process in the background for a job, a push or a geofence, the host the template builds there
    /// would never exist. <c>GetShinyBuilder</c> in <c>OnLaunched</c> later returns this same builder and host.
    /// </summary>
    /// <remarks>
    /// Works with both of Uno's Android renderers - it touches neither <c>NativeApplication</c> nor
    /// <c>ApplicationActivity</c>, which are different types under the native and Skia renderers.
    /// </remarks>
    /// <typeparam name="TApp">Your Uno <c>App</c></typeparam>
    /// <param name="application">Your <c>NativeApplication</c></param>
    [RequiresDynamicCode("Builds the Uno.Extensions host, whose configuration binding may require generating dynamic code at runtime.")]
    [RequiresUnreferencedCode("Builds the Uno.Extensions host, whose configuration binding cannot be statically analyzed.")]
    public static void StartShiny<TApp>(this Android.App.Application application)
        where TApp : Microsoft.UI.Xaml.Application, IShinyUnoApplication
        => ShinyUnoHost.GetOrCreateBuilder<TApp>().Build();
}
