using Microsoft.JSInterop;

namespace Shiny.Printing.Blazor;


/// <summary>
/// <see cref="IPrintService"/> for Blazor (WebAssembly / Server) that drives the browser print dialog
/// via a JS module. PDF, image and HTML content are rendered into a hidden iframe and printed with
/// <c>window.print()</c>. The browser always shows its own dialog, so silent printing and printer
/// enumeration are not available.
/// </summary>
public sealed class BlazorPrintService(IJSRuntime jsRuntime) : IPrintService, IAsyncDisposable
{
    const string ModulePath = "./_content/Shiny.Printing.Blazor/shiny-printing.js";
    IJSObjectReference? module;

    public PrintingCapabilities Capabilities =>
        PrintingCapabilities.Pdf |
        PrintingCapabilities.Image |
        PrintingCapabilities.Html |
        PrintingCapabilities.SystemDialog;


    public async Task<PrintResult> Print(PrintJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        try
        {
            var js = await this.GetModule(cancellationToken).ConfigureAwait(false);
            switch (job.Kind)
            {
                case PrintContentKind.Pdf:
                    await js.InvokeVoidAsync("printPdf", cancellationToken, Convert.ToBase64String(job.Bytes.Span)).ConfigureAwait(false);
                    break;

                case PrintContentKind.Image:
                    await js.InvokeVoidAsync("printImage", cancellationToken, Convert.ToBase64String(job.Bytes.Span), GuessImageMime(job.Bytes.Span)).ConfigureAwait(false);
                    break;

                case PrintContentKind.Html:
                    await js.InvokeVoidAsync("printHtml", cancellationToken, job.Text).ConfigureAwait(false);
                    break;

                case PrintContentKind.HtmlUrl:
                    await js.InvokeVoidAsync("printUrl", cancellationToken, job.Uri!.ToString()).ConfigureAwait(false);
                    break;

                default:
                    return PrintResult.Failed("Printing a local file is not supported in the browser; use PrintJob.Pdf / Image / Html.");
            }

            return PrintResult.Submitted();
        }
        catch (Exception ex)
        {
            return PrintResult.Failed(ex.Message);
        }
    }


    public Task<byte[]> HtmlToPdf(string html, PdfPageOptions? options = null, CancellationToken cancellationToken = default)
        => throw new PlatformNotSupportedException("HTML to PDF is not supported in the browser.");


    public Task<IReadOnlyList<PrinterInfo>> GetPrinters(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PrinterInfo>>([]);


    async Task<IJSObjectReference> GetModule(CancellationToken cancellationToken)
        => this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath).ConfigureAwait(false);


    static string GuessImageMime(ReadOnlySpan<byte> data)
    {
        // PNG magic number 0x89 'P' 'N' 'G'; everything else defaults to JPEG.
        if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return "image/png";
        return "image/jpeg";
    }


    public async ValueTask DisposeAsync()
    {
        if (this.module != null)
        {
            try
            {
                await this.module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // Circuit already gone - nothing to release.
            }
        }
    }
}
