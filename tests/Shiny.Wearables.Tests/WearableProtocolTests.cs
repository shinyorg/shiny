using Xunit;

namespace Shiny.Wearables.Tests;


public class WearableProtocolTests
{
    [Theory]
    [InlineData("sync", "sync")]
    [InlineData("/sync", "sync")]
    [InlineData("/workout/start/", "workout/start")]
    [InlineData("a.b-c_d", "a.b-c_d")]
    public void PathsAreNormalized(string input, string expected)
        => Assert.Equal(expected, WearableProtocol.NormalizePath(input));


    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("//")]
    [InlineData("has space")]
    [InlineData("query?x=1")]
    [InlineData("frag#x")]
    public void InvalidPathsAreRejected(string input)
        => Assert.Throws<ArgumentException>(() => WearableProtocol.NormalizePath(input));


    [Fact]
    public void MessagePathsRoundTrip()
    {
        var wire = WearableProtocol.ToMessagePath("/workout/start");
        Assert.Equal("/shiny/message/workout/start", wire);
        Assert.Equal("workout/start", WearableProtocol.FromMessagePath(wire));
    }


    [Theory]
    [InlineData(null)]
    [InlineData("/other/message/x")]
    [InlineData("/shiny/message/")]
    [InlineData("/shiny/context")]
    public void ForeignMessagePathsAreNotShiny(string? wire)
        => Assert.Null(WearableProtocol.FromMessagePath(wire));


    [Fact]
    public void CapabilityIsCoveredByTheListenerPrefix()
    {
        // The listener service's intent filter matches capability changes only because the capability URI
        // (wear://*/{capability}) falls under the /shiny path prefix.
        Assert.StartsWith(WearableProtocol.Prefix, "/" + WearableProtocol.Capability);
    }


    [Theory]
    [InlineData("photo.jpg", "photo.jpg")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("..\\..\\evil.txt", "evil.txt")]
    [InlineData("dir/sub/file.bin", "file.bin")]
    [InlineData("..", "file")]
    [InlineData("", "file")]
    public void InboxPathsStayInsideTheInbox(string fileName, string expectedName)
    {
        var root = Path.Combine(Path.GetTempPath(), "inbox-root");
        var path = WearableProtocol.GetInboxPath(root, "abc123", fileName);

        Assert.Equal(Path.Combine(root, "Shiny.Wearables", "abc123", expectedName), path);
        Assert.StartsWith(Path.Combine(root, "Shiny.Wearables"), Path.GetFullPath(path));
    }


    [Fact]
    public void InboxPathIgnoresATraversingTransferId()
    {
        var root = Path.Combine(Path.GetTempPath(), "inbox-root");
        var path = WearableProtocol.GetInboxPath(root, "..", "a.txt");

        Assert.StartsWith(Path.Combine(root, "Shiny.Wearables"), Path.GetFullPath(path));
        Assert.EndsWith("a.txt", path);
    }


    [Fact]
    public void TransferIdsAreUnique()
        => Assert.NotEqual(WearableProtocol.NewTransferId(), WearableProtocol.NewTransferId());
}
