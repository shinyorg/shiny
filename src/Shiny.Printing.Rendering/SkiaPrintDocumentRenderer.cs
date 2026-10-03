using Shiny.Printers;
using Shiny.Printers.Document;
using Shiny.Printers.Imaging;
using SkiaSharp;

namespace Shiny.Printing.Rendering;


/// <summary>
/// <see cref="IPrintDocumentRenderer"/> that paginates a <see cref="PrintDocument"/> onto PDF pages
/// using SkiaSharp. Text runs, alignment, bold, underline, size magnification, blank feeds and raster
/// images are rendered; barcode / QR elements fall back to printing their data as text (full symbol
/// rendering is a future addition). Paper cuts start a new page.
/// </summary>
public sealed class SkiaPrintDocumentRenderer : IPrintDocumentRenderer
{
    public byte[] RenderToPdf(PrintDocument document, PrintRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var opts = options ?? PrintRenderOptions.A4;

        using var ms = new MemoryStream();
        using (var doc = SKDocument.CreatePdf(ms))
        {
            var layout = new PageLayout(doc, opts);
            var state = new RenderState();
            var line = new List<TextRun>();

            foreach (var element in document.Elements)
            {
                switch (element)
                {
                    case AlignElement a:
                        state.Alignment = a.Alignment;
                        break;
                    case BoldElement b:
                        state.Bold = b.On;
                        break;
                    case UnderlineElement u:
                        state.Underline = u.On;
                        break;
                    case SizeElement s:
                        state.WidthScale = s.WidthScale;
                        state.HeightScale = s.HeightScale;
                        break;
                    case ResetStyleElement:
                        state = new RenderState();
                        break;

                    case TextElement t:
                        line.Add(new TextRun(t.Text, state.Bold, state.Underline, state.WidthScale, state.HeightScale));
                        if (t.NewLine)
                            layout.FlushLine(line, state.Alignment);
                        break;

                    case FeedElement f:
                        layout.FlushLine(line, state.Alignment);
                        for (var i = 0; i < f.Lines; i++)
                            layout.BlankLine();
                        break;

                    case BarcodeElement bc:
                        layout.FlushLine(line, state.Alignment);
                        layout.DrawTextLine($"[{bc.Format}] {bc.Data}", PrintAlignment.Center);
                        break;
                    case QrCodeElement qr:
                        layout.FlushLine(line, state.Alignment);
                        layout.DrawTextLine($"[QR] {qr.Data}", PrintAlignment.Center);
                        break;

                    case ImageElement img:
                        layout.FlushLine(line, state.Alignment);
                        layout.DrawImage(img.Image, state.Alignment);
                        break;

                    case CutElement:
                        layout.FlushLine(line, state.Alignment);
                        layout.NewPage();
                        break;

                    // RawElement and anything else have no visual PDF representation.
                }
            }

            layout.FlushLine(line, state.Alignment);
            layout.Finish();
        }
        return ms.ToArray();
    }


    sealed class RenderState
    {
        public PrintAlignment Alignment;
        public bool Bold;
        public bool Underline;
        public int WidthScale = 1;
        public int HeightScale = 1;
    }


    readonly record struct TextRun(string Text, bool Bold, bool Underline, int WidthScale, int HeightScale);


    /// <summary>Cursor-based page writer: owns the current PDF page and the vertical cursor.</summary>
    sealed class PageLayout
    {
        readonly SKDocument doc;
        readonly PrintRenderOptions opts;
        readonly float usableWidth;
        readonly float bottom;
        SKCanvas? canvas;
        float y;

        public PageLayout(SKDocument doc, PrintRenderOptions opts)
        {
            this.doc = doc;
            this.opts = opts;
            this.usableWidth = opts.PageWidth - (2 * opts.Margin);
            this.bottom = opts.PageHeight - opts.Margin;
        }

        void EnsurePage()
        {
            if (this.canvas != null)
                return;

            this.canvas = this.doc.BeginPage(this.opts.PageWidth, this.opts.PageHeight);
            this.canvas.Clear(SKColors.White);
            this.y = this.opts.Margin;
        }

        public void NewPage()
        {
            if (this.canvas != null)
            {
                this.doc.EndPage();
                this.canvas = null;
            }
            this.EnsurePage();
        }

        public void BlankLine() => this.Advance(this.opts.FontSize * (1 + this.opts.LineSpacing));

        public void DrawTextLine(string text, PrintAlignment alignment)
            => this.FlushLine([new TextRun(text, false, false, 1, 1)], alignment);

        public void FlushLine(List<TextRun> runs, PrintAlignment alignment)
        {
            if (runs.Count == 0)
                return;

            this.EnsurePage();

            var maxHeightScale = runs.Max(r => r.HeightScale);
            var lineHeight = this.opts.FontSize * maxHeightScale * (1 + this.opts.LineSpacing);
            if (this.y + lineHeight > this.bottom)
                this.NewPage();

            // Measure runs so we can position the whole line for centre / right alignment.
            var measured = new List<(TextRun Run, SKFont Font, SKPaint Paint, float Width)>();
            var totalWidth = 0f;
            foreach (var run in runs)
            {
                var (font, paint) = this.CreateFont(run);
                var width = font.MeasureText(run.Text);
                measured.Add((run, font, paint, width));
                totalWidth += width;
            }

            var x = alignment switch
            {
                PrintAlignment.Center => this.opts.Margin + Math.Max(0, (this.usableWidth - totalWidth) / 2),
                PrintAlignment.Right => this.opts.PageWidth - this.opts.Margin - totalWidth,
                _ => this.opts.Margin
            };
            var baseline = this.y + (this.opts.FontSize * maxHeightScale * 0.8f);

            foreach (var (run, font, paint, width) in measured)
            {
                this.canvas!.DrawText(run.Text, x, baseline, SKTextAlign.Left, font, paint);
                if (run.Underline)
                {
                    using var line = new SKPaint { Color = SKColors.Black, StrokeWidth = 1, IsAntialias = true };
                    this.canvas.DrawLine(x, baseline + 2, x + width, baseline + 2, line);
                }
                x += width;
                font.Dispose();
                paint.Dispose();
            }

            this.Advance(lineHeight);
        }

        public void DrawImage(PrinterImage image, PrintAlignment alignment)
        {
            this.EnsurePage();

            using var bitmap = ToBitmap(image);
            var scale = Math.Min(1f, this.usableWidth / image.Width);
            var drawWidth = image.Width * scale;
            var drawHeight = image.Height * scale;

            if (this.y + drawHeight > this.bottom)
                this.NewPage();

            var x = alignment switch
            {
                PrintAlignment.Center => this.opts.Margin + Math.Max(0, (this.usableWidth - drawWidth) / 2),
                PrintAlignment.Right => this.opts.PageWidth - this.opts.Margin - drawWidth,
                _ => this.opts.Margin
            };

            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
            this.canvas!.DrawBitmap(bitmap, new SKRect(x, this.y, x + drawWidth, this.y + drawHeight), sampling);
            this.Advance(drawHeight + (this.opts.FontSize * this.opts.LineSpacing));
        }

        public void Finish()
        {
            if (this.canvas != null)
            {
                this.doc.EndPage();
                this.canvas = null;
            }
            this.doc.Close();
        }

        void Advance(float amount)
        {
            this.y += amount;
            if (this.y > this.bottom)
                this.NewPage();
        }

        (SKFont Font, SKPaint Paint) CreateFont(TextRun run)
        {
            var typeface = SKTypeface.FromFamilyName(
                this.opts.FontFamily,
                run.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright
            );
            var font = new SKFont(typeface, this.opts.FontSize * run.HeightScale)
            {
                ScaleX = (float)run.WidthScale / run.HeightScale
            };
            var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            return (font, paint);
        }

        static SKBitmap ToBitmap(PrinterImage image)
        {
            var bitmap = new SKBitmap(image.Width, image.Height, SKColorType.Gray8, SKAlphaType.Opaque);
            for (var row = 0; row < image.Height; row++)
            {
                for (var col = 0; col < image.Width; col++)
                {
                    var bit = (image.Bits[(row * image.BytesPerRow) + (col / 8)] >> (7 - (col % 8))) & 1;
                    var shade = (byte)(bit == 1 ? 0 : 255);
                    bitmap.SetPixel(col, row, new SKColor(shade, shade, shade));
                }
            }
            return bitmap;
        }
    }
}
