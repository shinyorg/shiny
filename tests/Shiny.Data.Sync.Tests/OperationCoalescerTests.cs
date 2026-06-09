using System;
using System.Collections.Generic;
using Shiny.Data.Sync.Infrastructure;
using Xunit;

namespace Shiny.Data.Sync.Tests;


public class OperationCoalescerTests
{
    static SyncOperation Op(string entityId, SyncVerb verb, string? payload = null, int orderTicks = 0)
        => new(
            Identifier: $"op-{entityId}-{verb}-{orderTicks}",
            EndpointKey: "Endpoint",
            EntityIdentifier: entityId,
            Verb: verb,
            Payload: payload,
            State: SyncOperationState.Pending,
            CreatedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(orderTicks),
            Attempts: 0,
            LastError: null
        );


    [Fact]
    public void SingleOp_PassesThrough()
    {
        var input = new List<SyncOperation> { Op("a", SyncVerb.Create, "{}") };

        var result = OperationCoalescer.Coalesce(input);

        Assert.Single(result);
        Assert.Equal(SyncVerb.Create, result[0].Verb);
    }


    [Fact]
    public void EmptyInput_ReturnsEmpty()
    {
        var result = OperationCoalescer.Coalesce(new List<SyncOperation>());
        Assert.Empty(result);
    }


    [Fact]
    public void CreatePlusUpdate_KeepsCreateVerb_TakesLatestPayload()
    {
        var input = new List<SyncOperation>
        {
            Op("a", SyncVerb.Create, "{\"v\":1}", orderTicks: 1),
            Op("a", SyncVerb.Update, "{\"v\":2}", orderTicks: 2)
        };

        var result = OperationCoalescer.Coalesce(input);

        Assert.Single(result);
        Assert.Equal(SyncVerb.Create, result[0].Verb);
        Assert.Equal("{\"v\":2}", result[0].Payload);
    }


    [Fact]
    public void UpdatePlusUpdate_KeepsUpdate_LatestPayloadWins()
    {
        var input = new List<SyncOperation>
        {
            Op("a", SyncVerb.Update, "{\"v\":1}", orderTicks: 1),
            Op("a", SyncVerb.Update, "{\"v\":2}", orderTicks: 2),
            Op("a", SyncVerb.Update, "{\"v\":3}", orderTicks: 3)
        };

        var result = OperationCoalescer.Coalesce(input);

        Assert.Single(result);
        Assert.Equal(SyncVerb.Update, result[0].Verb);
        Assert.Equal("{\"v\":3}", result[0].Payload);
    }


    [Fact]
    public void TrailingDelete_DropsPriorCreateAndUpdates()
    {
        var input = new List<SyncOperation>
        {
            Op("a", SyncVerb.Create, "{\"v\":1}", orderTicks: 1),
            Op("a", SyncVerb.Update, "{\"v\":2}", orderTicks: 2),
            Op("a", SyncVerb.Delete, payload: null, orderTicks: 3)
        };

        var result = OperationCoalescer.Coalesce(input);

        Assert.Single(result);
        Assert.Equal(SyncVerb.Delete, result[0].Verb);
        Assert.Null(result[0].Payload);
    }


    [Fact]
    public void DeleteThenCreate_TreatedAsCreate()
    {
        var input = new List<SyncOperation>
        {
            Op("a", SyncVerb.Delete, payload: null, orderTicks: 1),
            Op("a", SyncVerb.Create, "{\"v\":42}", orderTicks: 2)
        };

        var result = OperationCoalescer.Coalesce(input);

        Assert.Single(result);
        Assert.Equal(SyncVerb.Create, result[0].Verb);
        Assert.Equal("{\"v\":42}", result[0].Payload);
    }


    [Fact]
    public void MultipleEntities_PreservesFirstAppearanceOrder()
    {
        var input = new List<SyncOperation>
        {
            Op("b", SyncVerb.Create, "{\"b\":1}", orderTicks: 1),
            Op("a", SyncVerb.Create, "{\"a\":1}", orderTicks: 2),
            Op("c", SyncVerb.Create, "{\"c\":1}", orderTicks: 3),
            Op("a", SyncVerb.Update, "{\"a\":2}", orderTicks: 4)
        };

        var result = OperationCoalescer.Coalesce(input);

        Assert.Equal(3, result.Count);
        Assert.Equal("b", result[0].EntityIdentifier);
        Assert.Equal("a", result[1].EntityIdentifier);
        Assert.Equal("c", result[2].EntityIdentifier);
        Assert.Equal("{\"a\":2}", result[1].Payload);
    }


    [Fact]
    public void MergedOp_RetainsFirstOpIdentifier()
    {
        var input = new List<SyncOperation>
        {
            Op("a", SyncVerb.Create, "{\"v\":1}", orderTicks: 1),
            Op("a", SyncVerb.Update, "{\"v\":2}", orderTicks: 2)
        };

        var result = OperationCoalescer.Coalesce(input);

        Assert.Single(result);
        Assert.Equal(input[0].Identifier, result[0].Identifier);
    }


    [Fact]
    public void DeletesAcrossDifferentEntities_AllPreserved()
    {
        var input = new List<SyncOperation>
        {
            Op("a", SyncVerb.Create, "{}", orderTicks: 1),
            Op("a", SyncVerb.Delete, payload: null, orderTicks: 2),
            Op("b", SyncVerb.Update, "{}", orderTicks: 3),
            Op("b", SyncVerb.Delete, payload: null, orderTicks: 4)
        };

        var result = OperationCoalescer.Coalesce(input);

        Assert.Equal(2, result.Count);
        Assert.All(result, op => Assert.Equal(SyncVerb.Delete, op.Verb));
    }
}
