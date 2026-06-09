using Xunit;

namespace Shiny.Data.Sync.Tests;


public class TombstoneParserTests
{
    [Fact]
    public void EmptyBody_ReturnsEmptyWithFallbackCursor()
    {
        var result = PublicSurface.ParseTombstone("", fallbackCursor: "prev");
        Assert.Equal("prev", result.NextCursor);
        Assert.Empty(result.DeletedIdentifiers);
    }


    [Fact]
    public void FlatArray_ParsesAllIds()
    {
        var result = PublicSurface.ParseTombstone("""["a","b","c"]""", fallbackCursor: "prev");

        Assert.Equal(new[] { "a", "b", "c" }, result.DeletedIdentifiers);
        Assert.Equal("prev", result.NextCursor);
    }


    [Fact]
    public void ObjectShape_ParsesBothCursorAndIds()
    {
        var body = """{ "cursor": "cur-99", "ids": ["x","y"] }""";

        var result = PublicSurface.ParseTombstone(body, fallbackCursor: "prev");

        Assert.Equal("cur-99", result.NextCursor);
        Assert.Equal(new[] { "x", "y" }, result.DeletedIdentifiers);
    }


    [Fact]
    public void ObjectShape_CursorOnly_KeepsEmptyIds()
    {
        var body = """{ "cursor": "cur-1" }""";
        var result = PublicSurface.ParseTombstone(body, fallbackCursor: "prev");

        Assert.Equal("cur-1", result.NextCursor);
        Assert.Empty(result.DeletedIdentifiers);
    }


    [Fact]
    public void ObjectShape_IdsOnly_FallsBackToProvidedCursor()
    {
        var body = """{ "ids": ["m"] }""";
        var result = PublicSurface.ParseTombstone(body, fallbackCursor: "fallback");

        Assert.Equal("fallback", result.NextCursor);
        Assert.Single(result.DeletedIdentifiers, "m");
    }


    [Fact]
    public void NonStringEntriesInArray_AreSkipped()
    {
        var body = """["a", 42, null, "b", true]""";
        var result = PublicSurface.ParseTombstone(body, fallbackCursor: null);

        Assert.Equal(new[] { "a", "b" }, result.DeletedIdentifiers);
    }


    [Fact]
    public void EmptyStringEntriesInArray_AreSkipped()
    {
        var body = """["a", "", "b"]""";
        var result = PublicSurface.ParseTombstone(body, fallbackCursor: null);

        Assert.Equal(new[] { "a", "b" }, result.DeletedIdentifiers);
    }


    [Fact]
    public void NullBody_ReturnsEmpty()
    {
        var result = PublicSurface.ParseTombstone(null, fallbackCursor: "z");
        Assert.Equal("z", result.NextCursor);
        Assert.Empty(result.DeletedIdentifiers);
    }
}
