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
    /// <para>
    /// Capabilities are granted globally (<c>AddCalendar(Read)</c>) and/or per calendar id
    /// (<c>AddCalendar("work-id", All)</c>), letting one calendar be read-only while another is
    /// read/write. The generated tools enforce the rules on every call - they filter what
    /// <c>list_calendars</c>/<c>search_events</c> return and refuse writes to calendars outside the
    /// allow-list.
    /// </para>
    /// <para>
    /// The generated tools assume calendar permissions are already granted - they do not trigger the
    /// platform permission UI (which needs a foreground activity). Call
    /// <c>ICalendarStore.RequestAccess</c> from your app before invoking the agent.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCalendarAITools(
        this IServiceCollection services,
        Action<ICalendarAIToolBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        // Validate eagerly so a bad registration fails at startup rather than on first resolve.
        var builder = new CalendarAIToolBuilder();
        configure(builder);
        Validate(builder.BuildPolicy());

        return services.AddCalendarAITools((_, b) => configure(b));
    }

    /// <summary>
    /// Service-provider aware overload of
    /// <see cref="AddCalendarAITools(IServiceCollection, Action{ICalendarAIToolBuilder})"/>. The callback
    /// runs when <see cref="CalendarAITools"/> is first resolved, so the calendar ids you allow can come
    /// from your own services (a settings store, the user's calendar picker, a feature flag, etc.).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Builder callback used to opt-in calendar operations and capabilities.</param>
    public static IServiceCollection AddCalendarAITools(
        this IServiceCollection services,
        Action<IServiceProvider, ICalendarAIToolBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton(sp =>
        {
            var builder = new CalendarAIToolBuilder();
            configure(sp, builder);

            var policy = builder.BuildPolicy();
            Validate(policy);

            var store = sp.GetRequiredService<ICalendarStore>();
            return new CalendarAITools(CalendarAIFunctionFactory.Build(store, policy));
        });

        return services;
    }

    static void Validate(CalendarAccessPolicy policy)
    {
        if (policy.Effective == CalendarAICapabilities.None)
            throw new InvalidOperationException(
                "AddCalendarAITools requires at least one AddCalendar call with a capability. " +
                "An empty registration would expose no tools to the LLM.");
    }
}
