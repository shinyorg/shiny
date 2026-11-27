using System;
using Microsoft.Extensions.Time.Testing;
using Shiny.Notifications;
using Xunit;

namespace Shiny.Notifications.Tests;


public class IntervalTriggerTests
{
    // -------- AssertValid --------

    [Fact]
    public void Assert_BothNull_Throws()
    {
        var t = new IntervalTrigger();
        Assert.Throws<InvalidOperationException>(() => t.AssertValid());
    }


    [Fact]
    public void Assert_BothSet_Throws()
    {
        var t = new IntervalTrigger
        {
            Interval = TimeSpan.FromMinutes(5),
            TimeOfDay = TimeSpan.FromHours(8)
        };
        Assert.Throws<InvalidOperationException>(() => t.AssertValid());
    }


    [Fact]
    public void Assert_OnlyInterval_Passes()
    {
        // Was a latent NRE before the fix — Interval-only triggers used to crash here.
        var t = new IntervalTrigger { Interval = TimeSpan.FromHours(1) };
        t.AssertValid();
    }


    [Fact]
    public void Assert_OnlyTimeOfDay_Passes()
    {
        var t = new IntervalTrigger { TimeOfDay = TimeSpan.FromHours(8) };
        t.AssertValid();
    }


    [Fact]
    public void Assert_TimeOfDayBeyond24h_Throws()
    {
        var t = new IntervalTrigger { TimeOfDay = TimeSpan.FromHours(25) };
        Assert.Throws<InvalidOperationException>(() => t.AssertValid());
    }


    // -------- CalculateNextAlarm: Interval mode --------

    [Fact]
    public void Calc_Interval_AddsToNow()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero));
        var t = new IntervalTrigger
        {
            Interval = TimeSpan.FromMinutes(30),
            TimeProvider = clock
        };

        Assert.Equal(new DateTime(2024, 6, 1, 10, 30, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }


    // -------- CalculateNextAlarm: TimeOfDay mode --------

    [Fact]
    public void Calc_TimeOfDay_ReturnsTomorrowAtTime()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero));
        var t = new IntervalTrigger
        {
            TimeOfDay = new TimeSpan(8, 0, 0),
            TimeProvider = clock
        };

        Assert.Equal(new DateTime(2024, 6, 2, 8, 0, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }


    [Fact]
    public void Calc_TimeOfDay_AtMonthEnd_DoesNotThrow()
    {
        // Was a latent ArgumentOutOfRangeException before the fix — Day+1 = 32 on Jan 31.
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 1, 31, 23, 30, 0, TimeSpan.Zero));
        var t = new IntervalTrigger
        {
            TimeOfDay = new TimeSpan(6, 0, 0),
            TimeProvider = clock
        };

        var next = t.CalculateNextAlarm();
        Assert.Equal(new DateTime(2024, 2, 1, 6, 0, 0, DateTimeKind.Utc), next);
    }


    [Fact]
    public void Calc_TimeOfDay_AtYearEnd_RollsToNextYear()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 12, 31, 22, 0, 0, TimeSpan.Zero));
        var t = new IntervalTrigger
        {
            TimeOfDay = new TimeSpan(9, 0, 0),
            TimeProvider = clock
        };

        Assert.Equal(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }


    // -------- CalculateNextAlarm: DayOfWeek --------

    [Fact]
    public void Calc_DayOfWeek_AdvancesToMatchingDay()
    {
        // 2024-06-01 is a Saturday — tomorrow=Sunday, the next Friday lands 2024-06-07.
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero));
        var t = new IntervalTrigger
        {
            TimeOfDay = new TimeSpan(7, 30, 0),
            DayOfWeek = DayOfWeek.Friday,
            TimeProvider = clock
        };

        Assert.Equal(new DateTime(2024, 6, 7, 7, 30, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }


    [Fact]
    public void Calc_DayOfWeek_WhenTomorrowMatches_PicksTomorrow()
    {
        // 2024-06-01 Sat — DayOfWeek=Sunday → 2024-06-02 09:00
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero));
        var t = new IntervalTrigger
        {
            TimeOfDay = new TimeSpan(9, 0, 0),
            DayOfWeek = DayOfWeek.Sunday,
            TimeProvider = clock
        };

        Assert.Equal(new DateTime(2024, 6, 2, 9, 0, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }


    [Fact]
    public void Calc_DayOfWeek_WhenTodayMatchesButTomorrowDoesnt_AdvancesFullWeek()
    {
        // 2024-06-01 Sat 10:00 — DayOfWeek=Saturday 09:00 has passed today → 2024-06-08 09:00
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero));
        var t = new IntervalTrigger
        {
            TimeOfDay = new TimeSpan(9, 0, 0),
            DayOfWeek = DayOfWeek.Saturday,
            TimeProvider = clock
        };

        Assert.Equal(new DateTime(2024, 6, 8, 9, 0, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }


    // -------- CalculateNextAlarm: local time zone --------

    static FakeTimeProvider Clock(DateTimeOffset utcNow, string tzId)
    {
        var clock = new FakeTimeProvider(utcNow);
        clock.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(tzId));
        return clock;
    }


    [Fact]
    public void Calc_TimeOfDay_LaterToday_FiresToday()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 7, 0, 0, TimeSpan.Zero));
        var t = new IntervalTrigger { TimeOfDay = new TimeSpan(9, 0, 0), TimeProvider = clock };

        Assert.Equal(new DateTime(2024, 6, 1, 9, 0, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }


    [Fact]
    public void Calc_TimeOfDay_IsLocalTime_NotUtc()
    {
        // 2024-06-01 06:00 New York (EDT, UTC-4) - 09:00 local is 13:00 UTC, not 09:00 UTC
        var clock = Clock(new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero), "America/New_York");
        var t = new IntervalTrigger { TimeOfDay = new TimeSpan(9, 0, 0), TimeProvider = clock };

        var next = t.CalculateNextAlarm();
        Assert.Equal(DateTimeKind.Utc, next.Kind);
        Assert.Equal(new DateTime(2024, 6, 1, 13, 0, 0, DateTimeKind.Utc), next);
    }


    [Fact]
    public void Calc_DayOfWeek_UsesLocalDay()
    {
        // 2024-06-08 02:00 UTC is still Friday 2024-06-07 22:00 in New York - Friday 23:00 local is later tonight
        var clock = Clock(new DateTimeOffset(2024, 6, 8, 2, 0, 0, TimeSpan.Zero), "America/New_York");
        var t = new IntervalTrigger
        {
            TimeOfDay = new TimeSpan(23, 0, 0),
            DayOfWeek = DayOfWeek.Friday,
            TimeProvider = clock
        };

        Assert.Equal(new DateTime(2024, 6, 8, 3, 0, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }


    [Fact]
    public void Calc_TimeOfDay_AcrossDstChange_KeepsLocalTime()
    {
        // 2024-03-09 12:00 New York (EST) - the next 09:00 is 2024-03-10, after clocks spring forward to EDT (UTC-4)
        var clock = Clock(new DateTimeOffset(2024, 3, 9, 17, 0, 0, TimeSpan.Zero), "America/New_York");
        var t = new IntervalTrigger { TimeOfDay = new TimeSpan(9, 0, 0), TimeProvider = clock };

        Assert.Equal(new DateTime(2024, 3, 10, 13, 0, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }


    [Fact]
    public void Calc_After_ReturnsFollowingOccurrences()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero));
        var t = new IntervalTrigger { TimeOfDay = new TimeSpan(8, 0, 0), TimeProvider = clock };

        var first = t.CalculateNextAlarm(clock.GetUtcNow());
        var second = t.CalculateNextAlarm(first);
        Assert.Equal(new DateTimeOffset(2024, 6, 2, 8, 0, 0, TimeSpan.Zero), first);
        Assert.Equal(new DateTimeOffset(2024, 6, 3, 8, 0, 0, TimeSpan.Zero), second);
    }
}
