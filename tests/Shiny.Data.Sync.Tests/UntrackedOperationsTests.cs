using System;
using System.Collections.Generic;
using System.Linq;
using Shiny.Data.Sync.Infrastructure;
using Xunit;

namespace Shiny.Data.Sync.Tests;


public class UntrackedOperationsTests
{
    static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);


    static SyncOperation Op(string id, SyncOperationState state, DateTimeOffset? nextAttemptAt = null, int order = 0)
        => new(id, "Endpoint", "entity-" + id, SyncVerb.Update, "{}", state, BaseTime.AddSeconds(order), 1, null, nextAttemptAt);


    static HashSet<string> Tracked(params string[] ids)
        => new(ids, StringComparer.InvariantCultureIgnoreCase);


    [Theory]
    [InlineData(SyncOperationState.Pending)]
    [InlineData(SyncOperationState.InProgress)]
    public void NoTaskNoRetryMarker_IsSelected(SyncOperationState state)
    {
        var result = UntrackedOperations.Select(new[] { Op("a", state) }, Tracked());

        Assert.Equal("a", Assert.Single(result).Identifier);
    }


    [Fact]
    public void OpWithNativeTask_IsNotSelected()
    {
        var result = UntrackedOperations.Select(new[] { Op("a", SyncOperationState.InProgress) }, Tracked("A"));

        Assert.Empty(result);
    }


    [Fact]
    public void OpWaitingOutRetry_IsLeftToTheRetryPass()
    {
        var ops = new[] { Op("a", SyncOperationState.Pending, BaseTime.AddMinutes(5)) };

        var result = UntrackedOperations.Select(ops, Tracked());

        Assert.Empty(result);
    }


    [Theory]
    [InlineData(SyncOperationState.Completed)]
    [InlineData(SyncOperationState.Error)]
    [InlineData(SyncOperationState.Canceled)]
    [InlineData(SyncOperationState.ConflictPending)]
    public void OtherStates_AreNotSelected(SyncOperationState state)
    {
        var result = UntrackedOperations.Select(new[] { Op("a", state) }, Tracked());

        Assert.Empty(result);
    }


    [Fact]
    public void Selected_InCreationOrder()
    {
        var ops = new[]
        {
            Op("c", SyncOperationState.Pending, order: 3),
            Op("a", SyncOperationState.InProgress, order: 1),
            Op("tracked", SyncOperationState.Pending, order: 0),
            Op("b", SyncOperationState.Pending, order: 2)
        };

        var result = UntrackedOperations.Select(ops, Tracked("tracked"));

        Assert.Equal(new[] { "a", "b", "c" }, result.Select(x => x.Identifier).ToArray());
    }
}
