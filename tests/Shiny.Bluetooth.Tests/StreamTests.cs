using Shiny.Bluetooth.Infrastructure;
using Xunit;

namespace Shiny.Bluetooth.Tests;


public class PushStreamTests
{
    [Fact]
    public async Task Read_DrainsPushedChunksAcrossSmallBuffers()
    {
        var stream = new PushStream((_, _) => Task.CompletedTask, () => { });
        stream.Push([1, 2, 3]);
        stream.Push([4, 5]);

        var buffer = new byte[2];
        var read = new List<byte>();
        for (var i = 0; i < 3; i++)
        {
            var count = await stream.ReadAsync(buffer);
            read.AddRange(buffer.Take(count));
        }
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, read);
    }


    [Fact]
    public async Task Read_ReturnsZeroOnceCompletedAndDrained()
    {
        var stream = new PushStream((_, _) => Task.CompletedTask, () => { });
        stream.Push([9]);
        stream.Complete();

        var buffer = new byte[4];
        Assert.Equal(1, await stream.ReadAsync(buffer));
        Assert.Equal(0, await stream.ReadAsync(buffer));
    }


    [Fact]
    public async Task Read_WaitsForData()
    {
        var stream = new PushStream((_, _) => Task.CompletedTask, () => { });
        var buffer = new byte[4];
        var pending = stream.ReadAsync(buffer).AsTask();
        Assert.False(pending.IsCompleted);

        stream.Push([7]);
        Assert.Equal(1, await pending.WaitAsync(TimeSpan.FromSeconds(5)));
    }


    [Fact]
    public async Task Read_SurfacesCompletionErrorAsIOException()
    {
        var stream = new PushStream((_, _) => Task.CompletedTask, () => { });
        stream.Complete(new InvalidOperationException("radio off"));
        await Assert.ThrowsAsync<IOException>(() => stream.ReadAsync(new byte[1]).AsTask());
    }


    [Fact]
    public async Task Write_GoesThroughTheDelegate_AndDisposeClosesOnce()
    {
        var written = new List<byte>();
        var closes = 0;
        var stream = new PushStream((data, _) =>
        {
            written.AddRange(data.ToArray());
            return Task.CompletedTask;
        }, () => closes++);

        await stream.WriteAsync(new byte[] { 1, 2 });
        stream.Dispose();
        stream.Dispose();

        Assert.Equal(new byte[] { 1, 2 }, written);
        Assert.Equal(1, closes);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => stream.WriteAsync(new byte[] { 3 }).AsTask());
    }
}


public class DuplexStreamTests
{
    [Fact]
    public async Task ReadsInput_WritesOutput_ClosesOnce()
    {
        var input = new MemoryStream([1, 2, 3]);
        var output = new MemoryStream();
        var closes = 0;
        var stream = new DuplexStream(input, output, () => closes++);

        var buffer = new byte[3];
        Assert.Equal(3, await stream.ReadAsync(buffer));
        await stream.WriteAsync(new byte[] { 4, 5 });
        Assert.Equal(new byte[] { 4, 5 }, output.ToArray());

        stream.Dispose();
        stream.Dispose();
        Assert.Equal(1, closes);
    }
}
