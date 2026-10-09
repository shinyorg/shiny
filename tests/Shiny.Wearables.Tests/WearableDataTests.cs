using System.Text;
using System.Text.Json;
using Xunit;

namespace Shiny.Wearables.Tests;


public class WearableDataTests
{
    enum Kind { Run, Walk }


    [Fact]
    public void StringsAreUtf8()
    {
        var bytes = WearableData.FromString("on your marks 🏃");
        Assert.Equal(Encoding.UTF8.GetBytes("on your marks 🏃"), bytes);
        Assert.Equal("on your marks 🏃", WearableData.GetString(bytes));
    }


    [Fact]
    public void NoBytesIsAnEmptyString()
    {
        Assert.Equal(String.Empty, WearableData.GetString([]));
        Assert.Equal(String.Empty, WearableData.GetString(null));
    }


    [Fact]
    public void ValuesAreAJsonObject()
    {
        var id = Guid.NewGuid();
        var bytes = WearableData.FromValues(new Dictionary<string, object?>
        {
            ["kind"] = Kind.Run,
            ["distance"] = 5.5,
            ["laps"] = 3,
            ["outdoor"] = true,
            ["note"] = null,
            ["id"] = id,
            ["splits"] = new[] { 300, 310 },
            ["coach"] = new Dictionary<string, string> { ["name"] = "Sam" }
        });

        using var doc = JsonDocument.Parse(bytes);
        var root = doc.RootElement;
        Assert.Equal("Run", root.GetProperty("kind").GetString());
        Assert.Equal(5.5, root.GetProperty("distance").GetDouble());
        Assert.Equal(3, root.GetProperty("laps").GetInt32());
        Assert.True(root.GetProperty("outdoor").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("note").ValueKind);
        Assert.Equal(id, root.GetProperty("id").GetGuid());
        Assert.Equal([300, 310], root.GetProperty("splits").EnumerateArray().Select(x => x.GetInt32()));
        Assert.Equal("Sam", root.GetProperty("coach").GetProperty("name").GetString());
    }


    [Fact]
    public void StronglyTypedDictionariesAreAccepted()
    {
        var bytes = WearableData.FromValues(new Dictionary<string, string> { ["plan"] = "5k" });
        Assert.Equal("""{"plan":"5k"}""", Encoding.UTF8.GetString(bytes));
    }


    [Fact]
    public void ValuesRoundTrip()
    {
        var values = WearableData.GetValues(WearableData.FromValues(new Dictionary<string, object> { ["plan"] = "5k", ["week"] = 2 }));

        Assert.Equal("5k", values["plan"].GetString());
        Assert.Equal(2, values["week"].GetInt32());
    }


    [Fact]
    public void NoBytesAreNoValues()
    {
        Assert.Empty(WearableData.GetValues([]));
        Assert.Empty(WearableData.GetValues(null));
    }


    [Fact]
    public void ANonObjectIsRejected()
        => Assert.ThrowsAny<JsonException>(() => WearableData.GetValues(Encoding.UTF8.GetBytes("[1,2]")));


    [Fact]
    public void AnUnsupportedValueIsRejected()
        => Assert.Throws<NotSupportedException>(() => WearableData.FromValues(new Dictionary<string, object> { ["x"] = new object() }));


    [Fact]
    public void ReceivedModelsReadTheirBody()
    {
        var message = new WearableMessage("sync", WearableData.FromValues(new Dictionary<string, int> { ["count"] = 4 }), "node", true);
        Assert.Equal(4, message.GetValues()["count"].GetInt32());

        var context = new WearableContext(WearableData.FromString("hello"), null);
        Assert.Equal("hello", context.GetString());
    }


    [Fact]
    public async Task ExtensionsEncodeAndDecode()
    {
        var manager = new EchoManager();

        Assert.Equal("pong", await manager.SendMessage("ping", "pong"));

        var reply = await manager.SendMessage("sync", new Dictionary<string, object> { ["n"] = 1 });
        Assert.Equal(1, reply["n"].GetInt32());

        await manager.UpdateContext(new Dictionary<string, string> { ["plan"] = "5k" });
        Assert.Equal("""{"plan":"5k"}""", Encoding.UTF8.GetString(manager.Context!));

        await manager.Transfer("log", "lap done");
        Assert.Equal("lap done", Encoding.UTF8.GetString(manager.Transferred!));
    }


    // replies with what it was sent
    class EchoManager : IWearableManager
    {
        public byte[]? Context { get; private set; }
        public byte[]? Transferred { get; private set; }

        public Task<byte[]> SendMessage(string path, byte[] data, string? nodeId = null, CancellationToken cancelToken = default) => Task.FromResult(data);
        public Task UpdateContext(byte[] data, CancellationToken cancelToken = default) { this.Context = data; return Task.CompletedTask; }
        public Task<string> Transfer(string path, byte[] data, CancellationToken cancelToken = default) { this.Transferred = data; return Task.FromResult("id"); }

        public Task<WearableStatus> GetStatus(CancellationToken cancelToken = default) => throw new NotImplementedException();
        public Task<byte[]?> GetContext(CancellationToken cancelToken = default) => throw new NotImplementedException();
        public Task<WearableContext?> GetReceivedContext(CancellationToken cancelToken = default) => throw new NotImplementedException();
        public Task<string> TransferFile(string path, string filePath, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancelToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<WearableTransferInfo>> GetPendingTransfers(CancellationToken cancelToken = default) => throw new NotImplementedException();
        public Task<bool> CancelTransfer(string id, CancellationToken cancelToken = default) => throw new NotImplementedException();
    }
}
