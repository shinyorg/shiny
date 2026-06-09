using System;
using System.Collections.Generic;
using Shiny.Data.Sync.Infrastructure;
using Xunit;

namespace Shiny.Data.Sync.Tests;


public class BatchResultParserTests
{
    static SyncOperation Op(string id) => new(
        Identifier: id,
        EndpointKey: "Endpoint",
        EntityIdentifier: id + "-entity",
        Verb: SyncVerb.Create,
        Payload: "{}",
        State: SyncOperationState.InProgress,
        CreatedAt: DateTimeOffset.UtcNow,
        Attempts: 1,
        LastError: null
    );


    [Fact]
    public void WellFormed_MapsEachResultToItsOp()
    {
        var ops = new[] { Op("op-1"), Op("op-2") };
        var body = """
        {
          "results": [
            { "id": "op-1", "status": 200, "body": { "ok": true }, "error": null },
            { "id": "op-2", "status": 201, "body": { "id": "srv-2" }, "error": null }
          ]
        }
        """;

        var result = PublicSurface.ParseBatch(outerStatus: 200, ops, body);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("op-1", result.Items[0].OperationIdentifier);
        Assert.Equal(200, result.Items[0].StatusCode);
        Assert.False(result.Items[0].IsConflict);
        Assert.Contains("ok", result.Items[0].ResponseBody);
        Assert.Equal(201, result.Items[1].StatusCode);
    }


    [Fact]
    public void MissingEntry_DefaultsToOuterStatus()
    {
        var ops = new[] { Op("op-1"), Op("op-2") };
        var body = """{ "results": [ { "id": "op-1", "status": 200 } ] }""";

        var result = PublicSurface.ParseBatch(outerStatus: 207, ops, body);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(200, result.Items[0].StatusCode);
        Assert.Equal(207, result.Items[1].StatusCode);     // default-fill from outer
        Assert.Null(result.Items[1].ResponseBody);
    }


    [Fact]
    public void Status409_FlagsConflict()
    {
        var ops = new[] { Op("op-1") };
        var body = """{ "results": [ { "id": "op-1", "status": 409, "body": { "current": {} } } ] }""";

        var result = PublicSurface.ParseBatch(outerStatus: 207, ops, body);

        Assert.True(result.Items[0].IsConflict);
        Assert.Equal(409, result.Items[0].StatusCode);
    }


    [Fact]
    public void Status412_FlagsConflict()
    {
        var ops = new[] { Op("op-1") };
        var body = """{ "results": [ { "id": "op-1", "status": 412 } ] }""";

        var result = PublicSurface.ParseBatch(outerStatus: 207, ops, body);

        Assert.True(result.Items[0].IsConflict);
    }


    [Fact]
    public void ErrorField_IsCarried()
    {
        var ops = new[] { Op("op-1") };
        var body = """{ "results": [ { "id": "op-1", "status": 500, "error": "boom" } ] }""";

        var result = PublicSurface.ParseBatch(outerStatus: 200, ops, body);

        Assert.Equal("boom", result.Items[0].ErrorMessage);
    }


    [Fact]
    public void MalformedJson_FallsBackToOuterStatus()
    {
        var ops = new[] { Op("op-1"), Op("op-2") };

        var result = PublicSurface.ParseBatch(outerStatus: 500, ops, "not json");

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal(500, item.StatusCode));
    }


    [Fact]
    public void NullBody_FallsBackToOuterStatus()
    {
        var ops = new[] { Op("op-1") };
        var result = PublicSurface.ParseBatch(outerStatus: 200, ops, body: null);
        Assert.Single(result.Items);
        Assert.Equal(200, result.Items[0].StatusCode);
    }


    [Fact]
    public void OuterStatusCodeOnResult()
    {
        var ops = new[] { Op("op-1") };
        var result = PublicSurface.ParseBatch(outerStatus: 207, ops, """{ "results": [] }""");
        Assert.Equal(207, result.StatusCode);
    }
}
