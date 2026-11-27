namespace Shiny.Net.Discovery.Internals;


/// <summary>What a caller got from <see cref="LegacyResolveSlot.AcquireAsync"/>.</summary>
enum LegacyResolveSlotOutcome
{
    /// <summary>The slot is the caller's - start the native resolve and finish the ticket when it reports back.</summary>
    Acquired,

    /// <summary>
    /// The slot is held by a resolve that already timed out and has not reported back. It may never
    /// report back, so waiting for it is pointless - resolve some other way.
    /// </summary>
    Orphaned,

    /// <summary>The caller's own window closed while a live resolve held the slot.</summary>
    TimedOut
}


/// <summary>
/// The single resolve NsdManager allows in flight before API 34.
/// </summary>
/// <remarks>
/// <para>The legacy NsdService answers a second resolve with FAILURE_ALREADY_ACTIVE while one is
/// outstanding, and nothing below API 34 can cancel one - so the slot is only free again once the
/// outstanding resolve's listener reports back. A caller's timeout does not free it: the ticket is
/// marked orphaned instead, and the slot frees itself if that listener ever fires.</para>
/// <para>Callers queue behind a live resolve with their own timeout. While the holder is orphaned
/// they are told so straight away rather than waiting on a listener that may never fire (a service
/// that disappeared mid-resolve leaves mdnsd asking forever).</para>
/// </remarks>
sealed class LegacyResolveSlot(TimeProvider? timeProvider = null)
{
    readonly Lock sync = new();
    readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    Ticket? holder;
    bool holderOrphaned;
    TaskCompletionSource changed = NewSignal();


    /// <summary>True while a resolve that timed out still holds the slot.</summary>
    public bool IsOrphaned
    {
        get
        {
            lock (this.sync)
                return this.holder != null && this.holderOrphaned;
        }
    }


    /// <summary>True while any resolve - live or orphaned - holds the slot.</summary>
    public bool IsBusy
    {
        get
        {
            lock (this.sync)
                return this.holder != null;
        }
    }


    /// <summary>
    /// Takes the slot, waiting up to <paramref name="timeout"/> behind a live resolve. Returns
    /// straight away when the holder is orphaned, unless <paramref name="waitBehindOrphan"/> is set
    /// (there is no other way to resolve, so the caller waits in case the orphan reports back).
    /// </summary>
    public async Task<(LegacyResolveSlotOutcome Outcome, Ticket? Ticket)> AcquireAsync(
        TimeSpan timeout,
        CancellationToken ct,
        bool waitBehindOrphan = false
    )
    {
        var deadline = this.clock.GetUtcNow() + timeout;
        while (true)
        {
            Task signal;
            lock (this.sync)
            {
                if (this.holder == null)
                {
                    this.holder = new Ticket(this);
                    this.holderOrphaned = false;
                    return (LegacyResolveSlotOutcome.Acquired, this.holder);
                }

                if (this.holderOrphaned && !waitBehindOrphan)
                    return (LegacyResolveSlotOutcome.Orphaned, null);

                signal = this.changed.Task;
            }

            var remaining = deadline - this.clock.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
                return (LegacyResolveSlotOutcome.TimedOut, null);

            try
            {
                await signal.WaitAsync(remaining, this.clock, ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return this.IsOrphaned && !waitBehindOrphan
                    ? (LegacyResolveSlotOutcome.Orphaned, null)
                    : (LegacyResolveSlotOutcome.TimedOut, null);
            }
        }
    }


    void Complete(Ticket ticket)
    {
        lock (this.sync)
        {
            if (!ReferenceEquals(this.holder, ticket))
                return;

            this.holder = null;
            this.holderOrphaned = false;
            this.Signal();
        }
    }


    void Orphan(Ticket ticket)
    {
        lock (this.sync)
        {
            if (!ReferenceEquals(this.holder, ticket) || this.holderOrphaned)
                return;

            this.holderOrphaned = true;
            this.Signal();
        }
    }


    // callers hold sync
    void Signal()
    {
        var previous = this.changed;
        this.changed = NewSignal();
        previous.TrySetResult();
    }


    static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);


    /// <summary>One native resolve's hold on the slot.</summary>
    public sealed class Ticket
    {
        readonly LegacyResolveSlot slot;

        internal Ticket(LegacyResolveSlot slot) => this.slot = slot;

        /// <summary>
        /// The native resolve reported back (resolved or failed), or never started - the slot is free.
        /// Safe to call more than once.
        /// </summary>
        public void Complete() => this.slot.Complete(this);

        /// <summary>
        /// The caller stopped waiting but the native resolve is still outstanding. The slot stays
        /// taken until <see cref="Complete"/>.
        /// </summary>
        public void Orphan() => this.slot.Orphan(this);
    }
}
