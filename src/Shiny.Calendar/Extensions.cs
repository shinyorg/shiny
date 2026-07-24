using Microsoft.Extensions.DependencyInjection;
using Shiny.Calendar;
using Shiny.Contacts;

namespace Shiny;

public static class CalendarStoreExtensions
{
#if PLATFORM
    /// <summary>
    /// Registers the platform calendar store implementation with the Shiny container.
    /// </summary>
    public static IServiceCollection AddCalendarStore(this IServiceCollection services)
    {
        services.AddSingleton<ICalendarStore, CalendarStoreImpl>();
        return services;
    }
#endif

    extension(ICalendarStore store)
    {
        /// <summary>
        /// Convenience overload that builds and creates an event from primitive values.
        /// Returns the new event's platform identifier.
        /// </summary>
        public Task<string> CreateEvent(
            string? calendarId,
            string title,
            string? description,
            string? location,
            DateTimeOffset start,
            DateTimeOffset end,
            bool isAllDay = false,
            IEnumerable<EventReminder>? reminders = null,
            CancellationToken ct = default
        )
        {
            var evt = new CalendarEvent
            {
                CalendarId = calendarId,
                Title = title,
                Description = description,
                Location = location,
                Start = start,
                End = end,
                IsAllDay = isAllDay
            };
            if (reminders != null)
                evt.Reminders.AddRange(reminders);

            return store.CreateEvent(evt, ct);
        }

        /// <summary>
        /// Convenience overload that creates an all-day event spanning the supplied date range.
        /// </summary>
        public Task<string> CreateAllDayEvent(
            string? calendarId,
            string title,
            string? description,
            string? location,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken ct = default
        )
            => store.CreateEvent(calendarId, title, description, location, startDate, endDate, isAllDay: true, ct: ct);
    }

    extension(EventAttendee attendee)
    {
        /// <summary>
        /// Resolves the device <see cref="Contact"/> that matches this attendee by email address, or null
        /// if there is no email or no matching contact. Bridges <c>Shiny.Calendar</c> to
        /// <c>Shiny.Contacts</c>.
        /// </summary>
        public async Task<Contact?> ResolveContact(IContactStore contactStore, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(contactStore);
            if (string.IsNullOrWhiteSpace(attendee.Email))
                return null;

            var all = await contactStore.GetAll(ct).ConfigureAwait(false);
            return all.FirstOrDefault(c =>
                c.Emails.Any(e => string.Equals(e.Address, attendee.Email, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
