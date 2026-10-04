using System.Collections.Generic;

namespace Shiny.Printing;


/// <summary>
/// <see cref="IPrintService"/> for CUPS-based systems (Linux, and non-MAUI .NET on macOS) that shells
/// out to the <c>lp</c> / <c>lpstat</c> command-line tools. Dependency-free and AOT / trim safe.
/// Printing is always direct-to-queue (there is no GUI dialog); HTML is not supported.
/// </summary>
public sealed class CupsPrintService(ICupsProcessRunner runner) : IPrintService
{
    public PrintingCapabilities Capabilities =>
        PrintingCapabilities.Pdf |
        PrintingCapabilities.Image |
        PrintingCapabilities.File |
        PrintingCapabilities.Silent |
        PrintingCapabilities.EnumeratePrinters;


    public async Task<PrintResult> Print(PrintJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        PrintContentKind kind;
        try
        {
            kind = job.ResolveFileKind();
        }
        catch (NotSupportedException ex)
        {
            return PrintResult.Failed(ex.Message);
        }

        if (kind is PrintContentKind.Html or PrintContentKind.HtmlUrl)
            return PrintResult.Failed("HTML printing is not supported by the CUPS backend; render it to PDF first (e.g. via Shiny.Printing.Rendering or an external tool).");

        // Resolve the file to hand to lp - either the caller's own file or a temp file we own.
        string filePath;
        var temp = false;
        if (job.Kind == PrintContentKind.File)
        {
            filePath = job.Text!;
        }
        else
        {
            filePath = Path.Combine(Path.GetTempPath(), $"shiny-print-{Guid.NewGuid():N}{ExtensionFor(kind)}");
            await System.IO.File.WriteAllBytesAsync(filePath, job.Bytes.ToArray(), cancellationToken).ConfigureAwait(false);
            temp = true;
        }

        try
        {
            var args = BuildPrintArguments(job.Options, filePath);
            var result = await runner.Run("lp", args, cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
                return PrintResult.Failed($"lp exited {result.ExitCode}: {result.StandardError.Trim()}");

            // lp queues the job and copies the file into the spool before returning.
            return PrintResult.Submitted(job.Options.PrinterId);
        }
        catch (Exception ex)
        {
            return PrintResult.Failed($"Failed to invoke lp: {ex.Message}");
        }
        finally
        {
            if (temp)
                TryDelete(filePath);
        }
    }


    public Task<byte[]> HtmlToPdf(string html, PdfPageOptions? options = null, CancellationToken cancellationToken = default)
        => throw new PlatformNotSupportedException("HTML to PDF is not supported by the CUPS backend.");


    public async Task<IReadOnlyList<PrinterInfo>> GetPrinters(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await runner.Run("lpstat", ["-p", "-d"], cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
                return [];

            return ParsePrinters(result.StandardOutput);
        }
        catch
        {
            return [];
        }
    }


    /// <summary>Builds the <c>lp</c> argument list. Kept static and internal so tests can assert it directly.</summary>
    internal static IReadOnlyList<string> BuildPrintArguments(PrintOptions options, string filePath)
    {
        var args = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.PrinterId))
        {
            args.Add("-d");
            args.Add(options.PrinterId);
        }
        if (options.Copies > 1)
        {
            args.Add("-n");
            args.Add(options.Copies.ToString());
        }
        if (!string.IsNullOrWhiteSpace(options.JobName))
        {
            args.Add("-t");
            args.Add(options.JobName);
        }
        if (options.Orientation != PrintOrientation.Auto)
        {
            // IPP orientation-requested: 3 = portrait, 4 = landscape.
            args.Add("-o");
            args.Add(options.Orientation == PrintOrientation.Landscape ? "orientation-requested=4" : "orientation-requested=3");
        }
        if (options.Duplex != PrintDuplex.Default)
        {
            args.Add("-o");
            args.Add(options.Duplex switch
            {
                PrintDuplex.OneSided => "sides=one-sided",
                PrintDuplex.TwoSidedShortEdge => "sides=two-sided-short-edge",
                _ => "sides=two-sided-long-edge"
            });
        }
        if (options.Color != PrintColorMode.Default)
        {
            args.Add("-o");
            args.Add(options.Color == PrintColorMode.Monochrome ? "print-color-mode=monochrome" : "print-color-mode=color");
        }

        args.Add("--");     // terminate options so a filename starting with '-' is safe
        args.Add(filePath);
        return args;
    }


    /// <summary>Parses <c>lpstat -p -d</c> output into printer info. Internal for testability.</summary>
    internal static IReadOnlyList<PrinterInfo> ParsePrinters(string lpstatOutput)
    {
        string? defaultName = null;
        var names = new List<string>();

        foreach (var raw in lpstatOutput.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("printer ", StringComparison.Ordinal))
            {
                var rest = line["printer ".Length..];
                var space = rest.IndexOf(' ');
                names.Add(space < 0 ? rest : rest[..space]);
            }
            else if (line.StartsWith("system default destination:", StringComparison.Ordinal))
            {
                defaultName = line["system default destination:".Length..].Trim();
            }
        }

        return names
            .Select(n => new PrinterInfo(n, n, n == defaultName))
            .ToList();
    }


    static string ExtensionFor(PrintContentKind kind) => kind == PrintContentKind.Image ? ".img" : ".pdf";

    static void TryDelete(string path)
    {
        try { System.IO.File.Delete(path); } catch { /* best effort */ }
    }
}
