using Microsoft.Extensions.DependencyInjection;
using Shiny.Contacts;

namespace Shiny;

public static class ContactStoreExtensions
{
#if ANDROID || IOS
    /// <summary>
    /// Registers the platform contact store implementation with the Shiny container.
    /// </summary>
    public static IServiceCollection AddContactStore(this IServiceCollection services)
    {
        services.AddSingleton<IContactStore, ContactStoreImpl>();
        return services;
    }
#endif

    extension(IContactStore store)
    {
        public async Task<IReadOnlyList<char>> GetFamilyNameFirstLetters(CancellationToken ct = default)
        {
            var contacts = await store.GetAll(ct);
            return contacts
                .Where(c => !string.IsNullOrWhiteSpace(c.FamilyName))
                .Select(c => char.ToUpperInvariant(c.FamilyName![0]))
                .Distinct()
                .Order()
                .ToList();
        }
    }
}
