using Microsoft.Extensions.DependencyInjection;
using Shiny.AppFunctions;
using Shiny.AppFunctions.Extensions.AI;
using Shiny.AppFunctions.Extensions.AI.Internal;

namespace Shiny;

/// <summary>
/// Dependency-injection extensions for exposing app functions as LLM tools.
/// </summary>
public static class AppFunctionsAiServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="AppFunctionAITools"/> singleton whose tools call the app functions you opt-in to
    /// through <see cref="AppFunctionDispatcher"/> - the same binding, delegates and handlers Siri and Gemini use.
    /// Requires the source-generated <c>AddAppFunctions()</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Builder callback used to opt-in app functions.</param>
    /// <remarks>
    /// <para>
    /// Each tool's name, description and JSON schema come from the function's descriptor. Calls run as
    /// <see cref="AppFunctionPlatform.Other"/> and not in the foreground, so a delegate's
    /// <see cref="AppFunctionGate.OpenApp"/> refuses them the way Android does, and the LLM receives the message.
    /// </para>
    /// <para>
    /// Function ids are checked when <see cref="AppFunctionAITools"/> is first resolved; an unknown id throws.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAppFunctionAITools(
        this IServiceCollection services,
        Action<IAppFunctionAIToolBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AppFunctionAIToolBuilder();
        configure(builder);

        if (!builder.All && builder.FunctionIds.Count == 0)
            throw new InvalidOperationException(
                "AddAppFunctionAITools requires AddAllFunctions or at least one AddFunction call. " +
                "An empty registration would expose no tools to the LLM.");

        services.AddSingleton(sp =>
        {
            var dispatcher = sp.GetRequiredService<AppFunctionDispatcher>();
            return new AppFunctionAITools(AppFunctionAIFunctionFactory.Build(dispatcher, builder));
        });

        return services;
    }
}
