using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;

namespace Shiny.Printing;


/// <summary>
/// <see cref="IPrintService"/> for Windows. Images print via GDI+ (<see cref="PrintDocument"/>) directly
/// to the chosen or default printer; PDFs are handed to the registered PDF handler through the shell
/// <c>print</c>/<c>printto</c> verb. Printer enumeration uses <see cref="PrinterSettings.InstalledPrinters"/>.
/// HTML printing requires WebView2 host integration and is not offered here.
/// </summary>
public sealed class WindowsPrintService : IPrintService
{
    public PrintingCapabilities Capabilities =>
        PrintingCapabilities.Pdf |
        PrintingCapabilities.Image |
        PrintingCapabilities.File |
        PrintingCapabilities.Silent |
        PrintingCapabilities.EnumeratePrinters;


    public Task<PrintResult> Print(PrintJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        PrintContentKind kind;
        try
        {
            kind = job.ResolveFileKind();
        }
        catch (NotSupportedException ex)
        {
            return Task.FromResult(PrintResult.Failed(ex.Message));
        }

        return kind switch
        {
            PrintContentKind.Image => Task.FromResult(PrintImage(job)),
            PrintContentKind.Pdf => Task.FromResult(PrintPdfViaShell(job)),
            _ => Task.FromResult(PrintResult.Failed("HTML printing on Windows requires WebView2 integration; render to PDF or an image first."))
        };
    }


    public Task<IReadOnlyList<PrinterInfo>> GetPrinters(CancellationToken cancellationToken = default)
    {
        var defaultName = new PrinterSettings().PrinterName;
        var printers = new List<PrinterInfo>();
        foreach (string name in PrinterSettings.InstalledPrinters)
            printers.Add(new PrinterInfo(name, name, string.Equals(name, defaultName, StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult<IReadOnlyList<PrinterInfo>>(printers);
    }


    static PrintResult PrintImage(PrintJob job)
    {
        var bytes = job.Kind == PrintContentKind.File ? System.IO.File.ReadAllBytes(job.Text!) : job.Bytes.ToArray();
        using var ms = new MemoryStream(bytes);
        using var image = Image.FromStream(ms);
        using var doc = new PrintDocument();

        if (!string.IsNullOrWhiteSpace(job.Options.PrinterId))
            doc.PrinterSettings.PrinterName = job.Options.PrinterId;
        doc.PrinterSettings.Copies = (short)Math.Clamp(job.Options.Copies, 1, short.MaxValue);
        doc.DefaultPageSettings.Landscape = job.Options.Orientation == PrintOrientation.Landscape;

        if (!doc.PrinterSettings.IsValid)
            return PrintResult.Failed($"Printer '{doc.PrinterSettings.PrinterName}' is not valid.");

        doc.PrintPage += (_, e) =>
        {
            var area = e.MarginBounds;
            var ratio = Math.Min((float)area.Width / image.Width, (float)area.Height / image.Height);
            var width = (int)(image.Width * ratio);
            var height = (int)(image.Height * ratio);
            e.Graphics!.DrawImage(image, area.X, area.Y, width, height);
            e.HasMorePages = false;
        };
        doc.Print();
        return PrintResult.Submitted(doc.PrinterSettings.PrinterName);
    }


    static PrintResult PrintPdfViaShell(PrintJob job)
    {
        // ShellExecute needs a file path; buffer our own bytes when necessary. The handler reads the
        // file asynchronously, so we intentionally don't delete a temp file here.
        var path = job.Kind == PrintContentKind.File
            ? job.Text!
            : WriteTemp(job.Bytes.ToArray());

        try
        {
            var psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                CreateNoWindow = true
            };
            if (!string.IsNullOrWhiteSpace(job.Options.PrinterId))
            {
                psi.Verb = "printto";
                psi.Arguments = $"\"{job.Options.PrinterId}\"";
            }
            else
            {
                psi.Verb = "print";
            }

            Process.Start(psi);
            return PrintResult.Submitted(job.Options.PrinterId);
        }
        catch (Exception ex)
        {
            return PrintResult.Failed(ex.Message);
        }
    }


    static string WriteTemp(byte[] data)
    {
        var path = Path.Combine(Path.GetTempPath(), $"shiny-print-{Guid.NewGuid():N}.pdf");
        System.IO.File.WriteAllBytes(path, data);
        return path;
    }
}
