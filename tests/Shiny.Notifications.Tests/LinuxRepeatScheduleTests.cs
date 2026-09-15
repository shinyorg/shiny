using System;
using Microsoft.Extensions.Time.Testing;
using Shiny.Notifications;
using Xunit;

namespace Shiny.Notifications.Tests;


public class LinuxRepeatScheduleTests
{
    static FakeTimeProvider Clock(DateTimeOffset utcNow, TimeZoneInfo? tz = null)
    {
        var clock = new FakeTimeProvider(utcNow);
        clock.SetLocalTimeZone(tz ?? TimeZoneInfo.Utc);
        return clock;
    }


    [Fact]
    public void Interval_BecomesDue_OnceIntervalElapses()
    {
        // The 5.6.3 bug: the scheduler recalculated "now + interval" on every tick, so it never came due.
        var clock = Clock(new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero));
        var trigger = new IntervalTrigger { Interval = TimeSpan.FromMinutes(5), TimeProvider = clock };

        var due = NotificationManager.CalculateNextFire(trigger);
        Assert.True(due > clock.GetUtcNow());

        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.True(due <= clock.GetUtcNow());

        var following = NotificationManager.CalculateNextFire(trigger);
        Assert.Equal(due.AddMinutes(5), following);
    }


    [Fact]
    public void TimeOfDay_LaterToday_FiresToday()
    {
        var clock = Clock(new DateTimeOffset(2026, 9, 15, 7, 0, 0, TimeSpan.Zero));
        var trigger = new IntervalTrigger { TimeOfDay = new TimeSpan(9, 0, 0), TimeProvider = clock };

        Assert.Equal(new DateTimeOffset(2026, 9, 15, 9, 0, 0, TimeSpan.Zero), NotificationManager.CalculateNextFire(trigger));
    }


    [Fact]
    public void TimeOfDay_AlreadyPassed_FiresTomorrow()
    {
        var clock = Clock(new DateTimeOffset(2026, 9, 15, 9, 0, 0, TimeSpan.Zero));
        var trigger = new IntervalTrigger { TimeOfDay = new TimeSpan(9, 0, 0), TimeProvider = clock };

        Assert.Equal(new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero), NotificationManager.CalculateNextFire(trigger));
    }


    [Fact]
    public void TimeOfDay_IsLocalTime()
    {
        // 2026-09-15 12:00 UTC is 08:00 in UTC-4 - a 09:00 trigger fires at 13:00 UTC, not 09:00 UTC
        var tz = TimeZoneInfo.CreateCustomTimeZone("test-4", TimeSpan.FromHours(-4), "test-4", "test-4");
        var clock = Clock(new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero), tz);
        var trigger = new IntervalTrigger { TimeOfDay = new TimeSpan(9, 0, 0), TimeProvider = clock };

        var next = NotificationManager.CalculateNextFire(trigger);
        Assert.Equal(new DateTimeOffset(2026, 9, 15, 13, 0, 0, TimeSpan.Zero), next.ToUniversalTime());
    }


    [Fact]
    public void DayOfWeek_Today_BeforeTime_FiresToday()
    {
        // 2026-09-15 is a Tuesday
        var clock = Clock(new DateTimeOffset(2026, 9, 15, 7, 0, 0, TimeSpan.Zero));
        var trigger = new IntervalTrigger
        {
            DayOfWeek = DayOfWeek.Tuesday,
            TimeOfDay = new TimeSpan(8, 0, 0),
            TimeProvider = clock
        };

        Assert.Equal(new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero), NotificationManager.CalculateNextFire(trigger));
    }


    [Fact]
    public void DayOfWeek_Today_AfterTime_FiresNextWeek()
    {
        var clock = Clock(new DateTimeOffset(2026, 9, 15, 8, 30, 0, TimeSpan.Zero));
        var trigger = new IntervalTrigger
        {
            DayOfWeek = DayOfWeek.Tuesday,
            TimeOfDay = new TimeSpan(8, 0, 0),
            TimeProvider = clock
        };

        Assert.Equal(new DateTimeOffset(2026, 9, 22, 8, 0, 0, TimeSpan.Zero), NotificationManager.CalculateNextFire(trigger));
    }


    [Fact]
    public void DayOfWeek_AdvancesToMatchingDay()
    {
        var clock = Clock(new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero));
        var trigger = new IntervalTrigger
        {
            DayOfWeek = DayOfWeek.Friday,
            TimeOfDay = new TimeSpan(7, 30, 0),
            TimeProvider = clock
        };

        Assert.Equal(new DateTimeOffset(2026, 9, 18, 7, 30, 0, TimeSpan.Zero), NotificationManager.CalculateNextFire(trigger));
    }
}
