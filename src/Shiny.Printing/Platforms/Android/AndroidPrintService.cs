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
/// <see cref="PrintOptions.Orientation"/> sets the dialog's starting paper orientation (on the locale's
/// default Letter / A4 size); duplex, colour and copies are chosen by the user in the system dialog.
/// </summary>
/// <remarks>
/// The dialog is hosted by Shiny's current activity, which <see cref="AndroidPlatform"/> tracks from app startup.
/// Print while the app is in the foreground; with no activity at all the result is <see cref="PrintStatus.Failed"/>.
/// </remarks>
public sealed class AndroidPrintService(AndroidPlatform platform) : IPrintService
{
    // off-screen web views have no window to root them; held here until their page has been handed off
    static readonly HashSet<WebView> loading = [];

    public PrintingCapabilities Capabilities =>
        PrintingCapabilities.Pdf |
        PrintingCapabilities.Image |
        PrintingCapabilities.Html |
        PrintingCapabilities.File |
        PrintingCapabilities.SystemDialog |
        PrintingCapabilities.HtmlToPdf;


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
        var attributes = BuildAttributes(job.Options);
        var tcs = new TaskCompletionSource<PrintResult>();

        activity.RunOnUiThread(() =>
        {
            try
            {
                switch (kind)
                {
                    case PrintContentKind.Html:
                        var html = job.ReadHtml();
                        PrintWebView(activity, manager, jobName, attributes, tcs, wv => wv.LoadDataWithBaseURL(null, html, "text/html", "UTF-8", null));
                        break;

                    case PrintContentKind.HtmlUrl:
                        PrintWebView(activity, manager, jobName, attributes, tcs, wv => wv.LoadUrl(job.Uri!.AbsoluteUri));
                        break;

                    case PrintContentKind.Pdf:
                    case PrintContentKind.Image:
                        var pdf = BuildPdf(job, kind);
                        manager.Print(jobName, new PdfBytesPrintDocumentAdapter(jobName, pdf), attributes);
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


    public Task<byte[]> HtmlToPdf(string html, PdfPageOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(html);
        var page = options ?? PdfPageOptions.A4;
        var markup = HtmlMarkup.WithDefaultPageMargin(html, page.Margin);
        var path = System.IO.Path.Combine(platform.Cache.FullName, $"shiny-print-{Guid.NewGuid():N}.pdf");

        // margins come from CSS @page (the WebView honours it), so the paper itself has none
        var attributes = new PrintAttributes.Builder()
            .SetMediaSize(new PrintAttributes.MediaSize("shiny_pdf", "PDF", ToMils(page.LaidOutWidth), ToMils(page.LaidOutHeight)))!
            .SetResolution(new PrintAttributes.Resolution("pdf", "PDF", 600, 600))!
            .SetMinMargins(PrintAttributes.Margins.NoMargins!)!
            .Build();

        var tcs = new TaskCompletionSource<byte[]>();
        var cancel = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        _ = tcs.Task.ContinueWith(_ => cancel.Dispose(), TaskScheduler.Default);

        platform.InvokeOnMainThread(() =>
        {
            try
            {
                // an activity context when there is one; the WebView never attaches to a window either way
                var webView = new WebView((Context?)platform.CurrentActivity ?? platform.AppContext);
                loading.Add(webView);
                webView.SetWebViewClient(new LoadedWebViewClient(() =>
                {
                    try
                    {
                        WritePdf(
                            webView.CreatePrintDocumentAdapter("Shiny PDF"),
                            attributes,
                            new Java.IO.File(path),
                            () => Finish(webView, () => tcs.TrySetResult(System.IO.File.ReadAllBytes(path))),
                            () => Finish(webView, () => tcs.TrySetException(new InvalidOperationException("The PDF could not be written.")))
                        );
                    }
                    catch (Exception ex)
                    {
                        Finish(webView, () => tcs.TrySetException(ex));
                    }
                }));
                webView.LoadDataWithBaseURL(null, markup, "text/html", "UTF-8", null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;

        void Finish(WebView webView, Action complete)
        {
            try
            {
                complete();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                loading.Remove(webView);
                try { System.IO.File.Delete(path); } catch { }
            }
        }
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


    /// <summary>
    /// The dialog's starting attributes: the locale's default paper turned to the requested orientation,
    /// or null to leave everything to the dialog.
    /// </summary>
    static PrintAttributes? BuildAttributes(PrintOptions options)
    {
        if (options.Orientation == PrintOrientation.Auto)
            return null;

        var paper = LetterCountries.Contains(Java.Util.Locale.Default.Country ?? "")
            ? PrintAttributes.MediaSize.NaLetter!
            : PrintAttributes.MediaSize.IsoA4!;

        var media = options.Orientation == PrintOrientation.Landscape ? paper.AsLandscape() : paper.AsPortrait();
        return new PrintAttributes.Builder().SetMediaSize(media!)!.Build();
    }

    // countries whose default paper is US Letter rather than A4
    static readonly HashSet<string> LetterCountries = ["US", "CA", "MX", "PH", "CL", "CO", "VE", "PR", "GT", "CR", "SV", "PA", "DO"];

    // PrintAttributes sizes are in mils (1/1000 inch); PDF points are 1/72 inch
    static int ToMils(float points) => (int)Math.Round(points * 1000f / 72f);


    static void PrintWebView(Android.App.Activity activity, PrintManager manager, string jobName, PrintAttributes? attributes, TaskCompletionSource<PrintResult> tcs, Action<WebView> load)
    {
        // WebView must finish loading before it can produce a print adapter
        var webView = new WebView(activity);
        loading.Add(webView);
        webView.SetWebViewClient(new LoadedWebViewClient(() =>
        {
            loading.Remove(webView);
            try
            {
                var adapter = webView.CreatePrintDocumentAdapter(jobName);
                manager.Print(jobName, adapter, attributes);
                tcs.TrySetResult(PrintResult.Submitted());
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(PrintResult.Failed(ex.Message));
            }
        }));
        load(webView);
    }


    /// <summary>Calls <c>android.print.ShinyPdfWriter.write</c> (Platforms/Android/Java).</summary>
    static void WritePdf(PrintDocumentAdapter adapter, PrintAttributes attributes, Java.IO.File file, Action onDone, Action onError)
    {
        // FindClass resolves app classes only from the main thread, which is where this runs
        var cls = Android.Runtime.JNIEnv.FindClass("android/print/ShinyPdfWriter");
        try
        {
            var method = Android.Runtime.JNIEnv.GetStaticMethodID(
                cls,
                "write",
                "(Landroid/print/PrintDocumentAdapter;Landroid/print/PrintAttributes;Ljava/io/File;Ljava/lang/Runnable;Ljava/lang/Runnable;)V"
            );
            Android.Runtime.JNIEnv.CallStaticVoidMethod(
                cls,
                method,
                new Android.Runtime.JValue(adapter),
                new Android.Runtime.JValue(attributes),
                new Android.Runtime.JValue(file),
                new Android.Runtime.JValue(new Callback(onDone)),
                new Android.Runtime.JValue(new Callback(onError))
            );
        }
        finally
        {
            Android.Runtime.JNIEnv.DeleteGlobalRef(cls);
        }
    }


    sealed class Callback(Action action) : Java.Lang.Object, Java.Lang.IRunnable
    {
        public void Run() => action();
    }


    /// <summary>Fires once, when the page has finished loading (redirects and sub-frames can repeat the callback).</summary>
    sealed class LoadedWebViewClient(Action onLoaded) : WebViewClient
    {
        bool done;

        public override void OnPageFinished(WebView? view, string? url)
        {
            if (this.done)
                return;

            this.done = true;
            onLoaded();
        }
    }
}
