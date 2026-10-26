using System;
using System.Collections.Generic;

namespace Shiny;


/// <summary>
/// The bookkeeping behind every <see cref="IScreenPosture"/>: each platform supplies how to read the posture and how to
/// watch it. The watch starts with the first subscriber and stops with the last, and a reading equal to the last one
/// raises nothing.
/// </summary>
public abstract class AbstractScreenPosture : IScreenPosture
{
    readonly object syncLock = new();
    EventHandler<ScreenPosture>? handler;
    IDisposable? watch;
    ScreenPosture last = ScreenPosture.None;


    public abstract bool IsSupported { get; }
    public virtual IReadOnlyList<FoldOrientation> SupportedPostures => [];
    public ScreenPosture Current => this.IsSupported ? this.ReadPosture() : ScreenPosture.None;


    public event EventHandler<ScreenPosture>? Changed
    {
        add
        {
            lock (this.syncLock)
                this.handler += value;

            this.UpdateWatch();
        }
        remove
        {
            lock (this.syncLock)
                this.handler -= value;

            this.UpdateWatch();
        }
    }


    /// <summary>
    /// Reads the posture now. Only called while <see cref="IsSupported"/> is true.
    /// </summary>
    protected abstract ScreenPosture ReadPosture();


    /// <summary>
    /// Starts watching natively, calling <paramref name="refresh"/> whenever the posture may have changed - spurious
    /// calls are fine, they are compared against the last reading. Disposing stops the watch.
    /// </summary>
    protected abstract IDisposable Watch(Action refresh);


    /// <summary>
    /// Re-reads the posture and raises <see cref="Changed"/> if it differs from the last reading.
    /// </summary>
    protected void Refresh()
    {
        var latest = this.Current;
        EventHandler<ScreenPosture>? changed;

        lock (this.syncLock)
        {
            if (this.watch == null || latest.Equals(this.last))
                return;

            this.last = latest;
            changed = this.handler;
        }
        changed?.Invoke(this, latest);
    }


    // The first reading is taken outside the lock - on Apple it is marshalled to the main thread, which may itself be
    // waiting on the lock inside Refresh.
    void UpdateWatch()
    {
        bool start;
        lock (this.syncLock)
        {
            var anyone = this.handler != null;
            if (!anyone && this.watch != null)
            {
                this.watch.Dispose();
                this.watch = null;
            }
            start = anyone && this.watch == null;
        }
        if (!start)
            return;

        // watched even while unsupported: the browser only knows once its JS module has loaded
        var first = this.Current;
        lock (this.syncLock)
        {
            if (this.handler != null && this.watch == null)
            {
                this.last = first;
                this.watch = this.Watch(this.Refresh);
            }
        }
    }
}
