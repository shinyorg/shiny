using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Pdf;
using Android.Print;
using Android.Webkit;
using Shiny;

namespace Shiny.Printing;


/// <summary>
/// <see cref="IPrintService"/> backed by Android's <see cref="PrintManager"/>. Always presents the
/// system print UI (Android has no silent print API). PDFs and images print via a
/// <see cref="PrintDocumentAdapter"/>; HTML renders through an off-screen <see cref="WebView"/>.
/// Layout options (orientation/duplex/colour/copies) are chosen by the user in the system dialog.
/// </summary>
/// <remarks>
/// The dialog is hosted by Shiny's current activity, which <see cref="AndroidPlatform"/> tracks from app startup.
/// Print while the app is in the foreground; with no activity at all the result is <see cref="PrintStatus.Failed"/>.
/// </remarks>
public sealed class AndroidPrintService(AndroidPlatform platform) : IPrintService
{
    public PrintingCapabilities Capabilities =>
        PrintingCapabilities.Pdf |
        PrintingCapabilities.Image |
        PrintingCapabilities.Html |
        PrintingCapabilities.File |
        PrintingCapabilities.SystemDialog;


    public Task<PrintResult> Print(PrintJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var activity = platform.CurrentActivity;
        if (activity == null)
            return Task.FromResult(PrintResult.Failed("No foreground Android activity is available to host the print dialog."));

        if (activity.GetSystemService(Context.PrintService) is not PrintManager manager)
            return Task.FromResult(PrintResult.Failed("PrintManager is unavailable on this device."));

        PrintContentKind kind;
        try
        {
            kind = job.ResolveFileKind();
        }
        catch (NotSupportedException ex)
        {
            return Task.FromResult(PrintResult.Failed(ex.Message));
        }

        var jobName = job.Options.JobName ?? "Shiny Print";
        var tcs = new TaskCompletionSource<PrintResult>();

        activity.RunOnUiThread(() =>
        {
            try
            {
                switch (kind)
                {
                    case PrintContentKind.Html:
                        PrintHtml(activity, manager, job.Text!, jobName, tcs);
                        break;

                    case PrintContentKind.HtmlUrl:
                        tcs.TrySetResult(PrintResult.Failed("Printing a web URL is not supported; pass rendered HTML via PrintJob.Html."));
                        break;

                    case PrintContentKind.Pdf:
                    case PrintContentKind.Image:
                        var pdf = BuildPdf(job, kind);
                        manager.Print(jobName, new PdfBytesPrintDocumentAdapter(jobName, pdf), null);
                        tcs.TrySetResult(PrintResult.Submitted());
                        break;

                    default:
                        tcs.TrySetResult(PrintResult.Failed($"Unsupported content kind '{kind}'."));
                        break;
                }
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


    static byte[] BuildPdf(PrintJob job, PrintContentKind kind)
    {
        // A real PDF (bytes or file) passes straight through; an image is wrapped in a one-page PDF.
        if (kind == PrintContentKind.Pdf)
            return job.Kind == PrintContentKind.File ? System.IO.File.ReadAllBytes(job.Text!) : job.Bytes.ToArray();

        var imageBytes = job.Kind == PrintContentKind.File ? System.IO.File.ReadAllBytes(job.Text!) : job.Bytes.ToArray();
        using var bitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length)
            ?? throw new InvalidOperationException("Image data could not be decoded.");

        using var document = new PdfDocument();
        var pageInfo = new PdfDocument.PageInfo.Builder(bitmap.Width, bitmap.Height, 1).Create();
        var page = document.StartPage(pageInfo)!;
        page.Canvas!.DrawBitmap(bitmap, 0, 0, null);
        document.FinishPage(page);

        using var ms = new MemoryStream();
        document.WriteTo(ms);
        document.Close();
        return ms.ToArray();
    }


    static void PrintHtml(Android.App.Activity activity, PrintManager manager, string html, string jobName, TaskCompletionSource<PrintResult> tcs)
    {
        // WebView must finish loading before it can produce a print adapter; keep a reference alive.
        var webView = new WebView(activity);
        webView.SetWebViewClient(new PrintOnLoadWebViewClient(manager, jobName, tcs, webView));
        webView.LoadDataWithBaseURL(null, html, "text/html", "UTF-8", null);
    }


    sealed class PrintOnLoadWebViewClient(PrintManager manager, string jobName, TaskCompletionSource<PrintResult> tcs, WebView webView) : WebViewClient
    {
        public override void OnPageFinished(WebView? view, string? url)
        {
            try
            {
                var adapter = webView.CreatePrintDocumentAdapter(jobName);
                manager.Print(jobName, adapter, null);
                tcs.TrySetResult(PrintResult.Submitted());
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(PrintResult.Failed(ex.Message));
            }
        }
    }
}
