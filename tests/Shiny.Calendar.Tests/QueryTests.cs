using Shiny.Calendar;
using Xunit;

namespace Shiny.Calendar.Tests;

public class QueryTests
{
    static readonly DateTimeOffset Base = new(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);

    static FakeCalendarStore Store()
    {
        var s = new FakeCalendarStore();
        s.Seed(
            new CalendarEvent("Standup", Base, Base.AddMinutes(30)) { Id = "1", CalendarId = "work" },
            new CalendarEvent("Lunch with Alice", Base.AddHours(3), Base.AddHours(4)) { Id = "2", CalendarId = "personal",
                Attendees = { new EventAttendee("Alice", "alice@example.com") } },
            new CalendarEvent("Sprint Review", Base.AddDays(2), Base.AddDays(2).AddHours(1)) { Id = "3", CalendarId = "work" },
            new CalendarEvent("Dentist", Base.AddDays(40), Base.AddDays(40).AddHours(1)) { Id = "4", CalendarId = "personal" }
        );
        return s;
    }

    [Fact]
    public async Task ForCalendar_Is_Pushed_To_Native_Fetch()
    {
        var store = Store();
        var results = await store.Query().ForCalendar("work").ToListAsync();

        Assert.Equal("work", store.LastDescriptor!.CalendarId);
        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Equal("work", e.CalendarId));
    }

    [Fact]
    public async Task Between_Becomes_Window_Hints()
    {
        var store = Store();
        var lower = Base.AddHours(-1);
        var upper = Base.AddDays(3);

        var results = await store.Query().Between(lower, upper).ToListAsync();

        Assert.Equal(lower, store.LastDescriptor!.StartAfter);
        Assert.Equal(upper, store.LastDescriptor!.EndBefore);
        // Standup, Lunch, Sprint Review are inside the window; Dentist (day 40) is not.
        Assert.Equal(3, results.Count);
        Assert.DoesNotContain(results, e => e.Title == "Dentist");
    }

    [Fact]
    public void Between_Rejects_Inverted_Window()
    {
        var store = Store();
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Query().Between(Base.AddDays(1), Base));
    }

    [Fact]
    public async Task TitleContains_Runs_InMemory_And_Filters_Exactly()
    {
        var store = Store();
        var results = await store.Query().TitleContains("sprint").ToListAsync();

        // Title is not a native hint - no calendar/window pushed down.
        Assert.Null(store.LastDescriptor!.CalendarId);
        Assert.Null(store.LastDescriptor.StartAfter);
        Assert.Single(results);
        Assert.Equal("Sprint Review", results[0].Title);
    }

    [Fact]
    public async Task Where_Predicate_Runs_InMemory()
    {
        var store = Store();
        var results = await store
            .Query()
            .Where(e => e.Attendees.Any(a => a.Email == "alice@example.com"))
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("Lunch with Alice", results[0].Title);
    }

    [Fact]
    public async Task Native_Hint_And_InMemory_Predicate_Combine()
    {
        var store = Store();
        var results = await store
            .Query()
            .ForCalendar("work")
            .TitleContains("Standup")
            .ToListAsync();

        Assert.Equal("work", store.LastDescriptor!.CalendarId);
        Assert.Single(results);
        Assert.Equal("Standup", results[0].Title);
    }

    [Fact]
    public async Task Multiple_Where_Predicates_Are_Anded()
    {
        var store = Store();
        var results = await store
            .Query()
            .Where(e => e.CalendarId == "personal")
            .Where(e => e.Title.StartsWith("Lunch"))
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("Lunch with Alice", results[0].Title);
    }

    [Fact]
    public async Task Skip_And_Take_Are_Applied()
    {
        var store = Store();
        var results = await store
            .Query()
            .ForCalendar("work")
            .OrderBy(CalendarEventSortField.Start)
            .Skip(1)
            .Take(1)
            .ToListAsync();

        Assert.Equal(1, store.LastDescriptor!.Skip);
        Assert.Equal(1, store.LastDescriptor.Take);
        Assert.Single(results);
        Assert.Equal("Sprint Review", results[0].Title);
    }

    [Fact]
    public async Task OrderBy_Descending_Sorts_Results()
    {
        var store = Store();
        var results = await store
            .Query()
            .OrderBy(CalendarEventSortField.Start, descending: true)
            .ToListAsync();

        Assert.Equal("Dentist", results[0].Title);
        Assert.Equal("Standup", results[^1].Title);
    }

    [Fact]
    public void ThenBy_Without_OrderBy_Throws()
    {
        var store = Store();
        Assert.Throws<InvalidOperationException>(() => store.Query().ThenBy(CalendarEventSortField.Title));
    }

    [Fact]
    public async Task CountAsync_Works()
    {
        var store = Store();
        var count = await store.Query().ForCalendar("personal").CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_Returns_Null_When_Empty()
    {
        var store = Store();
        var result = await store.Query().ForCalendar("does-not-exist").FirstOrDefaultAsync();
        Assert.Null(result);
    }
}
