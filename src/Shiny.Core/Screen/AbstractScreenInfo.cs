using System;
using System.Threading.Tasks;

namespace Shiny;


/// <summary>
/// The bookkeeping behind every <see cref="IScreenInfo"/>: each platform supplies only how to read the display,
/// how to watch it, and how to lock orientation. The watch starts with the first subscriber to either event and
/// stops with the last, so an app that never asks pays nothing.
/// </summary>
public abstract class AbstractScreenInfo : IScreenInfo
{
    readonly object syncLock = new();
    EventHandler<ScreenMetrics>? metricsHandler;
    EventHandler<DeviceOrientation>? orientationHandler;
    IDisposable? watch;
    ScreenMetrics last = ScreenMetrics.Empty;


    public ScreenMetrics Metrics => this.ReadMetrics();
    public DeviceOrientation Orientation => this.Metrics.Orientation;


    public event EventHandler<ScreenMetrics>? MetricsChanged
    {
        add
        {
            lock (this.syncLock)
                this.metricsHandler += value;

            this.UpdateWatch();
        }
        remove
        {
            lock (this.syncLock)
                this.metricsHandler -= value;

            this.UpdateWatch();
        }
    }


    public event EventHandler<DeviceOrientation>? OrientationChanged
    {
        add
        {
            lock (this.syncLock)
                this.orientationHandler += value;

            this.UpdateWatch();
        }
        remove
        {
            lock (this.syncLock)
                this.orientationHandler -= value;

            this.UpdateWatch();
        }
    }


    public Task<bool> SetOrientation(DeviceOrientation orientation) => this.ApplyOrientation(orientation);


    public Task<bool> ResetOrientation() => this.ApplyOrientation(DeviceOrientation.Unknown);


    /// <summary>
    /// Reads the main display now.
    /// </summary>
    protected abstract ScreenMetrics ReadMetrics();


    /// <summary>
    /// Starts watching the display natively, calling <paramref name="refresh"/> whenever it may have changed -
    /// spurious calls are fine, they are compared against the last reading. Disposing stops the watch.
    /// </summary>
    protected abstract IDisposable Watch(Action refresh);


    /// <summary>
    /// Locks to <paramref name="orientation"/>, or clears the lock for <see cref="DeviceOrientation.Unknown"/>.
    /// </summary>
    protected virtual Task<bool> ApplyOrientation(DeviceOrientation orientation) => Task.FromResult(false);


    /// <summary>
    /// Re-reads the display and raises the events for whatever changed.
    /// </summary>
    protected void Refresh()
    {
        var latest = this.ReadMetrics();
        ScreenMetrics previous;
        EventHandler<ScreenMetrics>? metrics;
        EventHandler<DeviceOrientation>? orientation;

        lock (this.syncLock)
        {
            if (latest == this.last)
                return;

            previous = this.last;
            this.last = latest;
            metrics = this.metricsHandler;
            orientation = this.orientationHandler;
        }

        metrics?.Invoke(this, latest);
        if (previous.Orientation != latest.Orientation)
            orientation?.Invoke(this, latest.Orientation);
    }


    // The first reading is taken outside the lock - on Apple it is marshalled to the main thread, which may
    // itself be waiting on the lock inside Refresh.
    void UpdateWatch()
    {
        bool start;
        lock (this.syncLock)
        {
            var anyone = this.metricsHandler != null || this.orientationHandler != null;
            if (!anyone && this.watch != null)
            {
                this.watch.Dispose();
                this.watch = null;
            }
            start = anyone && this.watch == null;
        }
        if (!start)
            return;

        var first = this.ReadMetrics();
        lock (this.syncLock)
        {
            var anyone = this.metricsHandler != null || this.orientationHandler != null;
            if (anyone && this.watch == null)
            {
                this.last = first;
                this.watch = this.Watch(this.Refresh);
            }
        }
    }
}
