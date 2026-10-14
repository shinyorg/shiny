namespace Shiny.DeviceTests.Framework;


/// <summary>
/// Shiny delivers most results through DI-created delegates (IGpsDelegate, IGeofenceDelegate, ...). The test
/// delegates publish what they receive here, and tests wait on a topic. Entries are kept until cleared, so an
/// event that lands before the test starts waiting is still seen.
/// </summary>
public sealed class EventHub
{
    readonly record struct Entry(string Topic, object Payload);

    readonly List<Entry> entries = [];
    readonly List<(string Topic, Func<object, bool> Match, TaskCompletionSource<object> Tcs)> waiters = [];


    public void Publish(string topic, object payload)
    {
        List<TaskCompletionSource<object>> completed = [];
        lock (this.entries)
        {
            this.entries.Add(new Entry(topic, payload));
            foreach (var waiter in this.waiters.Where(x => x.Topic == topic && x.Match(payload)).ToList())
            {
                this.waiters.Remove(waiter);
                completed.Add(waiter.Tcs);
            }
        }
        foreach (var tcs in completed)
            tcs.TrySetResult(payload);
    }


    public void Clear(string? topic = null)
    {
        lock (this.entries)
            this.entries.RemoveAll(x => topic == null || x.Topic == topic);
    }


    public IReadOnlyList<T> Get<T>(string topic)
    {
        lock (this.entries)
            return this.entries.Where(x => x.Topic == topic).Select(x => x.Payload).OfType<T>().ToList();
    }


    public async Task<T> WaitAsync<T>(string topic, TimeSpan timeout, CancellationToken cancellationToken, Func<T, bool>? predicate = null)
    {
        Func<object, bool> match = x => x is T t && (predicate?.Invoke(t) ?? true);
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (this.entries)
        {
            var existing = this.entries.FirstOrDefault(x => x.Topic == topic && match(x.Payload));
            if (existing.Topic != null)
                return (T)existing.Payload;

            this.waiters.Add((topic, match, tcs));
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        await using var reg = cts.Token.Register(() => tcs.TrySetCanceled());
        try
        {
            return (T)await tcs.Task.ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            lock (this.entries)
                this.waiters.RemoveAll(x => x.Tcs == tcs);

            throw new TimeoutException($"No '{topic}' event ({typeof(T).Name}) within {timeout.TotalSeconds:0.#}s");
        }
    }
}
