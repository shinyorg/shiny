using Shiny.Calendar;
using Shiny.Calendar.Extensions.AI;
using Shiny.Calendar.Extensions.AI.Internal;

namespace Shiny;

/// <summary>
/// Dependency-injection extensions for exposing <see cref="ICalendarStore"/> as LLM tools.
/// </summary>
public static class CalendarAiServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="CalendarAITools"/> singleton whose tools wrap <see cref="ICalendarStore"/>
    /// for the operations you opt-in to. Requires <c>AddCalendarStore()</c> to have registered
    /// <see cref="ICalendarStore"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Builder callback used to opt-in calendar operations and capabilities.</param>
    /// <remarks>
    /// The generated tools assume calendar permissions are already granted - they do not trigger the
    /// platform permission UI (which needs a foreground activity). Call
    /// <c>ICalendarStore.RequestAccess</c> from your app before invoking the agent.
    /// </remarks>
    public static IServiceCollection AddCalendarAITools(
        this IServiceCollection services,
        Action<ICalendarAIToolBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new CalendarAIToolBuilder();
        configure(builder);

        if (builder.Capabilities == CalendarAICapabilities.None)
            throw new InvalidOperationException(
                "AddCalendarAITools requires at least one AddCalendar call with a capability. " +
                "An empty registration would expose no tools to the LLM.");

        services.AddSingleton(sp =>
        {
            var store = sp.GetRequiredService<ICalendarStore>();
            var tools = CalendarAIFunctionFactory.Build(store, builder.Capabilities);
            return new CalendarAITools(tools);
        });

        return services;
    }
}
