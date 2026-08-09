using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Common.Tests.Infrastructure;
using Xunit;

namespace Shiny.BluetoothLE.Common.Tests;


public class L2CapFileTransferTests
{
    static readonly L2CapTransferOptions Fast = new()
    {
        BufferSize = 512,
        ProgressInterval = TimeSpan.FromMilliseconds(1),
        IdleTimeout = TimeSpan.FromSeconds(10)
    };


    /// <summary>
    /// Runs the responder side once - reads a single request and hands it to the caller's handler.
    /// </summary>
    static Task<T> Serve<T>(L2CapChannel responder, Func<L2CapFileRequest, Task<T>> handler)
        => Task.Run(async () =>
        {
            var request = await responder.ReadFileRequest(Fast);
            Assert.NotNull(request);
            return await handler(request!);
        });


    // -------- upload --------

    [Fact]
    public async Task Upload_RoundTrips()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var content = TempDirectory.RandomBytes(20_000);
        var source = temp.WriteFile("source.bin", content);
        var landing = temp.File("landing.bin");

        var serving = Serve(link.Responder, r => r.AcceptUpload(landing));
        var result = await link.Initiator.UploadFile(source, options: Fast);
        var served = await serving;

        Assert.Equal(content, File.ReadAllBytes(landing));
        Assert.Equal(L2CapTransferType.Upload, result.Type);
        Assert.Equal("source.bin", result.FileName);
        Assert.Equal(content.Length, result.BytesTransferred);
        Assert.Equal(content.Length, served.BytesTransferred);
        Assert.Equal("source.bin", served.FileName);
    }


    [Fact]
    public async Task Upload_UsesSuppliedRemoteName()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var source = temp.WriteFile("local-name.bin", TempDirectory.RandomBytes(64));
        string? seenName = null;

        var serving = Serve(link.Responder, r =>
        {
            seenName = r.FileName;
            return r.AcceptUpload(new MemoryStream());
        });
        var result = await link.Initiator.UploadFile(source, "remote-name.bin", options: Fast);
        await serving;

        Assert.Equal("remote-name.bin", seenName);
        Assert.Equal("remote-name.bin", result.FileName);
    }


    [Fact]
    public async Task Upload_EmptyFile_RoundTrips()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var source = temp.WriteFile("empty.bin", Array.Empty<byte>());
        var landing = temp.File("landing.bin");

        var serving = Serve(link.Responder, r => r.AcceptUpload(landing));
        var result = await link.Initiator.UploadFile(source, options: Fast);
        await serving;

        Assert.True(File.Exists(landing));
        Assert.Empty(File.ReadAllBytes(landing));
        Assert.Equal(0, result.BytesTransferred);
    }


    [Fact]
    public async Task Upload_ReassemblesFragmentedWrites()
    {
        // the real link fragments at the MTU - 7 bytes is deliberately hostile
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap(fragmentSize: 7);

        var content = TempDirectory.RandomBytes(5_000);
        var source = temp.WriteFile("source.bin", content);
        var landing = temp.File("landing.bin");

        var serving = Serve(link.Responder, r => r.AcceptUpload(landing));
        await link.Initiator.UploadFile(source, options: Fast);
        await serving;

        Assert.Equal(content, File.ReadAllBytes(landing));
    }


    [Fact]
    public async Task Upload_ReportsProgress()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var content = TempDirectory.RandomBytes(50_000);
        var source = temp.WriteFile("source.bin", content);

        var sender = new List<TransferProgress>();
        var receiver = new List<TransferProgress>();

        var serving = Serve(link.Responder, r => r.AcceptUpload(new MemoryStream(), receiver.Add));
        await link.Initiator.UploadFile(source, onProgress: sender.Add, options: Fast);
        await serving;

        foreach (var events in new[] { sender, receiver })
        {
            Assert.NotEmpty(events);

            // every event knows the total, so percent complete is real on both ends
            Assert.All(events, p => Assert.True(p.IsDeterministic));
            Assert.All(events, p => Assert.Equal(content.Length, p.BytesToTransfer));

            // bytes only ever move forward and finish exactly on the total
            var transferred = events.Select(x => x.BytesTransferred).ToList();
            Assert.Equal(transferred.OrderBy(x => x).ToList(), transferred);
            Assert.Equal(content.Length, events[^1].BytesTransferred);
            Assert.Equal(1.0, events[^1].PercentComplete);
            Assert.Equal(TimeSpan.Zero, events[^1].EstimatedTimeRemaining);
        }
    }


    // -------- download --------

    [Fact]
    public async Task Download_RoundTrips()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var content = TempDirectory.RandomBytes(20_000);
        var served = temp.WriteFile("served.bin", content);
        var landing = temp.File("landing.bin");

        var serving = Serve(link.Responder, r =>
        {
            Assert.Equal(L2CapTransferType.Download, r.Type);
            Assert.Equal("served.bin", r.FileName);
            return r.AcceptDownload(served);
        });
        var result = await link.Initiator.DownloadFile("served.bin", landing, options: Fast);
        await serving;

        Assert.Equal(content, File.ReadAllBytes(landing));
        Assert.Equal(L2CapTransferType.Download, result.Type);
        Assert.Equal(content.Length, result.BytesTransferred);
    }


    [Fact]
    public async Task Download_ReportsProgress()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var content = TempDirectory.RandomBytes(50_000);
        var served = temp.WriteFile("served.bin", content);
        var events = new List<TransferProgress>();

        var serving = Serve(link.Responder, r => r.AcceptDownload(served));
        await link.Initiator.DownloadFile("served.bin", temp.File("landing.bin"), events.Add, Fast);
        await serving;

        Assert.NotEmpty(events);
        Assert.All(events, p => Assert.Equal(content.Length, p.BytesToTransfer));
        Assert.Equal(content.Length, events[^1].BytesTransferred);
        Assert.Equal(1.0, events[^1].PercentComplete);
    }


    [Fact]
    public async Task Download_ToStream_RoundTrips()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var content = TempDirectory.RandomBytes(4_096);
        var destination = new MemoryStream();

        var serving = Serve(link.Responder, r => r.AcceptDownload(new MemoryStream(content), content.Length));
        await link.Initiator.DownloadFile("whatever.bin", destination, options: Fast);
        await serving;

        Assert.Equal(content, destination.ToArray());
    }


    // -------- rejection --------

    [Fact]
    public async Task Reject_SurfacesErrorCodeToInitiator()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var serving = Task.Run(async () =>
        {
            var request = await link.Responder.ReadFileRequest(Fast);
            await request!.Reject(L2CapTransferError.NotFound, "no such file");
        });

        var ex = await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.DownloadFile("missing.bin", temp.File("landing.bin"), options: Fast)
        );
        await serving;

        Assert.Equal(L2CapTransferError.NotFound, ex.Error);
        Assert.Equal("no such file", ex.Message);
    }


    [Fact]
    public async Task Reject_LeavesNoPartialLocalFile()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var landing = temp.File("landing.bin");
        var serving = Task.Run(async () =>
        {
            var request = await link.Responder.ReadFileRequest(Fast);
            await request!.Reject(L2CapTransferError.NotFound);
        });

        await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.DownloadFile("missing.bin", landing, options: Fast)
        );
        await serving;

        Assert.False(File.Exists(landing));
    }


    [Fact]
    public async Task Reject_KeepsChannelUsableForTheNextRequest()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var content = TempDirectory.RandomBytes(2_048);
        var source = temp.WriteFile("source.bin", content);
        var landing = temp.File("landing.bin");

        var serving = Task.Run(async () =>
        {
            var rejected = await link.Responder.ReadFileRequest(Fast);
            await rejected!.Reject(L2CapTransferError.NotFound);

            var accepted = await link.Responder.ReadFileRequest(Fast);
            await accepted!.AcceptUpload(landing);
        });

        await Assert.ThrowsAsync<L2CapTransferException>(
            () => link.Initiator.DownloadFile("missing.bin", temp.File("nope.bin"), options: Fast)
        );
        await link.Initiator.UploadFile(source, options: Fast);
        await serving;

        Assert.Equal(content, File.ReadAllBytes(landing));
    }


    // -------- sequencing --------

    [Fact]
    public async Task MultipleTransfers_ShareOneChannel()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var files = Enumerable
            .Range(0, 4)
            .Select(i => (Name: $"file-{i}.bin", Content: TempDirectory.RandomBytes(1_000 + i * 700)))
            .ToList();

        foreach (var f in files)
            temp.WriteFile(f.Name, f.Content);

        var serving = Task.Run(async () =>
        {
            for (var i = 0; i < files.Count; i++)
            {
                var request = await link.Responder.ReadFileRequest(Fast);
                await request!.AcceptUpload(temp.File("received-" + request.FileName));
            }
        });

        foreach (var f in files)
            await link.Initiator.UploadFile(temp.File(f.Name), options: Fast);

        await serving;

        foreach (var f in files)
            Assert.Equal(f.Content, File.ReadAllBytes(temp.File("received-" + f.Name)));
    }


    [Fact]
    public async Task ReadFileRequest_ReturnsNullWhenPeerClosesChannel()
    {
        using var link = new LoopbackL2Cap();

        var reading = link.Responder.ReadFileRequest(Fast);
        link.CloseInitiator();

        Assert.Null(await reading);
    }


    [Fact]
    public async Task AnsweringTwice_Throws()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var source = temp.WriteFile("source.bin", TempDirectory.RandomBytes(128));
        var serving = Task.Run(async () =>
        {
            var request = await link.Responder.ReadFileRequest(Fast);
            await request!.AcceptUpload(new MemoryStream());
            return await Record.ExceptionAsync(() => request.Reject());
        });

        await link.Initiator.UploadFile(source, options: Fast);
        var ex = await serving;

        Assert.IsType<InvalidOperationException>(ex);
    }


    [Fact]
    public async Task AnsweringWithTheWrongDirection_Throws()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var source = temp.WriteFile("source.bin", TempDirectory.RandomBytes(128));
        var serving = Task.Run(async () =>
        {
            var request = await link.Responder.ReadFileRequest(Fast);
            // it's an upload request, so AcceptDownload is the wrong answer
            var ex = await Record.ExceptionAsync(() => request!.AcceptDownload(new MemoryStream(), 0));
            await request!.Reject(L2CapTransferError.Unknown);
            return ex;
        });

        await Assert.ThrowsAsync<L2CapTransferException>(() => link.Initiator.UploadFile(source, options: Fast));
        Assert.IsType<InvalidOperationException>(await serving);
    }


    // -------- failure handling --------

    [Fact]
    public async Task DownloadTruncatedByPeer_ThrowsAndDeletesPartialFile()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var landing = temp.File("landing.bin");
        var serving = Task.Run(async () =>
        {
            var request = await link.Responder.ReadFileRequest(Fast);
            // announce 10,000 bytes but only ever send 100, then hang up
            var partial = new MemoryStream(TempDirectory.RandomBytes(100));
            var accepting = request!.AcceptDownload(partial, 10_000);
            await Record.ExceptionAsync(() => accepting);
            link.CloseResponder();
        });

        await Assert.ThrowsAnyAsync<Exception>(
            () => link.Initiator.DownloadFile("served.bin", landing, options: Fast)
        );
        await serving;

        Assert.False(File.Exists(landing));
    }


    [Fact]
    public async Task UploadOfMissingFile_ThrowsBeforeTouchingTheChannel()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => link.Initiator.UploadFile(temp.File("nope.bin"), options: Fast)
        );
        Assert.Equal(0, link.BytesOnTheWire);
    }


    [Fact]
    public async Task IdleTimeout_AbandonsAStalledTransfer()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var options = Fast with { IdleTimeout = TimeSpan.FromMilliseconds(250) };
        var landing = temp.File("landing.bin");

        // nobody is serving, so the Accept never arrives
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<TimeoutException>(
            () => link.Initiator.DownloadFile("served.bin", landing, options: options)
        );
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"timed out too slowly: {sw.Elapsed}");
    }


    [Fact]
    public async Task Cancellation_StopsTheTransfer()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => link.Initiator.DownloadFile("served.bin", temp.File("landing.bin"), options: Fast, cancellationToken: cts.Token)
        );
    }


    // -------- Rx surface --------

    [Fact]
    public async Task UploadFileWithProgress_EmitsProgressThenCompletes()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var content = TempDirectory.RandomBytes(30_000);
        var source = temp.WriteFile("source.bin", content);

        var serving = Serve(link.Responder, r => r.AcceptUpload(new MemoryStream()));
        var events = await link.Initiator
            .UploadFileWithProgress(source, options: Fast)
            .ToList()
            .ToTask();
        await serving;

        Assert.NotEmpty(events);
        Assert.Equal(content.Length, events[^1].BytesTransferred);
        Assert.Equal(1.0, events[^1].PercentComplete);
    }


    [Fact]
    public async Task DownloadFileWithProgress_EmitsProgressThenCompletes()
    {
        using var temp = new TempDirectory();
        using var link = new LoopbackL2Cap();

        var content = TempDirectory.RandomBytes(30_000);
        var served = temp.WriteFile("served.bin", content);

        var serving = Serve(link.Responder, r => r.AcceptDownload(served));
        var events = await link.Initiator
            .DownloadFileWithProgress("served.bin", temp.File("landing.bin"), Fast)
            .ToList()
            .ToTask();
        await serving;

        Assert.NotEmpty(events);
        Assert.Equal(content.Length, events[^1].BytesTransferred);
        Assert.Equal(content, File.ReadAllBytes(temp.File("landing.bin")));
    }
}
