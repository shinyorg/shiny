using Shiny.Calendar;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Real EventKit / CalendarContract CRUD. Everything is created in a calendar the suite makes for itself (falling
/// back to the first writable calendar when the platform refuses), and every title carries <see cref="Prefix"/> so
/// TeardownAsync can sweep anything an earlier, crashed run left behind.
/// </summary>
public class CalendarTests(ICalendarStore store) : DeviceTestSuite
{
    public const string Prefix = "ShinyE2E";

    string calendarId = null!;
    bool ownsCalendar;

    public override string Name => "Calendar";


    public override async Task SetupAsync(TestContext context)
    {
        var access = await store.RequestAccess(CalendarAccessType.ReadWrite, context.CancellationToken);
        if (access != AccessState.Available)
            throw new SkipException($"Calendar access is {access} - the runner pre-grants it on simulators/emulators");

        await this.SweepAsync(context);
        try
        {
            this.calendarId = await store.Create(NewMarker(), "#34C759", context.CancellationToken);
            this.ownsCalendar = true;
        }
        catch (Exception ex)
        {
            context.Log($"Could not create a test calendar ({ex.Message}) - using the first writable one");
            var writable = (await store.GetAll(context.CancellationToken)).FirstOrDefault(x => !x.IsReadOnly)
                ?? throw new SkipException("No writable calendar on this device and one could not be created");

            this.calendarId = writable.Id!;
        }
    }


    public override Task TeardownAsync(TestContext context) => this.SweepAsync(context);


    protected override void Define()
    {
        this.Test("Access_IsAvailable", _ =>
        {
            Assert.Equal(AccessState.Available, store.GetCurrentAccess());
            return Task.CompletedTask;
        });

        this.Test("Calendars_ListIncludesTestCalendar", async ctx =>
        {
            var all = await store.GetAll(ctx.CancellationToken);
            ctx.Log($"{all.Count} calendar(s): {String.Join(", ", all.Select(x => $"{x.Name}{(x.IsReadOnly ? " (ro)" : "")}"))}");
            Assert.Contains(all, x => x.Id == this.calendarId);

            var single = await store.GetById(this.calendarId, ctx.CancellationToken);
            Assert.NotNull(single);
            Assert.False(single.IsReadOnly);
        });

        this.Test("Calendar_CreateUpdateDelete", async ctx =>
        {
            ctx.SkipUnless(this.ownsCalendar, "This device would not let the suite create a calendar");

            var name = NewMarker();
            var id = await store.Create(name, "#FF3B30", ctx.CancellationToken);
            try
            {
                var created = await store.GetById(id, ctx.CancellationToken);
                Assert.NotNull(created);
                Assert.Equal(name, created.Name);
                ctx.Log($"Color read back as {created.Color}");
                Assert.NotNull(created.Color);
                if (ctx.IsAndroid)
                    Assert.Equal("#FF3B30", created.Color, ignoreCase: true);

                var renamed = name + "-renamed";
                await store.Update(id, renamed, "#007AFF", ctx.CancellationToken);
                var updated = await store.GetById(id, ctx.CancellationToken);
                Assert.NotNull(updated);
                Assert.Equal(renamed, updated.Name);
                if (ctx.IsAndroid)
                    Assert.Equal("#007AFF", updated.Color, ignoreCase: true);
            }
            finally
            {
                await store.Delete(id, ctx.CancellationToken);
            }
            Assert.Null(await store.GetById(id, ctx.CancellationToken));
        });

        this.Test("Event_CreateReadDelete", async ctx =>
        {
            var start = RoundToSecond(DateTimeOffset.Now.AddDays(1));
            var source = new CalendarEvent(NewMarker(), start, start.AddHours(1))
            {
                CalendarId = this.calendarId,
                Description = "Created by Shiny.DeviceTests",
                Location = "1 Infinite Loop",
                Url = "https://shinylib.net/",
                Availability = EventAvailability.Free
            };
            var id = await store.CreateEvent(source, ctx.CancellationToken);
            Assert.False(String.IsNullOrWhiteSpace(id));
            try
            {
                var read = await store.GetEvent(id, ctx.CancellationToken);
                Assert.NotNull(read);
                Assert.Equal(id, read.Id);
                Assert.Equal(this.calendarId, read.CalendarId);
                Assert.Equal(source.Title, read.Title);
                Assert.Equal(source.Description, read.Description);
                Assert.Equal(source.Location, read.Location);
                AssertSameInstant(source.Start, read.Start);
                AssertSameInstant(source.End, read.End);
                Assert.False(read.IsAllDay);
                Assert.False(read.IsRecurring);
                Assert.Null(read.RecurrenceRule);
                Assert.Equal(EventAvailability.Free, read.Availability);

                // CalendarContract has no URL column - only EventKit keeps it
                if (ctx.IsApple)
                    Assert.Equal(source.Url, read.Url);
                else
                    ctx.Log($"Url read back as '{read.Url}' (not stored on Android)");
            }
            finally
            {
                await store.DeleteEvent(id, ct: ctx.CancellationToken);
            }
            Assert.Null(await store.GetEvent(id, ctx.CancellationToken));
        });

        this.Test("Event_Update", async ctx =>
        {
            var start = RoundToSecond(DateTimeOffset.Now.AddDays(2));
            var id = await store.CreateEvent(new CalendarEvent(NewMarker(), start, start.AddMinutes(30))
            {
                CalendarId = this.calendarId,
                Location = "Before"
            }, ctx.CancellationToken);
            try
            {
                var evt = await store.GetEvent(id, ctx.CancellationToken);
                Assert.NotNull(evt);

                evt.Title += "-updated";
                evt.Location = "After";
                evt.Description = "Updated";
                evt.Start = start.AddHours(3);
                evt.End = start.AddHours(5);
                evt.Availability = EventAvailability.Tentative;
                await store.UpdateEvent(evt, ctx.CancellationToken);

                var updated = await store.GetEvent(id, ctx.CancellationToken);
                Assert.NotNull(updated);
                Assert.Equal(evt.Title, updated.Title);
                Assert.Equal("After", updated.Location);
                Assert.Equal("Updated", updated.Description);
                AssertSameInstant(start.AddHours(3), updated.Start);
                AssertSameInstant(start.AddHours(5), updated.End);

                // EventKit only honours Tentative on calendars that support it (CalDAV) - local ones keep Busy
                ctx.Log($"Availability read back as {updated.Availability}");
                if (ctx.IsAndroid)
                    Assert.Equal(EventAvailability.Tentative, updated.Availability);
            }
            finally
            {
                await store.DeleteEvent(id, ct: ctx.CancellationToken);
            }
        });

        this.Test("Event_PreservesInstantAcrossTimeZones", async ctx =>
        {
            // a start in a zone far from the device's - the store must keep the instant, not the wall clock
            // Unspecified kind - a Local DateTime only accepts the device's own offset
            var tokyo = new DateTimeOffset(DateTime.SpecifyKind(DateTime.Today.AddDays(3).AddHours(9), DateTimeKind.Unspecified), TimeSpan.FromHours(9));
            var id = await store.CreateEvent(new CalendarEvent(NewMarker(), tokyo, tokyo.AddMinutes(45))
            {
                CalendarId = this.calendarId
            }, ctx.CancellationToken);
            try
            {
                var read = await store.GetEvent(id, ctx.CancellationToken);
                Assert.NotNull(read);
                ctx.Log($"Wrote {tokyo:o}, read {read.Start:o}");
                AssertSameInstant(tokyo, read.Start);
                AssertSameInstant(tokyo.AddMinutes(45), read.End);
            }
            finally
            {
                await store.DeleteEvent(id, ct: ctx.CancellationToken);
            }
        });

        this.Test("Event_AllDay", async ctx =>
        {
            var day = DateTime.Today.AddDays(4);
            var id = await store.CreateAllDayEvent(
                this.calendarId,
                NewMarker(),
                null,
                null,
                new DateTimeOffset(day),
                new DateTimeOffset(day.AddDays(1)),
                ctx.CancellationToken
            );
            try
            {
                var read = await store.GetEvent(id, ctx.CancellationToken);
                Assert.NotNull(read);
                ctx.Log($"All-day for {day:yyyy-MM-dd}: start {read.Start:o}, end {read.End:o}");
                Assert.True(read.IsAllDay);

                // all-day storage is midnight-based - UTC on Android, the floating local day on Apple
                Assert.True(
                    read.Start.UtcDateTime.Date == day || read.Start.LocalDateTime.Date == day,
                    $"All-day event for {day:yyyy-MM-dd} read back starting {read.Start:o}"
                );

                var inWindow = await store.GetEvents(this.calendarId, new DateTimeOffset(day.AddDays(-1)), new DateTimeOffset(day.AddDays(2)), ctx.CancellationToken);
                Assert.Contains(inWindow, x => x.Id == id);
            }
            finally
            {
                await store.DeleteEvent(id, ct: ctx.CancellationToken);
            }
        });

        this.Test("Event_Reminders", async ctx =>
        {
            var start = RoundToSecond(DateTimeOffset.Now.AddDays(5));
            var id = await store.CreateEvent(
                this.calendarId,
                NewMarker(),
                null,
                null,
                start,
                start.AddHours(1),
                reminders: [new EventReminder(TimeSpan.FromMinutes(15)), new EventReminder(TimeSpan.FromHours(1))],
                ct: ctx.CancellationToken
            );
            try
            {
                var read = await store.GetEvent(id, ctx.CancellationToken);
                Assert.NotNull(read);
                Assert.Equal([15d, 60d], read.Reminders.Select(x => x.Offset.TotalMinutes).Order());

                // update replaces the whole reminder set
                read.Reminders = [new EventReminder(TimeSpan.FromMinutes(5))];
                await store.UpdateEvent(read, ctx.CancellationToken);

                var updated = await store.GetEvent(id, ctx.CancellationToken);
                Assert.NotNull(updated);
                Assert.Equal([5d], updated.Reminders.Select(x => x.Offset.TotalMinutes));
            }
            finally
            {
                await store.DeleteEvent(id, ct: ctx.CancellationToken);
            }
        });

        this.Test("Event_Attendees", async ctx =>
        {
            ctx.SkipUnless(ctx.IsAndroid, "EventKit cannot write attendees - the store ignores them on Apple");

            var start = RoundToSecond(DateTimeOffset.Now.AddDays(6));
            var source = new CalendarEvent(NewMarker(), start, start.AddHours(1)) { CalendarId = this.calendarId };
            source.Attendees.Add(new EventAttendee("Shiny Tester", "tester@shinylib.net", AttendeeRole.Optional));
            var id = await store.CreateEvent(source, ctx.CancellationToken);
            try
            {
                var read = await store.GetEvent(id, ctx.CancellationToken);
                Assert.NotNull(read);
                var attendee = Assert.Single(read.Attendees, x => x.Email == "tester@shinylib.net");
                Assert.Equal("Shiny Tester", attendee.Name);
                Assert.Equal(AttendeeRole.Optional, attendee.Role);
            }
            finally
            {
                await store.DeleteEvent(id, ct: ctx.CancellationToken);
            }
        });

        this.Test("Events_QueryByWindowFilterSortPage", async ctx =>
        {
            var marker = NewMarker();
            var baseStart = RoundToSecond(DateTimeOffset.Now.Date.AddDays(10).AddHours(10));
            List<string> ids = [];
            try
            {
                for (var i = 0; i < 3; i++)
                {
                    var start = baseStart.AddDays(i);
                    ids.Add(await store.CreateEvent(new CalendarEvent($"{marker}-{i}", start, start.AddHours(1))
                    {
                        CalendarId = this.calendarId
                    }, ctx.CancellationToken));
                }

                var windowStart = baseStart.AddHours(-1);
                var windowEnd = baseStart.AddDays(3);

                var all = await store
                    .Query()
                    .ForCalendar(this.calendarId)
                    .Between(windowStart, windowEnd)
                    .TitleContains(marker)
                    .OrderBy(CalendarEventSortField.Start, descending: true)
                    .ToListAsync(ctx.CancellationToken);
                Assert.Equal([$"{marker}-2", $"{marker}-1", $"{marker}-0"], all.Select(x => x.Title));

                var page = await store
                    .Query()
                    .ForCalendar(this.calendarId)
                    .Between(windowStart, windowEnd)
                    .TitleContains(marker)
                    .OrderBy(CalendarEventSortField.Title)
                    .Skip(1)
                    .Take(1)
                    .ToListAsync(ctx.CancellationToken);
                Assert.Equal($"{marker}-1", Assert.Single(page).Title);

                var count = await store
                    .Query()
                    .ForCalendar(this.calendarId)
                    .Between(windowStart, windowEnd)
                    .Where(x => x.Title.StartsWith(marker, StringComparison.Ordinal))
                    .CountAsync(ctx.CancellationToken);
                Assert.Equal(3, count);

                // a window that ends before the third event starts must leave it out
                var narrowed = await store.GetEvents(this.calendarId, windowStart, baseStart.AddDays(1).AddHours(2), ctx.CancellationToken);
                var narrowedTitles = narrowed.Where(x => x.Title.StartsWith(marker, StringComparison.Ordinal)).Select(x => x.Title).Order().ToList();
                Assert.Equal([$"{marker}-0", $"{marker}-1"], narrowedTitles);

                // another calendar must not see them
                var other = (await store.GetAll(ctx.CancellationToken)).FirstOrDefault(x => x.Id != this.calendarId);
                if (other == null)
                {
                    ctx.Log("Only one calendar on the device - cross-calendar isolation not checked");
                }
                else
                {
                    var elsewhere = await store.Query().ForCalendar(other.Id).Between(windowStart, windowEnd).TitleContains(marker).CountAsync(ctx.CancellationToken);
                    Assert.Equal(0, elsewhere);
                }
            }
            finally
            {
                foreach (var id in ids)
                    await store.DeleteEvent(id, ct: ctx.CancellationToken);
            }
        });

        this.Test("Event_DefaultCalendarFallback", async ctx =>
        {
            var start = RoundToSecond(DateTimeOffset.Now.AddDays(7));
            var id = await store.CreateEvent(new CalendarEvent(NewMarker(), start, start.AddHours(1)), ctx.CancellationToken);
            try
            {
                var read = await store.GetEvent(id, ctx.CancellationToken);
                Assert.NotNull(read);
                Assert.False(String.IsNullOrWhiteSpace(read.CalendarId));
                ctx.Log($"Landed in calendar {read.CalendarId}");
            }
            finally
            {
                await store.DeleteEvent(id, ct: ctx.CancellationToken);
            }
        });

        this.Test("Event_DeleteMissingThrows", async ctx =>
        {
            var start = RoundToSecond(DateTimeOffset.Now.AddDays(8));
            var id = await store.CreateEvent(new CalendarEvent(NewMarker(), start, start.AddHours(1)) { CalendarId = this.calendarId }, ctx.CancellationToken);
            await store.DeleteEvent(id, ct: ctx.CancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.DeleteEvent(id, ct: ctx.CancellationToken));
        });
    }


    // removes every calendar and event an earlier run left behind
    async Task SweepAsync(TestContext context)
    {
        var ct = context.CancellationToken;
        var leftovers = await store
            .Query()
            .Between(DateTimeOffset.Now.AddDays(-30), DateTimeOffset.Now.AddDays(60))
            .TitleContains(Prefix)
            .ToListAsync(ct);

        foreach (var evt in leftovers.Where(x => x.Id != null))
        {
            try
            {
                await store.DeleteEvent(evt.Id!, true, ct);
            }
            catch (Exception ex)
            {
                context.Log($"Sweep could not delete event {evt.Title}: {ex.Message}");
            }
        }

        var calendars = (await store.GetAll(ct)).Where(x => x.Id != null && x.Name.StartsWith(Prefix, StringComparison.Ordinal)).ToList();
        foreach (var calendar in calendars)
        {
            try
            {
                await store.Delete(calendar.Id!, ct);
            }
            catch (Exception ex)
            {
                context.Log($"Sweep could not delete calendar {calendar.Name}: {ex.Message}");
            }
        }

        if (leftovers.Count > 0 || calendars.Count > 0)
            context.Log($"Swept {leftovers.Count} event(s) and {calendars.Count} calendar(s)");
    }


    static string NewMarker() => $"{Prefix}-{Guid.NewGuid():N}"[..(Prefix.Length + 9)];

    // stores keep whole seconds at best
    static DateTimeOffset RoundToSecond(DateTimeOffset value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Offset);

    static void AssertSameInstant(DateTimeOffset expected, DateTimeOffset actual)
        => Assert.True(
            Math.Abs((expected - actual).TotalSeconds) < 1,
            $"Expected {expected:o} ({expected.UtcDateTime:o} UTC) but read {actual:o} ({actual.UtcDateTime:o} UTC)"
        );
}
