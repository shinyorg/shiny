using Shiny.Calendar.Internals;
using Xunit;

namespace Shiny.Calendar.Tests;

public class CalendarHelperTests
{
    [Fact]
    public void OccurrenceId_RoundTrips()
    {
        var start = new DateTimeOffset(2026, 7, 1, 9, 30, 0, TimeSpan.Zero);
        var id = EventOccurrenceId.Encode("ABC:123", start);

        var (eventId, occurrence) = EventOccurrenceId.Decode(id);
        Assert.Equal("ABC:123", eventId);
        Assert.Equal(start, occurrence);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("ABC:DEF")]
    [InlineData("a|b")]
    [InlineData("|123")]
    [InlineData("abc|")]
    public void OccurrenceId_PlainIdsDecodeUnchanged(string id)
    {
        var (eventId, occurrence) = EventOccurrenceId.Decode(id);
        Assert.Equal(id, eventId);
        Assert.Null(occurrence);
    }

    [Theory]
    [InlineData("P3600S", 3600)]
    [InlineData("PT1H", 3600)]
    [InlineData("PT1H30M", 5400)]
    [InlineData("P1D", 86400)]
    [InlineData("P1W", 604800)]
    [InlineData("P1DT2H", 93600)]
    [InlineData("-PT15M", -900)]
    public void Duration_Parses(string value, int seconds)
        => Assert.Equal(TimeSpan.FromSeconds(seconds), RecurrenceDuration.Parse(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("3600")]
    [InlineData("PX")]
    [InlineData("P12")]
    public void Duration_RejectsGarbage(string? value)
        => Assert.Null(RecurrenceDuration.Parse(value));

    [Fact]
    public void Duration_Formats()
    {
        Assert.Equal("P5400S", RecurrenceDuration.ToRfc5545(TimeSpan.FromMinutes(90), false));
        Assert.Equal("P2D", RecurrenceDuration.ToRfc5545(TimeSpan.FromDays(2), true));
        Assert.Equal("P1D", RecurrenceDuration.ToRfc5545(TimeSpan.Zero, true));
    }

    [Fact]
    public void AllDay_EastOfUtc_KeepsTheCalendarDay()
    {
        // local midnight Nov 27 in UTC+10 is Nov 26 14:00Z - it must still be stored as Nov 27
        var offset = TimeSpan.FromHours(10);
        var (start, end) = AllDayDates.ToStorage(
            new DateTimeOffset(2026, 11, 27, 0, 0, 0, offset),
            new DateTimeOffset(2026, 11, 28, 0, 0, 0, offset)
        );
        Assert.Equal(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 11, 28, 0, 0, 0, TimeSpan.Zero), end);
    }

    [Fact]
    public void AllDay_SameDayEnd_BecomesOneDay()
    {
        var offset = TimeSpan.FromHours(-5);
        var (start, end) = AllDayDates.ToStorage(
            new DateTimeOffset(2026, 11, 27, 9, 0, 0, offset),
            new DateTimeOffset(2026, 11, 27, 17, 0, 0, offset)
        );
        Assert.Equal(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 11, 28, 0, 0, 0, TimeSpan.Zero), end);
    }

    [Fact]
    public void AllDay_FromStorage_IsLocalMidnightOfTheSameDay()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("test-5", TimeSpan.FromHours(-5), "test", "test");
        var local = AllDayDates.FromStorage(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero), zone);

        Assert.Equal(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.FromHours(-5)), local);
        // and it round-trips back to the same stored day
        Assert.Equal(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero), AllDayDates.ToStorage(local, local).Start);
    }

    [Theory]
    [InlineData("FREQ=WEEKLY;COUNT=10", false, "FREQ=WEEKLY;UNTIL=20260707T085959Z")]
    [InlineData("FREQ=DAILY;UNTIL=20270101T000000Z;INTERVAL=2", false, "FREQ=DAILY;INTERVAL=2;UNTIL=20260707T085959Z")]
    [InlineData("FREQ=WEEKLY;BYDAY=TU", true, "FREQ=WEEKLY;BYDAY=TU;UNTIL=20260706")]
    public void Rrule_EndBefore(string rrule, bool allDay, string expected)
        => Assert.Equal(expected, RecurrenceRules.EndBefore(rrule, new DateTimeOffset(2026, 7, 7, 9, 0, 0, TimeSpan.Zero), allDay));
}
