namespace Shiny.Printing;


/// <summary>
/// A unit of work handed to <see cref="IPrintService.Print"/>. Build one with a factory method for the
/// content you have - the least-common-denominator sources every OS print pipeline understands:
/// <see cref="Pdf(ReadOnlyMemory{byte}, PrintOptions?)"/>, <see cref="Image(ReadOnlyMemory{byte}, PrintOptions?)"/>,
/// <see cref="Html(string, PrintOptions?)"/> / <see cref="HtmlUrl(Uri, PrintOptions?)"/>, or
/// <see cref="File(string, PrintOptions?)"/> (routed by extension).
/// </summary>
public sealed class PrintJob
{
    PrintJob(PrintContentKind kind, PrintOptions options)
    {
        this.Kind = kind;
        this.Options = options;
    }

    /// <summary>The content type carried by this job.</summary>
    public PrintContentKind Kind { get; }

    /// <summary>Per-job options (never null; defaults present the system dialog).</summary>
    public PrintOptions Options { get; }

    /// <summary>Raw payload for <see cref="PrintContentKind.Pdf"/> / <see cref="PrintContentKind.Image"/>.</summary>
    internal ReadOnlyMemory<byte> Bytes { get; private init; }

    /// <summary>HTML markup (<see cref="PrintContentKind.Html"/>) or a file path (<see cref="PrintContentKind.File"/>).</summary>
    internal string? Text { get; private init; }

    /// <summary>Target URI for <see cref="PrintContentKind.HtmlUrl"/>.</summary>
    internal Uri? Uri { get; private init; }

    // ---- factories --------------------------------------------------------------------------------

    /// <summary>A PDF document supplied as bytes.</summary>
    public static PrintJob Pdf(ReadOnlyMemory<byte> data, PrintOptions? options = null)
    {
        if (data.IsEmpty)
            throw new ArgumentException("PDF data is empty.", nameof(data));

        return new(PrintContentKind.Pdf, options ?? new()) { Bytes = data };
    }

    /// <summary>A PDF document read fully from <paramref name="stream"/>.</summary>
    public static PrintJob Pdf(Stream stream, PrintOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Pdf(ms.ToArray(), options);
    }

    /// <summary>A raster image (PNG or JPEG) supplied as bytes.</summary>
    public static PrintJob Image(ReadOnlyMemory<byte> data, PrintOptions? options = null)
    {
        if (data.IsEmpty)
            throw new ArgumentException("Image data is empty.", nameof(data));

        return new(PrintContentKind.Image, options ?? new()) { Bytes = data };
    }

    /// <summary>HTML markup to render and print.</summary>
    public static PrintJob Html(string html, PrintOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(html);
        return new(PrintContentKind.Html, options ?? new()) { Text = html };
    }

    /// <summary>A web page URL to load and print.</summary>
    public static PrintJob HtmlUrl(Uri url, PrintOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(url);
        return new(PrintContentKind.HtmlUrl, options ?? new()) { Uri = url };
    }

    /// <summary>A file on disk. The effective content type is inferred from the extension.</summary>
    public static PrintJob File(string path, PrintOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return new(PrintContentKind.File, options ?? new()) { Text = path };
    }

    /// <summary>The markup of an <see cref="PrintContentKind.Html"/> job, read from disk for an <c>.html</c> file job.</summary>
    internal string ReadHtml() => this.Kind == PrintContentKind.File ? System.IO.File.ReadAllText(this.Text!) : this.Text!;


    /// <summary>
    /// Maps a <see cref="PrintContentKind.File"/> job to the concrete kind implied by its extension
    /// (<c>.pdf</c> → Pdf, image extensions → Image, <c>.htm/.html</c> → Html). Throws for anything else.
    /// </summary>
    internal PrintContentKind ResolveFileKind()
    {
        if (this.Kind != PrintContentKind.File)
            return this.Kind;

        var ext = Path.GetExtension(this.Text!).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => PrintContentKind.Pdf,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" or ".heic" => PrintContentKind.Image,
            ".htm" or ".html" => PrintContentKind.Html,
            _ => throw new NotSupportedException($"Cannot print file with extension '{ext}'.")
        };
    }
}
