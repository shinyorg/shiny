using System.Collections.Generic;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using UIKit;
using WebKit;

namespace Shiny.Printing;


/// <summary>
/// <see cref="IPrintService"/> backed by AirPrint (<see cref="UIPrintInteractionController"/>). Presents
/// the system print sheet by default; can print silently to a previously-picked printer when
/// <see cref="PrintOptions.PreferSilent"/> is set and <see cref="PrintOptions.PrinterId"/> is a
/// <see cref="UIPrinter"/> URL. Printer enumeration is not offered by the OS without UI. HTML renders
/// through an off-screen <see cref="WKWebView"/> so flexbox and CSS page breaks survive printing.
/// </summary>
public sealed class ApplePrintService : IPrintService
{
    // US Letter in points; the print formatter re-lays the page out for the chosen paper
    static readonly CGRect WebViewFrame = new(0, 0, 612, 792);

    public PrintingCapabilities Capabilities =>
        PrintingCapabilities.Pdf |
        PrintingCapabilities.Image |
        PrintingCapabilities.Html |
        PrintingCapabilities.File |
        PrintingCapabilities.SystemDialog |
        PrintingCapabilities.Silent |
        PrintingCapabilities.HtmlToPdf;


    public Task<PrintResult> Print(PrintJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var tcs = new TaskCompletionSource<PrintResult>();
        DispatchQueue.MainQueue.DispatchAsync(async () =>
        {
            try
            {
                var controller = UIPrintInteractionController.SharedPrintController;
                controller.PrintInfo = BuildPrintInfo(job);

                // held until the print sheet completes so its formatter stays valid
                var (webView, error) = await TryAssignContent(controller, job, cancellationToken);
                if (error != null)
                {
                    tcs.TrySetResult(PrintResult.Failed(error));
                    return;
                }

                void Completion(UIPrintInteractionController? _, bool completed, NSError? err)
                {
                    GC.KeepAlive(webView);
                    if (err != null)
                        tcs.TrySetResult(PrintResult.Failed(err.LocalizedDescription));
                    else
                        tcs.TrySetResult(completed ? PrintResult.Completed() : PrintResult.Cancelled());
                }

                var silentPrinter = ResolveSilentPrinter(job.Options);
                if (silentPrinter != null)
                    controller.PrintToPrinter(silentPrinter, Completion);
                else
                    controller.Present(true, Completion);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetResult(PrintResult.Cancelled());
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(PrintResult.Failed(ex.Message));
            }
        });
        return tcs.Task;
    }


    public Task<byte[]> HtmlToPdf(string html, PdfPageOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(html);
        var page = options ?? PdfPageOptions.A4;
        var paper = new CGRect(0, 0, page.LaidOutWidth, page.LaidOutHeight);

        var tcs = new TaskCompletionSource<byte[]>();
        DispatchQueue.MainQueue.DispatchAsync(async () =>
        {
            try
            {
                var webView = await HtmlWebView.LoadHtml(html, paper, cancellationToken);
                tcs.TrySetResult(RenderPdf(webView, paper, page.Margin));
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }


    public Task<IReadOnlyList<PrinterInfo>> GetPrinters(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PrinterInfo>>([]);


    /// <summary>
    /// Paginates the loaded page with the same formatter AirPrint uses, drawing each page into a PDF
    /// context. The formatter does not apply CSS <c>@page</c> margins, so the margin is set as insets.
    /// </summary>
    static byte[] RenderPdf(WKWebView webView, CGRect paper, float margin)
    {
        var formatter = webView.ViewPrintFormatter;
        formatter.PerPageContentInsets = new UIEdgeInsets(margin, margin, margin, margin);

        using var renderer = new UIPrintPageRenderer();
        renderer.AddPrintFormatter(formatter, 0);
        // paperRect / printableRect are read-only; KVC is the documented way to size an offline render
        renderer.SetValueForKey(NSValue.FromCGRect(paper), new NSString("paperRect"));
        renderer.SetValueForKey(NSValue.FromCGRect(paper), new NSString("printableRect"));

        var data = new NSMutableData();
        UIGraphics.BeginPDFContext(data, paper, null);
        var pages = renderer.NumberOfPages;
        renderer.PrepareForDrawingPages(new NSRange(0, pages));
        var bounds = UIGraphics.PDFContextBounds;
        for (nint i = 0; i < pages; i++)
        {
            UIGraphics.BeginPDFPage();
            renderer.DrawPage(i, bounds);
        }
        UIGraphics.EndPDFContext();

        return data.ToArray();
    }


    static UIPrintInfo BuildPrintInfo(PrintJob job)
    {
        var info = UIPrintInfo.PrintInfo;
        info.JobName = job.Options.JobName ?? "Shiny Print";
        info.OutputType = job.Kind == PrintContentKind.Image ? UIPrintInfoOutputType.Photo : UIPrintInfoOutputType.General;

        info.Orientation = job.Options.Orientation == PrintOrientation.Landscape
            ? UIPrintInfoOrientation.Landscape
            : UIPrintInfoOrientation.Portrait;

        info.Duplex = job.Options.Duplex switch
        {
            PrintDuplex.OneSided => UIPrintInfoDuplex.None,
            PrintDuplex.TwoSidedShortEdge => UIPrintInfoDuplex.ShortEdge,
            PrintDuplex.TwoSidedLongEdge => UIPrintInfoDuplex.LongEdge,
            _ => UIPrintInfoDuplex.None
        };
        return info;
    }


    static async Task<(WKWebView? WebView, string? Error)> TryAssignContent(UIPrintInteractionController controller, PrintJob job, CancellationToken cancellationToken)
    {
        PrintContentKind kind;
        try
        {
            kind = job.ResolveFileKind();
        }
        catch (NotSupportedException ex)
        {
            return (null, ex.Message);
        }

        switch (kind)
        {
            case PrintContentKind.Pdf when job.Kind == PrintContentKind.File:
            case PrintContentKind.Image when job.Kind == PrintContentKind.File:
                controller.PrintingItem = NSUrl.FromFilename(job.Text!);
                return (null, null);

            case PrintContentKind.Pdf:
                controller.PrintingItem = NSData.FromArray(job.Bytes.ToArray());
                return (null, null);

            case PrintContentKind.Image:
                var image = UIImage.LoadFromData(NSData.FromArray(job.Bytes.ToArray()));
                if (image == null)
                    return (null, "Image data could not be decoded.");

                controller.PrintingItem = image;
                return (null, null);

            case PrintContentKind.Html:
                var htmlView = await HtmlWebView.LoadHtml(job.ReadHtml(), WebViewFrame, cancellationToken);
                controller.PrintFormatter = htmlView.ViewPrintFormatter;
                return (htmlView, null);

            case PrintContentKind.HtmlUrl:
                var urlView = await HtmlWebView.LoadUrl(job.Uri!, WebViewFrame, cancellationToken);
                controller.PrintFormatter = urlView.ViewPrintFormatter;
                return (urlView, null);

            default:
                return (null, $"Unsupported content kind '{kind}'.");
        }
    }


    static UIPrinter? ResolveSilentPrinter(PrintOptions options)
    {
        if (!options.PreferSilent || string.IsNullOrWhiteSpace(options.PrinterId))
            return null;

        var url = NSUrl.FromString(options.PrinterId);
        return url == null ? null : UIPrinter.FromUrl(url);
    }
}
