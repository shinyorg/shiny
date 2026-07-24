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
    public void CalendarId_Is_Extracted_As_Native_Hint()
    {
        var store = Store();
        var results = store.Query().Where(e => e.CalendarId == "work").ToList();

        Assert.Equal("work", store.LastDescriptor!.CalendarId);
        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Equal("work", e.CalendarId));
    }

    [Fact]
    public void Start_And_End_Bounds_Become_Window_Hints()
    {
        var store = Store();
        var lower = Base.AddHours(-1);
        var upper = Base.AddDays(3);

        var results = store.Query()
            .Where(e => e.Start >= lower && e.End <= upper)
            .ToList();

        Assert.Equal(lower, store.LastDescriptor!.StartAfter);
        Assert.Equal(upper, store.LastDescriptor!.EndBefore);
        // Standup, Lunch, Sprint Review are inside the window; Dentist (day 40) is not.
        Assert.Equal(3, results.Count);
        Assert.DoesNotContain(results, e => e.Title == "Dentist");
    }

    [Fact]
    public void Title_Contains_Runs_InMemory_And_Filters_Exactly()
    {
        var store = Store();
        var results = store.Query().Where(e => e.Title.Contains("Sprint")).ToList();

        // Title is not a native hint - no calendar/window extracted.
        Assert.Null(store.LastDescriptor!.CalendarId);
        Assert.Null(store.LastDescriptor.StartAfter);
        Assert.Single(results);
        Assert.Equal("Sprint Review", results[0].Title);
    }

    [Fact]
    public void Attendee_Any_Predicate_Runs_InMemory()
    {
        var store = Store();
        var results = store.Query()
            .Where(e => e.Attendees.Any(a => a.Email == "alice@example.com"))
            .ToList();

        Assert.Single(results);
        Assert.Equal("Lunch with Alice", results[0].Title);
    }

    [Fact]
    public void Combined_Native_Hint_And_InMemory_Predicate()
    {
        var store = Store();
        var results = store.Query()
            .Where(e => e.CalendarId == "work" && e.Title.Contains("Standup"))
            .ToList();

        Assert.Equal("work", store.LastDescriptor!.CalendarId);
        Assert.Single(results);
        Assert.Equal("Standup", results[0].Title);
    }

    [Fact]
    public void Skip_And_Take_Are_Applied()
    {
        var store = Store();
        var results = store.Query()
            .Where(e => e.CalendarId == "work")
            .Skip(1)
            .Take(1)
            .ToList();

        Assert.Equal(1, store.LastDescriptor!.Skip);
        Assert.Equal(1, store.LastDescriptor.Take);
        Assert.Single(results);
    }

    [Fact]
    public void Count_Operation_Works()
    {
        var store = Store();
        var count = store.Query().Where(e => e.CalendarId == "personal").Count();
        Assert.Equal(2, count);
    }

    [Fact]
    public void Captured_Variable_Value_Is_Resolved()
    {
        var store = Store();
        var wanted = "work";
        var results = store.Query().Where(e => e.CalendarId == wanted).ToList();

        Assert.Equal("work", store.LastDescriptor!.CalendarId);
        Assert.Equal(2, results.Count);
    }
}
