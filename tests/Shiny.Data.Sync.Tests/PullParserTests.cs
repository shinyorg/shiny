using System.Text.Json;
using Shiny.Data.Sync.Infrastructure;
using Xunit;

namespace Shiny.Data.Sync.Tests;


public class PullParserTests
{
    // PullParser is internal; access via InternalsVisibleTo would be cleanest but it's not set up,
    // so we exercise it through RestSyncTransport behaviour or use reflection. For these tests we
    // call via PublicSurface.Parse which delegates - see PublicSurface helper below.

    [Fact]
    public void EmptyBody_ReturnsEmptyResult()
    {
        var result = PublicSurface.ParsePull("");
        Assert.Null(result.NextCursor);
        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
    }


    [Fact]
    public void WhitespaceBody_ReturnsEmptyResult()
    {
        var result = PublicSurface.ParsePull("   \n\t  ");
        Assert.Empty(result.Items);
    }


    [Fact]
    public void NullBody_ReturnsEmptyResult()
    {
        var result = PublicSurface.ParsePull(null);
        Assert.Empty(result.Items);
    }


    [Fact]
    public void ValidResponse_ParsesAllFields()
    {
        var body = """
        {
          "cursor": "cur-123",
          "hasMore": true,
          "items": [
            { "id": "a", "verb": "Create", "payload": { "v": 1 } },
            { "id": "b", "verb": "Update", "payload": { "v": 2 } },
            { "id": "c", "verb": "Delete", "payload": null }
          ]
        }
        """;

        var result = PublicSurface.ParsePull(body);

        Assert.Equal("cur-123", result.NextCursor);
        Assert.True(result.HasMore);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal("a", result.Items[0].EntityIdentifier);
        Assert.Equal(SyncVerb.Create, result.Items[0].Verb);
        Assert.Equal(SyncVerb.Update, result.Items[1].Verb);
        Assert.Equal(SyncVerb.Delete, result.Items[2].Verb);
    }


    [Fact]
    public void VerbIsCaseInsensitive()
    {
        var body = """{ "items": [ { "id": "x", "verb": "create", "payload": null } ] }""";

        var result = PublicSurface.ParsePull(body);

        Assert.Equal(SyncVerb.Create, result.Items[0].Verb);
    }


    [Fact]
    public void MissingCursor_IsNull()
    {
        var body = """{ "items": [] }""";
        var result = PublicSurface.ParsePull(body);
        Assert.Null(result.NextCursor);
    }


    [Fact]
    public void MissingHasMore_DefaultsFalse()
    {
        var body = """{ "items": [] }""";
        var result = PublicSurface.ParsePull(body);
        Assert.False(result.HasMore);
    }


    [Fact]
    public void EmptyItemsArray_ReturnsEmpty()
    {
        var body = """{ "cursor": "x", "items": [] }""";
        var result = PublicSurface.ParsePull(body);
        Assert.Empty(result.Items);
        Assert.Equal("x", result.NextCursor);
    }


    [Fact]
    public void PayloadRoundtripsAsRawJson()
    {
        var body = """{ "items": [ { "id": "a", "verb": "Update", "payload": { "nested": { "n": 7 } } } ] }""";
        var result = PublicSurface.ParsePull(body);

        var doc = JsonDocument.Parse(result.Items[0].Payload);
        Assert.Equal(7, doc.RootElement.GetProperty("nested").GetProperty("n").GetInt32());
    }
}
