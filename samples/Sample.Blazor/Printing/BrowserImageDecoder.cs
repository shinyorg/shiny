using Microsoft.JSInterop;
using Shiny.Printers;
using Shiny.Printers.Imaging;

namespace Sample.Blazor.Printing;


/// <summary>
/// Decodes an image to raw RGBA pixels using the browser's own canvas, then hands them to
/// <see cref="PrinterImage.FromPixels"/> for monochrome conversion.
/// </summary>
/// <remarks>
/// The dependency-free <c>Shiny.Printers</c> core deliberately does not decode PNG/JPEG - that is the
/// caller's job. The MAUI sample uses SkiaSharp; on the web the browser is already a decoder, so no
/// imaging library needs to ship in the WASM payload.
/// </remarks>
public sealed class BrowserImageDecoder(IJSRuntime jsRuntime) : IAsyncDisposable
{
    const string ModulePath = "./printing-interop.js";
    IJSObjectReference? module;

    sealed record DecodedImage(int Width, int Height, byte[] Rgba);


    /// <summary>
    /// Decodes <paramref name="url"/> and converts it for the printhead, scaled down to fit
    /// <paramref name="maxWidth"/> dots. Returns null when the image cannot be decoded.
    /// </summary>
    public async Task<PrinterImage?> Decode(string url, int maxWidth, ImageDithering dithering = ImageDithering.FloydSteinberg)
    {
        var js = await this.GetModule().ConfigureAwait(false);
        var decoded = await js.InvokeAsync<DecodedImage?>("decodeImage", url, maxWidth).ConfigureAwait(false);

        return decoded is null
            ? null
            : PrinterImage.FromPixels(decoded.Rgba, decoded.Width, decoded.Height, dithering);
    }


    async Task<IJSObjectReference> GetModule()
        => this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).ConfigureAwait(false);


    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync().ConfigureAwait(false);
    }
}
