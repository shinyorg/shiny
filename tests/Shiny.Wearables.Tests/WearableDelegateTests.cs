using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Shiny.Wearables.Tests;


public class WearableDelegateTests
{
    static readonly WearableMessage Message = new("sync", [1, 2, 3], "node", true);


    [Fact]
    public async Task FirstNonNullReplyWins()
    {
        var calls = new List<string>();
        var delegates = new IWearableDelegate[]
        {
            new Replying("silent", null, calls),
            new Replying("first", [9], calls),
            new Replying("second", [8], calls)
        };

        var reply = await WearableDelegates.GetReply(delegates, Message, NullLogger.Instance);

        Assert.Equal([9], reply);
        Assert.Equal(["silent", "first"], calls); // the rest are not asked once one answered
    }


    [Fact]
    public async Task NoReplyIsEmpty()
    {
        var reply = await WearableDelegates.GetReply([new WearableDelegate()], Message, NullLogger.Instance);
        Assert.Empty(reply);
    }


    [Fact]
    public async Task NoDelegatesIsEmpty()
    {
        var reply = await WearableDelegates.GetReply([], Message, NullLogger.Instance);
        Assert.Empty(reply);
    }


    [Fact]
    public async Task AThrowingDelegateIsSkipped()
    {
        var calls = new List<string>();
        var reply = await WearableDelegates.GetReply(
            [new Throwing(), new Replying("after", [7], calls)],
            Message,
            NullLogger.Instance
        );

        Assert.Equal([7], reply);
    }


    sealed class Replying(string name, byte[]? reply, List<string> calls) : WearableDelegate
    {
        public override Task<byte[]?> OnMessageReceived(WearableMessage message)
        {
            calls.Add(name);
            return Task.FromResult(reply);
        }
    }


    sealed class Throwing : WearableDelegate
    {
        public override Task<byte[]?> OnMessageReceived(WearableMessage message)
            => throw new InvalidOperationException("boom");
    }
}
