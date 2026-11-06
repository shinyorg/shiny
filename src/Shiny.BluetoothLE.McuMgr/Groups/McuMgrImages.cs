using System.Diagnostics;
using System.Security.Cryptography;

namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// An image slot as reported by the device.
/// </summary>
/// <param name="Image">The image number (0 for single-core parts; 1 is the network core on an nRF5340).</param>
/// <param name="Slot">The slot - 0 is primary (running), 1 is secondary (the staged upgrade).</param>
/// <param name="Version">The image version, e.g. <c>1.2.3</c> or <c>1.2.3.4</c>.</param>
/// <param name="Hash">The image hash (the SHA TLV MCUboot computed at build time).</param>
/// <param name="Bootable">The image is bootable.</param>
/// <param name="Pending">The image will be swapped in on the next reset.</param>
/// <param name="Confirmed">The image is confirmed - it stays after the next reset.</param>
/// <param name="Active">The image is running.</param>
/// <param name="Permanent">The pending swap is permanent (confirmed) rather than a test.</param>
public record McuMgrImageSlot(
    int Image,
    int Slot,
    string Version,
    byte[] Hash,
    bool Bootable,
    bool Pending,
    bool Confirmed,
    bool Active,
    bool Permanent
)
{
    /// <summary>The hash as lower case hex.</summary>
    public string HashHex => Convert.ToHexStringLower(this.Hash);
}


/// <summary>
/// Options for an image or file upload.
/// </summary>
public class McuMgrUploadOptions
{
    /// <summary>
    /// The largest frame to send. Null (the default) asks the device for its SMP buffer size and falls back to the
    /// transport's <see cref="ISmpTransport.MaxFrameSize"/> when the device cannot say.
    /// </summary>
    public int? MaxFrameSize { get; set; }

    /// <summary>
    /// Use the device's SMP buffer size (which can exceed the BLE MTU, relying on the device to reassemble frames
    /// split across writes). Turn off for firmware built without <c>CONFIG_MCUMGR_TRANSPORT_BT_REASSEMBLY</c> that
    /// still reports its parameters. Default true.
    /// </summary>
    public bool UseDeviceBufferSize { get; set; } = true;

    /// <summary>
    /// Rounds every chunk except the last down to a multiple of this many bytes (1, 2, 4, 8 or 16). Some flash
    /// drivers need aligned writes. Default 1 (no alignment).
    /// </summary>
    public int DataAlignment { get; set; } = 1;

    /// <summary>
    /// The timeout for the first chunk, which can take several seconds while the device erases the slot.
    /// Default 40 seconds.
    /// </summary>
    public TimeSpan FirstChunkTimeout { get; set; } = TimeSpan.FromSeconds(40);
}


/// <summary>
/// Image management - group 1.
/// </summary>
public sealed class McuMgrImages
{
    readonly McuMgrClient client;
    internal McuMgrImages(McuMgrClient client) => this.client = client;


    /// <summary>
    /// Reads the state of every image slot.
    /// </summary>
    public async Task<IReadOnlyList<McuMgrImageSlot>> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(SmpOperation.Read, McuMgrGroups.Image, 0, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return ParseState(response);
    }


    /// <summary>
    /// Marks the image with <paramref name="hash"/> to be swapped in on the next reset. With
    /// <paramref name="confirm"/> false the swap is a test: MCUboot reverts it on the following reset unless the
    /// new firmware is confirmed. With <paramref name="confirm"/> true the swap is permanent - or, for the image
    /// that is already running, confirms it.
    /// </summary>
    public async Task<IReadOnlyList<McuMgrImageSlot>> SetStateAsync(byte[] hash, bool confirm, CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(
                SmpOperation.Write,
                McuMgrGroups.Image,
                0,
                new Dictionary<string, object?> { ["hash"] = hash, ["confirm"] = confirm },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return ParseState(response);
    }


    /// <summary>
    /// Confirms the running image so MCUboot keeps it.
    /// </summary>
    public async Task<IReadOnlyList<McuMgrImageSlot>> ConfirmRunningAsync(CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(SmpOperation.Write, McuMgrGroups.Image, 0, new Dictionary<string, object?> { ["confirm"] = true }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return ParseState(response);
    }


    /// <summary>
    /// Erases the secondary slot (or <paramref name="slot"/> when given).
    /// </summary>
    public Task EraseAsync(int? slot = null, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>();
        if (slot != null)
            payload["slot"] = slot.Value;

        return this.client.SendAsync(SmpOperation.Write, McuMgrGroups.Image, 5, payload, TimeSpan.FromSeconds(40), cancellationToken);
    }


    /// <summary>
    /// Uploads an image to the secondary slot of <paramref name="image"/>. This does not swap it in - follow with
    /// <see cref="SetStateAsync"/> and a reset, or use <see cref="McuMgrClient.UpgradeFirmwareAsync(FirmwareImage, FirmwareUpgradeOptions?, IProgress{FirmwareUpgradeProgress}?, CancellationToken)"/>.
    /// An interrupted upload of the same file resumes where it stopped.
    /// </summary>
    /// <param name="data">The signed MCUboot image (<c>*.signed.bin</c> / <c>app_update.bin</c>).</param>
    /// <param name="image">The image number.</param>
    /// <param name="upgradeOnly">Ask the device to reject the image unless it is newer than the running one.</param>
    /// <param name="options">Chunking options.</param>
    /// <param name="progress">Upload progress.</param>
    /// <param name="cancellationToken">Cancels the upload.</param>
    public async Task UploadAsync(
        ReadOnlyMemory<byte> data,
        int image = 0,
        bool upgradeOnly = false,
        McuMgrUploadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        options ??= new McuMgrUploadOptions();
        var sha = SHA256.HashData(data.Span);

        await ChunkedUpload.RunAsync(
            this.client,
            McuMgrGroups.Image,
            1,
            data,
            offset =>
            {
                var map = new Dictionary<string, object?>();
                if (offset == 0)
                {
                    if (image != 0)
                        map["image"] = image;

                    map["len"] = data.Length;
                    map["off"] = 0;
                    map["sha"] = sha;
                    if (upgradeOnly)
                        map["upgrade"] = true;
                }
                else
                {
                    map["off"] = offset;
                }
                return map;
            },
            response =>
            {
                if (response.GetBoolean("match") == false)
                    throw new McuMgrException("The device reports the uploaded image does not match its SHA-256");
            },
            options,
            progress,
            cancellationToken
        ).ConfigureAwait(false);
    }


    internal static IReadOnlyList<McuMgrImageSlot> ParseState(IReadOnlyDictionary<string, object?> response) => response
        .GetList("images")
        .OfType<IReadOnlyDictionary<string, object?>>()
        .Select(x => new McuMgrImageSlot(
            (int)(x.GetInt64("image") ?? 0),
            (int)(x.GetInt64("slot") ?? 0),
            x.GetString("version") ?? String.Empty,
            x.GetBytes("hash") ?? [],
            x.GetBoolean("bootable") ?? false,
            x.GetBoolean("pending") ?? false,
            x.GetBoolean("confirmed") ?? false,
            x.GetBoolean("active") ?? false,
            x.GetBoolean("permanent") ?? false
        ))
        .ToList();
}


/// <summary>
/// The offset-driven upload loop shared by image and file uploads.
/// </summary>
static class ChunkedUpload
{
    public static async Task RunAsync(
        McuMgrClient client,
        ushort group,
        byte command,
        ReadOnlyMemory<byte> data,
        Func<long, Dictionary<string, object?>> buildRequest,
        Action<IReadOnlyDictionary<string, object?>>? onLastResponse,
        McuMgrUploadOptions options,
        IProgress<TransferProgress>? progress,
        CancellationToken cancellationToken
    )
    {
        if (options.DataAlignment is not (1 or 2 or 4 or 8 or 16))
            throw new ArgumentException("DataAlignment must be 1, 2, 4, 8 or 16", nameof(options));

        var frameLimit = await client
            .ResolveMaxFrameSizeAsync(options.MaxFrameSize, options.UseDeviceBufferSize, cancellationToken)
            .ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        long offset = 0;
        var stalls = 0;
        IReadOnlyDictionary<string, object?>? response = null;

        progress?.Report(new TransferProgress(0, data.Length, 0));
        while (offset < data.Length)
        {
            var request = buildRequest(offset);
            var length = ComputeChunkLength(request, (int)(data.Length - offset), frameLimit, options.DataAlignment);
            request["data"] = data.Slice((int)offset, length).ToArray();

            response = await client
                .SendAsync(
                    SmpOperation.Write,
                    group,
                    command,
                    request,
                    offset == 0 ? options.FirstChunkTimeout : null,
                    cancellationToken
                )
                .ConfigureAwait(false);

            var next = response.GetInt64("off") ?? throw new McuMgrException("The upload response has no offset");
            if (next < 0 || next > data.Length)
                throw new McuMgrException($"The device returned an invalid upload offset {next}");

            stalls = next == offset ? stalls + 1 : 0;
            if (stalls >= 3)
                throw new McuMgrException($"The device is not accepting data at offset {offset}");

            // a device holding a partial upload of the same image answers the first chunk with its own offset
            offset = next;

            var seconds = stopwatch.Elapsed.TotalSeconds;
            progress?.Report(new TransferProgress(seconds > 0 ? (long)(offset / seconds) : 0, data.Length, offset));
        }

        if (response != null)
            onLastResponse?.Invoke(response);
    }


    /// <summary>
    /// The largest data length that keeps header + CBOR request within <paramref name="frameLimit"/>.
    /// </summary>
    internal static int ComputeChunkLength(Dictionary<string, object?> request, int remaining, int frameLimit, int alignment)
    {
        request["data"] = Array.Empty<byte>();
        var overhead = SmpHeader.Size + SmpCbor.Encode(request).Length;
        var available = frameLimit - overhead;

        // the empty byte string's 1 byte header is already in the overhead - longer strings need more
        var length = available;
        while (length > 0 && length + ExtraLengthBytes(length) > available)
            length--;

        if (length < remaining)
            length -= length % alignment;
        else
            length = remaining;

        if (length <= 0)
            throw new McuMgrException($"A frame size of {frameLimit} bytes leaves no room for data");

        return length;
    }


    static int ExtraLengthBytes(int length) => length switch
    {
        < 24 => 0,
        < 256 => 1,
        < 65536 => 2,
        _ => 4
    };
}
