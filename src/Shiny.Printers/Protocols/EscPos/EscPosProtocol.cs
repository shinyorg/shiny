using System.Text;
using Shiny.Printers.Document;
using Shiny.Printers.Imaging;

namespace Shiny.Printers.Protocols.EscPos;


/// <summary>
/// Encodes a <see cref="PrintDocument"/> as ESC/POS - the de-facto standard for thermal receipt printers.
/// Covers text styling, alignment, 1D barcodes, QR codes, raster images and paper cut.
/// </summary>
public sealed class EscPosProtocol : IPrinterProtocol
{
    const byte ESC = 0x1B;
    const byte GS = 0x1D;
    const byte LF = 0x0A;

    /// <summary>Text encoding used for character data. Defaults to ASCII; set a code-page encoding for extended characters.</summary>
    public Encoding Encoding { get; set; } = Encoding.ASCII;

    /// <summary>
    /// Code page selected via <c>ESC t n</c> at the start of every document. 0 = PC437 (USA/Standard Europe).
    /// Set this to match <see cref="Encoding"/> when printing extended characters.
    /// </summary>
    public byte CodePage { get; set; } = 0;

    public string Name => "ESC/POS";


    public byte[] Encode(PrintDocument document, PrinterCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(capabilities);

        var buffer = new CommandBuffer();

        // Initialise: ESC @  then select code page: ESC t n
        buffer.Write(ESC, (byte)'@');
        buffer.Write(ESC, (byte)'t', this.CodePage);

        foreach (var element in document.Elements)
            this.EncodeElement(buffer, element, capabilities);

        return buffer.ToArray();
    }


    void EncodeElement(CommandBuffer buffer, IPrintElement element, PrinterCapabilities capabilities)
    {
        switch (element)
        {
            case AlignElement a:
                buffer.Write(ESC, (byte)'a', (byte)a.Alignment);
                break;

            case BoldElement b:
                buffer.Write(ESC, (byte)'E', (byte)(b.On ? 1 : 0));
                break;

            case UnderlineElement u:
                buffer.Write(ESC, (byte)'-', (byte)(u.On ? 1 : 0));
                break;

            case SizeElement s:
                // GS ! n : high nibble = width-1, low nibble = height-1
                var n = (byte)(((s.WidthScale - 1) << 4) | (s.HeightScale - 1));
                buffer.Write(GS, (byte)'!', n);
                break;

            case ResetStyleElement:
                buffer.Write(ESC, (byte)'@');
                buffer.Write(ESC, (byte)'t', this.CodePage);
                break;

            case TextElement t:
                buffer.Write(this.Encoding.GetBytes(t.Text));
                if (t.NewLine)
                    buffer.Write(LF);
                break;

            case FeedElement f:
                EncodeFeed(buffer, f.Lines);
                break;

            case BarcodeElement bc:
                this.EncodeBarcode(buffer, bc);
                break;

            case QrCodeElement qr:
                EncodeQrCode(buffer, qr);
                break;

            case ImageElement img:
                EncodeImage(buffer, img.Image, capabilities);
                break;

            case CutElement c:
                EncodeCut(buffer, c);
                break;

            case RawElement r:
                buffer.Write(r.Data);
                break;

            default:
                throw new NotSupportedException($"ESC/POS encoder does not handle element '{element.GetType().Name}'.");
        }
    }


    static void EncodeFeed(CommandBuffer buffer, int lines)
    {
        // ESC d n  (n = 0..255)
        while (lines > 0)
        {
            var chunk = (byte)Math.Min(lines, 255);
            buffer.Write(ESC, (byte)'d', chunk);
            lines -= chunk;
        }
    }


    void EncodeBarcode(CommandBuffer buffer, BarcodeElement bc)
    {
        // HRI text position: GS H n
        buffer.Write(GS, (byte)'H', (byte)bc.TextPosition);
        // Barcode height: GS h n
        buffer.Write(GS, (byte)'h', (byte)Math.Clamp(bc.Height, 1, 255));
        // Module width: GS w n  (2..6 typical)
        buffer.Write(GS, (byte)'w', (byte)Math.Clamp(bc.ModuleWidth, 2, 6));

        var (code, data) = MapBarcode(bc.Format, bc.Data, this.Encoding);

        // GS k m n d1..dn   (function B form, m = 65..73)
        buffer.Write(GS, (byte)'k', code, (byte)data.Length);
        buffer.Write(data);
    }


    static (byte Code, byte[] Data) MapBarcode(BarcodeFormat format, string text, Encoding encoding)
    {
        // Function-B selectors (GS k m ...). See Epson ESC/POS spec.
        switch (format)
        {
            case BarcodeFormat.UpcA: return (65, encoding.GetBytes(text));
            case BarcodeFormat.UpcE: return (66, encoding.GetBytes(text));
            case BarcodeFormat.Ean13: return (67, encoding.GetBytes(text));
            case BarcodeFormat.Ean8: return (68, encoding.GetBytes(text));
            case BarcodeFormat.Code39: return (69, encoding.GetBytes(text));
            case BarcodeFormat.Itf: return (70, encoding.GetBytes(text));
            case BarcodeFormat.Codabar: return (71, encoding.GetBytes(text));
            case BarcodeFormat.Code93: return (72, encoding.GetBytes(text));
            case BarcodeFormat.Code128:
                // CODE128 data must carry an embedded code-set selector. Escape literal braces, then
                // default everything to code set B. (For explicit code-set control, emit bytes via Raw.)
                var escaped = "{B" + text.Replace("{", "{{");
                return (73, encoding.GetBytes(escaped));
            default:
                throw new NotSupportedException($"Unsupported barcode format '{format}'.");
        }
    }


    static void EncodeQrCode(CommandBuffer buffer, QrCodeElement qr)
    {
        var data = Encoding.UTF8.GetBytes(qr.Data);

        // Select model 2:  GS ( k 04 00 31 41 50 00
        buffer.Write(GS, (byte)'(', (byte)'k', 0x04, 0x00, 0x31, 0x41, 0x32, 0x00);

        // Module size:     GS ( k 03 00 31 43 n
        buffer.Write(GS, (byte)'(', (byte)'k', 0x03, 0x00, 0x31, 0x43, (byte)qr.ModuleSize);

        // Error correction: GS ( k 03 00 31 45 n   (48=L,49=M,50=Q,51=H)
        var ec = qr.Correction switch
        {
            QrCorrectionLevel.Low => (byte)48,
            QrCorrectionLevel.Medium => (byte)49,
            QrCorrectionLevel.Quartile => (byte)50,
            QrCorrectionLevel.High => (byte)51,
            _ => (byte)49
        };
        buffer.Write(GS, (byte)'(', (byte)'k', 0x03, 0x00, 0x31, 0x45, ec);

        // Store data:      GS ( k pL pH 31 50 30 d1..dn   (length = data + 3)
        var len = data.Length + 3;
        buffer.Write(GS, (byte)'(', (byte)'k', (byte)(len & 0xFF), (byte)((len >> 8) & 0xFF), 0x31, 0x50, 0x30);
        buffer.Write(data);

        // Print:           GS ( k 03 00 31 51 30
        buffer.Write(GS, (byte)'(', (byte)'k', 0x03, 0x00, 0x31, 0x51, 0x30);
    }


    static void EncodeImage(CommandBuffer buffer, PrinterImage image, PrinterCapabilities capabilities)
    {
        if (!capabilities.SupportsImages)
            throw new NotSupportedException("This printer does not support raster images.");

        // GS v 0 m xL xH yL yH  d1..dk
        var bytesPerRow = image.BytesPerRow;
        var height = image.Height;

        buffer.Write(GS, (byte)'v', (byte)'0', 0x00);
        buffer.Write((byte)(bytesPerRow & 0xFF), (byte)((bytesPerRow >> 8) & 0xFF));
        buffer.Write((byte)(height & 0xFF), (byte)((height >> 8) & 0xFF));
        buffer.Write(image.Bits);
    }


    static void EncodeCut(CommandBuffer buffer, CutElement cut)
    {
        EncodeFeed(buffer, cut.FeedBefore);
        // GS V m  : 0 = full cut, 1 = partial cut
        buffer.Write(GS, (byte)'V', (byte)(cut.Mode == CutMode.Full ? 0 : 1));
    }
}
