using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Shiny.InAppPurchases.Server
{
    /// <summary>
    /// Remembers processed notification ids so store redeliveries are not dispatched twice. The default is in-memory
    /// (per process) - replace it with <c>UseDeduplicator&lt;T&gt;()</c> backed by shared storage when running more than
    /// one instance.
    /// </summary>
    public interface IPurchaseEventDeduplicator
    {
        ValueTask<bool> HasProcessedAsync(StorePlatform platform, string notificationId, CancellationToken cancellationToken);

        /// <summary>Called only after every handler completed successfully</summary>
        ValueTask MarkProcessedAsync(StorePlatform platform, string notificationId, CancellationToken cancellationToken);
    }
}


namespace Shiny.InAppPurchases.Server.Infrastructure
{
    public sealed class InMemoryPurchaseEventDeduplicator(
        IOptions<InAppPurchaseServerOptions> options,
        TimeProvider timeProvider
    ) : IPurchaseEventDeduplicator
    {
        readonly ConcurrentDictionary<string, DateTimeOffset> entries = new(StringComparer.Ordinal);


        public ValueTask<bool> HasProcessedAsync(StorePlatform platform, string notificationId, CancellationToken cancellationToken)
        {
            var key = Key(platform, notificationId);
            if (!this.entries.TryGetValue(key, out var expiresAt))
                return ValueTask.FromResult(false);

            if (expiresAt > timeProvider.GetUtcNow())
                return ValueTask.FromResult(true);

            this.entries.TryRemove(key, out _);
            return ValueTask.FromResult(false);
        }


        public ValueTask MarkProcessedAsync(StorePlatform platform, string notificationId, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow();
            this.entries[Key(platform, notificationId)] = now + options.Value.DeduplicationWindow;

            if (this.entries.Count > options.Value.DeduplicationCapacity)
                this.Prune(now);

            return ValueTask.CompletedTask;
        }


        internal int Count => this.entries.Count;


        void Prune(DateTimeOffset now)
        {
            foreach (var entry in this.entries)
            {
                if (entry.Value <= now)
                    this.entries.TryRemove(entry.Key, out _);
            }

            var over = this.entries.Count - options.Value.DeduplicationCapacity;
            if (over <= 0)
                return;

            foreach (var entry in this.entries.OrderBy(x => x.Value).Take(over).ToList())
                this.entries.TryRemove(entry.Key, out _);
        }


        static string Key(StorePlatform platform, string notificationId) => $"{platform}:{notificationId}";
    }
}
