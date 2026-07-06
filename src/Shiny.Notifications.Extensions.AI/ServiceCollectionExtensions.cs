using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications.Extensions.AI.Internal;

namespace Shiny.Notifications.Extensions.AI;

/// <summary>
/// Dependency-injection extensions for exposing <see cref="INotificationManager"/> as LLM reminder tools.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="NotificationAITools"/> singleton whose tools wrap
    /// <see cref="INotificationManager"/> for the operations you opt-in to. Requires
    /// <c>AddNotifications()</c> to have registered <see cref="INotificationManager"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Builder callback used to opt-in reminder operations and capabilities.</param>
    /// <remarks>
    /// The generated tools assume notification permissions are already granted - they do not trigger
    /// the platform permission UI (which needs a foreground activity). Call
    /// <c>INotificationManager.RequestAccess</c> from your app before invoking the agent.
    /// </remarks>
    public static IServiceCollection AddNotificationAITools(
        this IServiceCollection services,
        Action<INotificationAIToolBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new NotificationAIToolBuilder();
        configure(builder);

        if (builder.Capabilities == ReminderAICapabilities.None)
            throw new InvalidOperationException(
                "AddNotificationAITools requires at least one AddReminders call with a capability. " +
                "An empty registration would expose no tools to the LLM.");

        services.AddSingleton(sp =>
        {
            var manager = sp.GetRequiredService<INotificationManager>();
            var tools = NotificationAIFunctionFactory.Build(manager, builder.Capabilities, builder.Channel);
            return new NotificationAITools(tools);
        });

        return services;
    }
}
