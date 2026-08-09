using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Common.Tests.Infrastructure;
using Shiny.BluetoothLE.Hosting;
using Xunit;

namespace Shiny.BluetoothLE.Common.Tests;


public class L2CapFileServerTests
{
    static readonly L2CapTransferOptions Fast = new()
    {
        BufferSize = 512,
        ProgressInterval = TimeSpan.FromMilliseconds(1),
        IdleTimeout = TimeSpan.FromSeconds(10)
    };


    /// <summary>
    /// Spins up the directory backed server on the responder end of an in-memory link.
    /// </summary>
    static (LoopbackL2Cap Link, FakeBleHostingManager Hosting, L2CapFileServerOptions Options) Server(
        string root,
        Action<L2CapFileServerOptions>? configure = null
    )
    {
        var link = new LoopbackL2Cap();
        var hosting = new FakeBleHostingManager(link.Responder);
        var options = new L2CapFileServerOptions(root) { Transfer = Fast };
        configure?.Invoke(options);

        return (link, hosting, options);
    }


    [Fact]
    public async Task Upload_LandsInTheRootDirectory()
    {
        using var temp = new TempDirectory();
        using var source = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path);
        using var _ = link;

        var completed = new TaskCompletionSource<L2CapFileServerResult>();
        options.OnCompleted = completed.SetResult;

        var content = TempDirectory.RandomBytes(10_000);
        var local = source.WriteFile("payload.bin", content);

        var instance = await hosting.OpenL2CapFileServer(options);
        var result = await link.Initiator.UploadFile(local, options: Fast);
        var served = await completed.Task;

        Assert.Equal(content, File.ReadAllBytes(temp.File("payload.bin")));
        Assert.Equal(content.Length, result.BytesTransferred);
        Assert.Equal(temp.File("payload.bin"), served.LocalFilePath);
        Assert.Equal("initiator-peer", served.PeerIdentifier);

        instance.Dispose();
        Assert.True(hosting.IsClosed);
    }


    [Fact]
    public async Task Download_ServesFromTheRootDirectory()
    {
        using var temp = new TempDirectory();
        using var landing = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path);
        using var _ = link;

        var content = TempDirectory.RandomBytes(10_000);
        temp.WriteFile("served.bin", content);

        await hosting.OpenL2CapFileServer(options);
        var result = await link.Initiator.DownloadFile("served.bin", landing.File("got.bin"), options: Fast);

        Assert.Equal(content, File.ReadAllBytes(landing.File("got.bin")));
        Assert.Equal(content.Length, result.BytesTransferred);
    }


    [Fact]
    public async Task Download_ReportsProgressAndCompletion()
    {
        using var temp = new TempDirectory();
        using var landing = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path);
        using var _ = link;

        var events = new List<L2CapFileTransferEvent>();
        var completed = new TaskCompletionSource<L2CapFileServerResult>();
        options.OnProgress = events.Add;
        options.OnCompleted = completed.SetResult;

        var content = TempDirectory.RandomBytes(30_000);
        temp.WriteFile("served.bin", content);

        await hosting.OpenL2CapFileServer(options);
        await link.Initiator.DownloadFile("served.bin", landing.File("got.bin"), options: Fast);
        var served = await completed.Task;

        Assert.NotEmpty(events);
        Assert.All(events, e => Assert.Equal("served.bin", e.FileName));
        Assert.All(events, e => Assert.Equal(L2CapTransferType.Download, e.Type));
        Assert.Equal(content.Length, events[^1].Progress.BytesTransferred);
        Assert.Equal(content.Length, served.Result.BytesTransferred);
    }


    [Fact]
    public async Task Download_OfMissingFile_IsRefusedAsNotFound()
    {
        using var temp = new TempDirectory();
        using var landing = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path);
        using var _ = link;

        await hosting.OpenL2CapFileServer(options);
        var ex = await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.DownloadFile("nope.bin", landing.File("got.bin"), options: Fast)
        );

        Assert.Equal(L2CapTransferError.NotFound, ex.Error);
    }


    [Theory]
    [InlineData("../escape.bin")]
    [InlineData("sub/../../escape.bin")]
    [InlineData("/etc/passwd")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PeerSuppliedNamesCannotEscapeTheRoot(string fileName)
    {
        using var temp = new TempDirectory();
        using var source = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path);
        using var _ = link;

        var local = source.WriteFile("payload.bin", TempDirectory.RandomBytes(64));

        await hosting.OpenL2CapFileServer(options);

        // an empty/whitespace name never makes it onto the wire - the protocol rejects it locally
        var ex = await Record.ExceptionAsync(
            () => link.Initiator.UploadFile(local, fileName, options: Fast)
        );

        Assert.NotNull(ex);
        if (ex is L2CapTransferException transfer)
            Assert.Equal(L2CapTransferError.NotPermitted, transfer.Error);
        else
            Assert.IsType<ArgumentException>(ex);

        Assert.Empty(Directory.GetFiles(temp.Path));
    }


    [Fact]
    public async Task Upload_OverTheSizeLimit_IsRefusedBeforeAnyBodyBytesMove()
    {
        using var temp = new TempDirectory();
        using var source = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path, o => o.MaxUploadSize = 1_000);
        using var _ = link;

        var local = source.WriteFile("big.bin", TempDirectory.RandomBytes(5_000));

        await hosting.OpenL2CapFileServer(options);
        var ex = await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.UploadFile(local, options: Fast)
        );

        Assert.Equal(L2CapTransferError.TooLarge, ex.Error);
        Assert.Empty(Directory.GetFiles(temp.Path));

        // the refusal costs a request and an error frame, nowhere near the 5k body
        Assert.True(link.BytesOnTheWire < 500, $"{link.BytesOnTheWire} bytes crossed the wire");
    }


    [Fact]
    public async Task Upload_WhenDisabled_IsRefused()
    {
        using var temp = new TempDirectory();
        using var source = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path, o => o.AllowUploads = false);
        using var _ = link;

        var local = source.WriteFile("payload.bin", TempDirectory.RandomBytes(64));

        await hosting.OpenL2CapFileServer(options);
        var ex = await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.UploadFile(local, options: Fast)
        );

        Assert.Equal(L2CapTransferError.NotPermitted, ex.Error);
    }


    [Fact]
    public async Task Download_WhenDisabled_IsRefused()
    {
        using var temp = new TempDirectory();
        using var landing = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path, o => o.AllowDownloads = false);
        using var _ = link;

        temp.WriteFile("served.bin", TempDirectory.RandomBytes(64));

        await hosting.OpenL2CapFileServer(options);
        var ex = await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.DownloadFile("served.bin", landing.File("got.bin"), options: Fast)
        );

        Assert.Equal(L2CapTransferError.NotPermitted, ex.Error);
    }


    [Fact]
    public async Task Upload_OverExistingFile_IsRefusedWhenOverwriteIsOff()
    {
        using var temp = new TempDirectory();
        using var source = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path, o => o.OverwriteExistingUploads = false);
        using var _ = link;

        var existing = TempDirectory.RandomBytes(32);
        temp.WriteFile("payload.bin", existing);
        var local = source.WriteFile("payload.bin", TempDirectory.RandomBytes(64));

        await hosting.OpenL2CapFileServer(options);
        var ex = await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.UploadFile(local, options: Fast)
        );

        Assert.Equal(L2CapTransferError.NotPermitted, ex.Error);
        Assert.Equal(existing, File.ReadAllBytes(temp.File("payload.bin")));
    }


    [Fact]
    public async Task AuthorizeHook_CanRefuseARequest()
    {
        using var temp = new TempDirectory();
        using var source = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path, o => o.Authorize = r => r.FileName.EndsWith(".ok"));
        using var _ = link;

        var blocked = source.WriteFile("payload.bin", TempDirectory.RandomBytes(64));
        var allowed = source.WriteFile("payload.ok", TempDirectory.RandomBytes(64));

        await hosting.OpenL2CapFileServer(options);

        var ex = await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.UploadFile(blocked, options: Fast)
        );
        Assert.Equal(L2CapTransferError.NotPermitted, ex.Error);

        // and the channel keeps serving after the refusal
        await link.Initiator.UploadFile(allowed, options: Fast);
        Assert.True(File.Exists(temp.File("payload.ok")));
    }


    [Fact]
    public async Task Server_HandlesSequentialTransfersOnOneChannel()
    {
        using var temp = new TempDirectory();
        using var source = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path);
        using var _ = link;

        await hosting.OpenL2CapFileServer(options);

        for (var i = 0; i < 3; i++)
        {
            var content = TempDirectory.RandomBytes(1_000 + i);
            var local = source.WriteFile($"file-{i}.bin", content);

            await link.Initiator.UploadFile(local, options: Fast);
            Assert.Equal(content, File.ReadAllBytes(temp.File($"file-{i}.bin")));
        }

        // ...and a download over the same channel afterwards
        var downloaded = await link.Initiator.DownloadFile("file-1.bin", source.File("back.bin"), options: Fast);
        Assert.Equal(1_001, downloaded.BytesTransferred);
    }


    [Fact]
    public async Task Server_CreatesTheRootDirectory()
    {
        using var temp = new TempDirectory();
        var root = temp.File("nested/store");
        var (link, hosting, options) = Server(root);
        using var _ = link;

        await hosting.OpenL2CapFileServer(options);
        Assert.True(Directory.Exists(root));
    }


    [Fact]
    public async Task Server_PassesTheSecureFlagThrough()
    {
        using var temp = new TempDirectory();
        var (link, hosting, options) = Server(temp.Path, o => o.Secure = true);
        using var _ = link;

        await hosting.OpenL2CapFileServer(options);
        Assert.True(hosting.Secure);
    }


    [Fact]
    public async Task CustomHandler_ReceivesRequests()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();
        var hosting = new FakeBleHostingManager(link.Responder);

        var received = new MemoryStream();
        await hosting.HandleL2CapRequests(
            false,
            async (request, ct) =>
            {
                if (request.Type == L2CapTransferType.Upload)
                    await request.AcceptUpload(received, cancellationToken: ct);
                else
                    await request.Reject(L2CapTransferError.NotPermitted, "read only", ct);
            },
            Fast
        );

        var content = TempDirectory.RandomBytes(2_048);
        var local = temp.WriteFile("payload.bin", content);

        await link.Initiator.UploadFile(local, options: Fast);
        Assert.Equal(content, received.ToArray());

        var ex = await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.DownloadFile("payload.bin", temp.File("got.bin"), options: Fast)
        );
        Assert.Equal(L2CapTransferError.NotPermitted, ex.Error);
    }


    [Fact]
    public async Task CustomHandler_ThatForgetsToAnswer_DoesNotHangThePeer()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();
        var hosting = new FakeBleHostingManager(link.Responder);

        await hosting.HandleL2CapRequests(false, (_, _) => Task.CompletedTask, Fast);

        var local = temp.WriteFile("payload.bin", TempDirectory.RandomBytes(64));
        var ex = await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.UploadFile(local, options: Fast)
        );

        Assert.Equal(L2CapTransferError.Unknown, ex.Error);
    }
}
