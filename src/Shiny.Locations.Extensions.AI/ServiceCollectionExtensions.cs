using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations.Extensions.AI.Internal;

namespace Shiny;

/// <summary>
/// Dependency-injection extensions for exposing <see cref="IGpsManager"/> as read-only LLM tools.
/// </summary>
public static class LocationsAiServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="LocationAITools"/> singleton whose read-only GPS tools wrap
    /// <see cref="IGpsManager"/> (current location, plus distance and rough travel-time to a
    /// destination). Requires <c>AddGps()</c> to have registered <see cref="IGpsManager"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <remarks>
    /// GPS is read-only, so there is nothing to opt-in to — the call registers all three location
    /// tools. The generated tools read the last cached GPS reading; location permission should
    /// already be granted (and, for a fresh fix, a listener started) before invoking the agent.
    /// </remarks>
    public static IServiceCollection AddLocationAITool(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var gps = sp.GetRequiredService<IGpsManager>();
            var tools = LocationAIFunctionFactory.Build(gps);
            return new LocationAITools(tools);
        });

        return services;
    }
}
