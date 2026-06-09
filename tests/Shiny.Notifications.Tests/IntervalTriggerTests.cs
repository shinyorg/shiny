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
        // 2024-06-01 Sat — DayOfWeek=Saturday → 2024-06-08 09:00 (current code never returns "today")
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero));
        var t = new IntervalTrigger
        {
            TimeOfDay = new TimeSpan(9, 0, 0),
            DayOfWeek = DayOfWeek.Saturday,
            TimeProvider = clock
        };

        Assert.Equal(new DateTime(2024, 6, 8, 9, 0, 0, DateTimeKind.Utc), t.CalculateNextAlarm());
    }
}
