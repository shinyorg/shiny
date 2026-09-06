using System.Text.Json;
using Shiny.LiveActivities;

namespace Shiny.LiveActivities.Tests;


/// <summary>
/// The content-state JSON is a contract shared by three codebases — this library, the Swift
/// <c>ShinyActivityAttributes.ContentState</c>, and whatever a server pushes as <c>content-state</c>.
/// A field rename or a date encoded against the wrong epoch shows up at runtime as an activity that
/// silently stops updating, so it is pinned here.
/// </summary>
public class ContentSchemaTests
{
    static JsonElement Parse(LiveActivityContent content)
        => JsonDocument.Parse(LiveActivityContentSchema.ToJson(content)).RootElement;


    [Fact]
    public void Text_FieldNamesMatchTheSwiftContentState()
    {
        var root = Parse(new LiveActivityContent
        {
            Title = "Out for delivery",
            Body = "2 stops away",
            ShortStatus = "5 min"
        });

        Assert.Equal("Out for delivery", root.GetProperty("title").GetString());
        Assert.Equal("2 stops away", root.GetProperty("body").GetString());
        Assert.Equal("5 min", root.GetProperty("shortStatus").GetString());
    }


    [Fact]
    public void OmittedText_IsAbsentRatherThanNull()
    {
        var root = Parse(new LiveActivityContent());

        Assert.False(root.TryGetProperty("title", out _));
        Assert.False(root.TryGetProperty("body", out _));
        Assert.False(root.TryGetProperty("shortStatus", out _));
    }


    [Fact]
    public void Data_IsAlwaysPresent_EvenWhenEmpty()
    {
        var root = Parse(new LiveActivityContent());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("data").ValueKind);
    }


    [Fact]
    public void Data_RoundTripsCustomValues()
    {
        var root = Parse(new LiveActivityContent
        {
            Data = new Dictionary<string, string> { ["orderId"] = "A-1234" }
        });
        Assert.Equal("A-1234", root.GetProperty("data").GetProperty("orderId").GetString());
    }


    [Fact]
    public void Progress_Value_IsANumber()
    {
        var root = Parse(new LiveActivityContent { Progress = LiveActivityProgress.FromValue(0.65) });

        Assert.Equal(JsonValueKind.Number, root.GetProperty("progress").ValueKind);
        Assert.Equal(0.65, root.GetProperty("progress").GetDouble());
    }


    [Fact]
    public void Progress_Range_UsesSwiftReferenceDate()
    {
        // Swift's Date zero is 2001-01-01T00:00:00Z, not the Unix epoch. Getting this wrong puts the
        // widget's timer about 31 years out.
        var start = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);

        var root = Parse(new LiveActivityContent { Progress = LiveActivityProgress.FromRange(start, end) });

        Assert.Equal(0, root.GetProperty("progressStart").GetDouble());
        Assert.Equal(3600, root.GetProperty("progressEnd").GetDouble());
    }


    [Fact]
    public void ReferenceDate_OffsetIsUnixMinus978307200()
    {
        var epoch = DateTimeOffset.FromUnixTimeSeconds(0);
        Assert.Equal(-978307200d, LiveActivityContentSchema.ToAppleReferenceSeconds(epoch));
    }


    [Fact]
    public void Indeterminate_OnlyWrittenWhenTrue()
    {
        var off = Parse(new LiveActivityContent { Progress = LiveActivityProgress.FromValue(0.5) });
        Assert.False(off.TryGetProperty("indeterminate", out _));

        var on = Parse(new LiveActivityContent { Progress = new LiveActivityProgress { Indeterminate = true } });
        Assert.True(on.GetProperty("indeterminate").GetBoolean());
    }


    [Fact]
    public void StaleDate_IsNotPartOfTheContentState()
    {
        // It travels as an ActivityContent parameter / aps.stale-date, not inside content-state.
        var root = Parse(new LiveActivityContent { StaleDate = DateTimeOffset.UtcNow });
        Assert.False(root.TryGetProperty("staleDate", out _));
    }


    [Fact]
    public void Attributes_CarryKindAndValues()
    {
        var json = LiveActivityContentSchema.AttributesToJson(new LiveActivityRequest
        {
            Content = new LiveActivityContent(),
            Kind = "delivery",
            Attributes = new Dictionary<string, string> { ["orderNumber"] = "A-1234" }
        });
        var root = JsonDocument.Parse(json).RootElement;

        Assert.Equal("delivery", root.GetProperty("kind").GetString());
        Assert.Equal("A-1234", root.GetProperty("values").GetProperty("orderNumber").GetString());
    }


    [Fact]
    public void Attributes_ValuesPresentWithoutKind()
    {
        var json = LiveActivityContentSchema.AttributesToJson(new LiveActivityRequest { Content = new LiveActivityContent() });
        var root = JsonDocument.Parse(json).RootElement;

        Assert.False(root.TryGetProperty("kind", out _));
        Assert.Equal(JsonValueKind.Object, root.GetProperty("values").ValueKind);
    }
}
