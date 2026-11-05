using Uno.Extensions.Hosting;

namespace Shiny;


/// <summary>
/// Implemented by your Uno <c>App</c> so Shiny can create its <see cref="IApplicationBuilder"/> - once per
/// process, either from <c>OnLaunched</c> or, on Android, from <c>Application.OnCreate</c> before any activity exists.
/// </summary>
public interface IShinyUnoApplication
{
    /// <summary>
    /// Configure the application builder - everything the Uno template does between <c>this.CreateBuilder(args)</c>
    /// and <c>NavigateAsync</c> / <c>Build</c>, including <c>.Configure(host => host.UseShiny())</c>.
    /// </summary>
    /// <remarks>
    /// Runs once per process. On Android it can run from <c>Application.OnCreate</c> when the OS starts the app in the
    /// background (a job, a push, a geofence), so it must not touch UI or assume a launch argument.
    /// </remarks>
    static abstract void ConfigureApp(IApplicationBuilder builder);
}
