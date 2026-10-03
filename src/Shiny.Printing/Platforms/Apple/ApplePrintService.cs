using System.Collections.Generic;
using CoreFoundation;
using Foundation;
using UIKit;

namespace Shiny.Printing;


/// <summary>
/// <see cref="IPrintService"/> backed by AirPrint (<see cref="UIPrintInteractionController"/>). Presents
/// the system print sheet by default; can print silently to a previously-picked printer when
/// <see cref="PrintOptions.PreferSilent"/> is set and <see cref="PrintOptions.PrinterId"/> is a
/// <see cref="UIPrinter"/> URL. Printer enumeration is not offered by the OS without UI.
/// </summary>
public sealed class ApplePrintService : IPrintService
{
    public PrintingCapabilities Capabilities =>
        PrintingCapabilities.Pdf |
        PrintingCapabilities.Image |
        PrintingCapabilities.Html |
        PrintingCapabilities.File |
        PrintingCapabilities.SystemDialog |
        PrintingCapabilities.Silent;


    public Task<PrintResult> Print(PrintJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var tcs = new TaskCompletionSource<PrintResult>();
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            try
            {
                var controller = UIPrintInteractionController.SharedPrintController;
                controller.PrintInfo = BuildPrintInfo(job);

                if (!TryAssignContent(controller, job, out var error))
                {
                    tcs.TrySetResult(PrintResult.Failed(error!));
                    return;
                }

                void Completion(UIPrintInteractionController? _, bool completed, NSError? err)
                {
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
            catch (Exception ex)
            {
                tcs.TrySetResult(PrintResult.Failed(ex.Message));
            }
        });
        return tcs.Task;
    }


    public Task<IReadOnlyList<PrinterInfo>> GetPrinters(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PrinterInfo>>([]);


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


    static bool TryAssignContent(UIPrintInteractionController controller, PrintJob job, out string? error)
    {
        error = null;
        var kind = job.Kind == PrintContentKind.File ? job.ResolveFileKind() : job.Kind;

        switch (kind)
        {
            case PrintContentKind.Pdf when job.Kind == PrintContentKind.File:
            case PrintContentKind.Image when job.Kind == PrintContentKind.File:
                controller.PrintingItem = NSUrl.FromFilename(job.Text!);
                return true;

            case PrintContentKind.Pdf:
                controller.PrintingItem = NSData.FromArray(job.Bytes.ToArray());
                return true;

            case PrintContentKind.Image:
                var image = UIImage.LoadFromData(NSData.FromArray(job.Bytes.ToArray()));
                if (image == null)
                {
                    error = "Image data could not be decoded.";
                    return false;
                }
                controller.PrintingItem = image;
                return true;

            case PrintContentKind.Html:
                controller.PrintFormatter = new UIMarkupTextPrintFormatter(job.Text!);
                return true;

            case PrintContentKind.HtmlUrl:
                // A live web page needs a WKWebView print formatter; a bare URL isn't a printing item.
                error = "Printing a web URL is not supported on this platform; pass rendered HTML via PrintJob.Html.";
                return false;

            default:
                error = $"Unsupported content kind '{kind}'.";
                return false;
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
