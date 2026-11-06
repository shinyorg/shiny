using System.IO.Compression;
using System.Text.Json;

namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// A signed MCUboot image to upload, and which image (core) it belongs to.
/// </summary>
public sealed class FirmwareImage
{
    /// <summary>
    /// Wraps a signed MCUboot image.
    /// </summary>
    /// <param name="data">The signed image (<c>zephyr.signed.bin</c>, <c>app_update.bin</c>).</param>
    /// <param name="imageIndex">The image number - 0 for the application core, 1 for an nRF5340 network core.</param>
    /// <exception cref="InvalidDataException">The data is not a signed MCUboot image.</exception>
    public FirmwareImage(byte[] data, int imageIndex = 0)
    {
        this.Data = data ?? throw new ArgumentNullException(nameof(data));
        this.ImageIndex = imageIndex;
        this.Header = McuBootImage.Parse(data);
    }


    /// <summary>The image bytes.</summary>
    public byte[] Data { get; }

    /// <summary>The image number.</summary>
    public int ImageIndex { get; }

    /// <summary>The parsed MCUboot header and hash.</summary>
    public McuBootImage Header { get; }


    /// <summary>
    /// Reads an nRF Connect SDK / Zephyr DFU package (<c>dfu_application.zip</c>): <c>manifest.json</c> lists each
    /// image file and its <c>image_index</c>. SUIT envelopes are not MCUboot images and are rejected.
    /// </summary>
    public static async Task<IReadOnlyList<FirmwareImage>> FromDfuPackageAsync(Stream zip, CancellationToken cancellationToken = default)
    {
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: true);
        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidDataException("The DFU package has no manifest.json");

        using var manifest = await JsonDocument.ParseAsync(manifestEntry.Open(), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!manifest.RootElement.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("The DFU package manifest has no files list");

        var images = new List<FirmwareImage>();
        foreach (var file in files.EnumerateArray())
        {
            var type = file.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (type != null && type.StartsWith("suit", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException($"The DFU package contains a SUIT image ({type}) - SUIT updates are not MCUboot updates");

            var fileName = file.GetProperty("file").GetString()
                ?? throw new InvalidDataException("A DFU manifest entry has no file name");

            var entry = archive.GetEntry(fileName)
                ?? throw new InvalidDataException($"The DFU package is missing {fileName}");

            using var ms = new MemoryStream();
            await using (var stream = entry.Open())
                await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);

            images.Add(new FirmwareImage(ms.ToArray(), ReadImageIndex(file)));
        }

        if (images.Count == 0)
            throw new InvalidDataException("The DFU package lists no images");

        return images;
    }


    // image_index is a string ("0") in the nRF Connect SDK manifests, but accept a number too
    static int ReadImageIndex(JsonElement file)
    {
        if (!file.TryGetProperty("image_index", out var value))
            return 0;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetInt32(),
            JsonValueKind.String when Int32.TryParse(value.GetString(), out var i) => i,
            _ => 0
        };
    }
}
