using Xunit;

namespace Shiny.Push.Tests;


public class PushImagesTests
{
    [Fact]
    public void Find_ReturnsFirstDefaultKeyWithUrl()
    {
        var data = new Dictionary<string, string>
        {
            ["attachment-url"] = "https://example.com/b.png",
            ["image"] = "https://example.com/a.png"
        };
        Assert.Equal("https://example.com/a.png", PushImages.Find(data));
    }


    [Fact]
    public void Find_SkipsValuesThatAreNotWebUrls()
    {
        var data = new Dictionary<string, string>
        {
            ["image"] = "not a url",
            ["image-url"] = "file:///tmp/a.png",
            ["imageUrl"] = " http://example.com/c.jpg "
        };
        Assert.Equal("http://example.com/c.jpg", PushImages.Find(data));
    }


    [Fact]
    public void Find_UsesCustomKeys()
    {
        var data = new Dictionary<string, string>
        {
            ["image"] = "https://example.com/a.png",
            ["hero"] = "https://example.com/hero.png"
        };
        Assert.Equal("https://example.com/hero.png", PushImages.Find(data, ["hero"]));
    }


    [Fact]
    public void Find_NoImage_ReturnsNull()
    {
        Assert.Null(PushImages.Find(new Dictionary<string, string> { ["title"] = "hi" }));
        Assert.Null(PushImages.Find((IDictionary<string, string>?)null));
    }


    [Fact]
    public void Notification_ImageUriDefaultsToNull()
        => Assert.Null(new Notification("t", "m").ImageUri);
}
