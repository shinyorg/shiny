using System;
using System.Globalization;
using System.Threading;

namespace Shiny;


/// <summary>
/// The bookkeeping behind <see cref="IPlatform.CultureChanged"/> and <see cref="IPlatform.TimeZoneChanged"/>,
/// shared by every platform: each one supplies only how to watch for a change natively. The watch starts
/// with the first subscriber and stops with the last, so an app that never asks pays nothing.
/// </summary>
sealed class GlobalizationMonitor(
    object sender,
    Func<Action, IDisposable> watchCulture,
    Func<Action, IDisposable> watchTimeZone
)
{
    /// <summary>For platforms with no change notification - the slow poll the change events fall back on.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    readonly object syncLock = new();
    EventHandler<CultureInfo>? cultureHandler;
    EventHandler<TimeZoneInfo>? timeZoneHandler;
    IDisposable? cultureWatch;
    IDisposable? timeZoneWatch;
    CultureInfo lastCulture = CultureInfo.CurrentCulture;
    TimeZoneInfo lastTimeZone = TimeZoneInfo.Local;


    public static IDisposable Poll(Action refresh)
        => new Timer(_ => refresh(), null, PollInterval, PollInterval);


    public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;
    public TimeZoneInfo CurrentTimeZone => TimeZoneInfo.Local;


    public void AddCultureHandler(EventHandler<CultureInfo>? handler)
    {
        lock (this.syncLock)
        {
            var wasEmpty = this.cultureHandler == null;
            this.cultureHandler += handler;
            if (wasEmpty && this.cultureHandler != null)
            {
                this.lastCulture = CultureInfo.CurrentCulture;
                this.cultureWatch = watchCulture(this.RefreshCulture);
            }
        }
    }


    public void RemoveCultureHandler(EventHandler<CultureInfo>? handler)
    {
        lock (this.syncLock)
        {
            this.cultureHandler -= handler;
            if (this.cultureHandler == null)
            {
                this.cultureWatch?.Dispose();
                this.cultureWatch = null;
            }
        }
    }


    public void AddTimeZoneHandler(EventHandler<TimeZoneInfo>? handler)
    {
        lock (this.syncLock)
        {
            var wasEmpty = this.timeZoneHandler == null;
            this.timeZoneHandler += handler;
            if (wasEmpty && this.timeZoneHandler != null)
            {
                this.lastTimeZone = TimeZoneInfo.Local;
                this.timeZoneWatch = watchTimeZone(this.RefreshTimeZone);
            }
        }
    }


    public void RemoveTimeZoneHandler(EventHandler<TimeZoneInfo>? handler)
    {
        lock (this.syncLock)
        {
            this.timeZoneHandler -= handler;
            if (this.timeZoneHandler == null)
            {
                this.timeZoneWatch?.Dispose();
                this.timeZoneWatch = null;
            }
        }
    }


    void RefreshCulture()
    {
        // .NET caches the culture it read from the OS - drop it so the new one is read
        CultureInfo.CurrentCulture.ClearCachedData();
        var latest = CultureInfo.CurrentCulture;

        EventHandler<CultureInfo>? handler;
        lock (this.syncLock)
        {
            if (latest.Equals(this.lastCulture))
                return;

            this.lastCulture = latest;
            handler = this.cultureHandler;
        }
        handler?.Invoke(sender, latest);
    }


    void RefreshTimeZone()
    {
        TimeZoneInfo.ClearCachedData();
        var latest = TimeZoneInfo.Local;

        EventHandler<TimeZoneInfo>? handler;
        lock (this.syncLock)
        {
            if (latest.Equals(this.lastTimeZone))
                return;

            this.lastTimeZone = latest;
            handler = this.timeZoneHandler;
        }
        handler?.Invoke(sender, latest);
    }
}


sealed class ActionDisposable(Action dispose) : IDisposable
{
    int disposed;
    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) == 0)
            dispose();
    }
}


static class PlatformVersionParser
{
    /// <summary>
    /// Reads the leading numeric part of a version string ("17.4.1", "14", "2.3.0-beta (45)") and pads it
    /// to at least major.minor, which is all <see cref="Version"/> will parse.
    /// </summary>
    public static Version? Parse(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
            return null;

        var end = 0;
        while (end < value.Length && (Char.IsDigit(value[end]) || value[end] == '.'))
            end++;

        var numeric = value[..end].Trim('.');
        if (numeric.Length == 0)
            return null;

        if (!numeric.Contains('.'))
            numeric += ".0";

        return Version.TryParse(numeric, out var version) ? version : null;
    }
}
