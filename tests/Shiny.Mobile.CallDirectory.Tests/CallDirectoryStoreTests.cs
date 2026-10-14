using Shiny.CallDirectory.Infrastructure;

namespace Shiny.CallDirectory.Tests;


public class CallDirectoryStoreTests : IDisposable
{
    readonly string dir = Directory.CreateTempSubdirectory("shinycd").FullName;
    readonly CallDirectoryStore store;

    public CallDirectoryStoreTests() => this.store = new(Path.Combine(this.dir, "store.db"));
    public void Dispose() => Directory.Delete(this.dir, true);


    [MacFact]
    public async Task SetGetRemove()
    {
        await this.store.Set([new(18005550100, "Support"), new(18005550101, null, CallDirectoryEntryType.Block)], default);
        Assert.Equal(2, await this.store.Count(default));
        Assert.Equal(new CallDirectoryEntry(18005550100, "Support"), await this.store.Get(18005550100, default));

        await this.store.Remove([18005550100], default);
        Assert.Null(await this.store.Get(18005550100, default));
        Assert.Equal(1, await this.store.Count(default));
    }


    [MacFact]
    public async Task UnchangedEntriesAreNotLogged()
    {
        await this.store.Set([new(18005550100, "Support")], default);
        await this.store.Set([new(18005550100, "Support")], default);
        Assert.Equal(1, await this.store.PendingChanges(default));
    }


    [MacFact]
    public async Task UnknownRemovalsAreNotLogged()
    {
        await this.store.Remove([18005550100], default);
        Assert.Equal(0, await this.store.PendingChanges(default));
    }


    [MacFact]
    public async Task ReplaceAllStartsANewGeneration()
    {
        await this.store.Set([new(18005550100, "Support")], default);
        var before = await this.store.Generation(default);

        await this.store.ReplaceAll([new(18005550101, "Sales"), new(18005550102, "Billing")], default);
        Assert.Equal(before + 1, await this.store.Generation(default));
        Assert.Equal(0, await this.store.PendingChanges(default));
        Assert.Equal(2, await this.store.Count(default));
    }


    [MacFact]
    public async Task TrimOnlyDropsAppliedChangesOfTheSameGeneration()
    {
        await this.store.Set([new(1, "a"), new(2, "b"), new(3, "c")], default);
        var gen = await this.store.Generation(default);

        await this.store.Trim(gen + 1, 100, default);
        Assert.Equal(3, await this.store.PendingChanges(default));

        await this.store.Trim(gen, 2, default);
        Assert.Equal(1, await this.store.PendingChanges(default));
    }


    [MacFact]
    public async Task FailedWriteRollsBack()
    {
        await this.store.Set([new(1, "a")], default);
        await Assert.ThrowsAsync<ArgumentException>(() => this.store.Set([new(2, "b"), new(-5, "bad")], default));

        Assert.Equal(1, await this.store.Count(default));
        Assert.Equal(1, await this.store.PendingChanges(default));
    }


    [MacFact]
    public async Task WithoutChangeTracking_NothingIsLogged()
    {
        // Android reads entries live, so its store keeps no change log at all
        var untracked = new CallDirectoryStore(Path.Combine(this.dir, "untracked.db"), trackChanges: false);
        await untracked.Set([new(18005550100, "Support"), new(18005550101, null, CallDirectoryEntryType.Block)], default);
        await untracked.Remove([18005550101], default);

        Assert.Equal(0, await untracked.PendingChanges(default));
        Assert.Equal(new CallDirectoryEntry(18005550100, "Support"), await untracked.Get(18005550100, default));
        Assert.Equal(1, await untracked.Count(default));
    }
}
