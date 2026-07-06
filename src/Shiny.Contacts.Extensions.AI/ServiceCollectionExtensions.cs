using Microsoft.Extensions.DependencyInjection;
using Shiny.Contacts.Extensions.AI.Internal;

namespace Shiny.Contacts.Extensions.AI;

/// <summary>
/// Dependency-injection extensions for exposing <see cref="IContactStore"/> as LLM tools.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ContactAITools"/> singleton whose tools wrap <see cref="IContactStore"/>
    /// for the operations you opt-in to. Requires <c>AddContactStore()</c> to have registered
    /// <see cref="IContactStore"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Builder callback used to opt-in contact operations and capabilities.</param>
    /// <remarks>
    /// The generated tools assume contact permissions are already granted - they do not trigger the
    /// platform permission UI (which needs a foreground activity). Call
    /// <c>IContactStore.RequestAccess</c> from your app before invoking the agent.
    /// </remarks>
    public static IServiceCollection AddContactsAITools(
        this IServiceCollection services,
        Action<IContactAIToolBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ContactAIToolBuilder();
        configure(builder);

        if (builder.Capabilities == ContactAICapabilities.None)
            throw new InvalidOperationException(
                "AddContactsAITools requires at least one AddContacts call with a capability. " +
                "An empty registration would expose no tools to the LLM.");

        services.AddSingleton(sp =>
        {
            var store = sp.GetRequiredService<IContactStore>();
            var tools = ContactAIFunctionFactory.Build(store, builder.Capabilities);
            return new ContactAITools(tools);
        });

        return services;
    }
}
