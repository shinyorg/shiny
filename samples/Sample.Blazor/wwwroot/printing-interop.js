// Sample-only helper: decode an image with the browser's canvas and hand raw RGBA back to .NET, so
// PrinterImage.FromPixels can do the monochrome conversion without shipping an imaging library to WASM.

export async function decodeImage(url, maxWidth) {
    const response = await fetch(url);
    if (!response.ok)
        return null;

    const bitmap = await createImageBitmap(await response.blob());

    // A raster wider than the printhead simply will not print - scale to fit, preserving aspect.
    const scale = Math.min(1, maxWidth / bitmap.width);
    const width = Math.max(1, Math.round(bitmap.width * scale));
    const height = Math.max(1, Math.round(bitmap.height * scale));

    const canvas = new OffscreenCanvas(width, height);
    const ctx = canvas.getContext("2d");

    // Thermal output is 1bpp, so flatten transparency onto white rather than letting it read as black.
    ctx.fillStyle = "#ffffff";
    ctx.fillRect(0, 0, width, height);
    ctx.drawImage(bitmap, 0, 0, width, height);
    bitmap.close();

    // getImageData returns RGBA in exactly the byte order PrinterImage.FromPixels expects.
    return { width, height, rgba: ctx.getImageData(0, 0, width, height).data };
}
