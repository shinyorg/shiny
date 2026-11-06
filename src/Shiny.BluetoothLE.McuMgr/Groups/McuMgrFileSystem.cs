namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// File system management - group 8. Paths are the device's, e.g. <c>/lfs1/log.txt</c>.
/// </summary>
public sealed class McuMgrFileSystem
{
    readonly McuMgrClient client;
    internal McuMgrFileSystem(McuMgrClient client) => this.client = client;


    /// <summary>
    /// Downloads a file from the device into <paramref name="destination"/>.
    /// </summary>
    /// <returns>The number of bytes downloaded.</returns>
    public async Task<long> DownloadAsync(
        string path,
        Stream destination,
        IProgress<TransferProgress>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        long offset = 0;
        long? total = null;
        var stalls = 0;
        var started = DateTimeOffset.UtcNow;

        while (total == null || offset < total)
        {
            var response = await this.client
                .SendAsync(
                    SmpOperation.Read,
                    McuMgrGroups.FileSystem,
                    0,
                    new Dictionary<string, object?> { ["name"] = path, ["off"] = offset },
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);

            total ??= response.GetInt64("len") ?? throw new McuMgrException("The first download response has no file length");

            var data = response.GetBytes("data") ?? [];
            if (data.Length == 0 && offset < total)
            {
                stalls++;
                if (stalls >= 3)
                    throw new McuMgrException($"The device returned no data at offset {offset} of {total}");
            }
            else
            {
                stalls = 0;
                await destination.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                offset += data.Length;

                var seconds = (DateTimeOffset.UtcNow - started).TotalSeconds;
                progress?.Report(new TransferProgress(seconds > 0 ? (long)(offset / seconds) : 0, total, offset));
            }
        }
        return offset;
    }


    /// <summary>
    /// Downloads a file from the device into memory.
    /// </summary>
    public async Task<byte[]> DownloadAsync(string path, IProgress<TransferProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await this.DownloadAsync(path, ms, progress, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }


    /// <summary>
    /// Uploads <paramref name="data"/> to <paramref name="path"/> on the device, replacing any existing file.
    /// </summary>
    public Task UploadAsync(
        string path,
        ReadOnlyMemory<byte> data,
        McuMgrUploadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken cancellationToken = default
    ) => ChunkedUpload.RunAsync(
        this.client,
        McuMgrGroups.FileSystem,
        0,
        data,
        offset =>
        {
            var map = new Dictionary<string, object?> { ["name"] = path, ["off"] = offset };
            if (offset == 0)
                map["len"] = data.Length;
            return map;
        },
        null,
        options ?? new McuMgrUploadOptions { FirstChunkTimeout = TimeSpan.FromSeconds(10) },
        progress,
        cancellationToken
    );


    /// <summary>
    /// Returns the size of a file in bytes.
    /// </summary>
    public async Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(SmpOperation.Read, McuMgrGroups.FileSystem, 1, new Dictionary<string, object?> { ["name"] = path }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.GetInt64("len") ?? 0;
    }


    /// <summary>
    /// Has the device compute the SHA-256 of a file - compare it with a local hash to verify a transfer.
    /// </summary>
    public async Task<byte[]> GetSha256Async(string path, CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(
                SmpOperation.Read,
                McuMgrGroups.FileSystem,
                2,
                new Dictionary<string, object?> { ["name"] = path, ["type"] = "sha256" },
                TimeSpan.FromSeconds(30),
                cancellationToken
            )
            .ConfigureAwait(false);

        return response.GetBytes("output") ?? throw new McuMgrException("The hash response has no output");
    }


    /// <summary>
    /// Has the device compute the IEEE CRC32 of a file.
    /// </summary>
    public async Task<uint> GetCrc32Async(string path, CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(
                SmpOperation.Read,
                McuMgrGroups.FileSystem,
                2,
                new Dictionary<string, object?> { ["name"] = path, ["type"] = "crc32" },
                TimeSpan.FromSeconds(30),
                cancellationToken
            )
            .ConfigureAwait(false);

        return (uint)(response.GetInt64("output") ?? throw new McuMgrException("The checksum response has no output"));
    }


    /// <summary>
    /// Closes any file the device holds open from an earlier transfer.
    /// </summary>
    public Task CloseAsync(CancellationToken cancellationToken = default)
        => this.client.SendAsync(SmpOperation.Write, McuMgrGroups.FileSystem, 4, cancellationToken: cancellationToken);
}
