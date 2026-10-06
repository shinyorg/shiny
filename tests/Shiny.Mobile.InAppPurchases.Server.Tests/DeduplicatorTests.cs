using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shiny.InAppPurchases.Server.Infrastructure;

namespace Shiny.InAppPurchases.Server.Tests;


public class DeduplicatorTests
{
    [Fact]
    public async Task RemembersUntilWindowExpires()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var dedup = new InMemoryPurchaseEventDeduplicator(Options.Create(new InAppPurchaseServerOptions { DeduplicationWindow = TimeSpan.FromHours(1) }), time);

        Assert.False(await dedup.HasProcessedAsync(StorePlatform.AppStore, "n1", default));
        await dedup.MarkProcessedAsync(StorePlatform.AppStore, "n1", default);
        Assert.True(await dedup.HasProcessedAsync(StorePlatform.AppStore, "n1", default));

        // ids are scoped per platform
        Assert.False(await dedup.HasProcessedAsync(StorePlatform.GooglePlay, "n1", default));

        time.Advance(TimeSpan.FromHours(2));
        Assert.False(await dedup.HasProcessedAsync(StorePlatform.AppStore, "n1", default));
    }


    [Fact]
    public async Task BoundedByCapacity_EvictsOldest()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var dedup = new InMemoryPurchaseEventDeduplicator(Options.Create(new InAppPurchaseServerOptions { DeduplicationCapacity = 3 }), time);

        for (var i = 0; i < 5; i++)
        {
            await dedup.MarkProcessedAsync(StorePlatform.GooglePlay, $"m{i}", default);
            time.Advance(TimeSpan.FromSeconds(1));
        }

        Assert.Equal(3, dedup.Count);
        Assert.False(await dedup.HasProcessedAsync(StorePlatform.GooglePlay, "m0", default));
        Assert.True(await dedup.HasProcessedAsync(StorePlatform.GooglePlay, "m4", default));
    }
}
