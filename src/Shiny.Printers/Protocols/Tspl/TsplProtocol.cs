using System.Globalization;
using System.Text;
using Shiny.Printers.Document;

namespace Shiny.Printers.Protocols.Tspl;


/// <summary>
/// A deliberately minimal TSPL (TSC Printer Language) encoder for label printers. It exists to prove the
/// <see cref="IPrinterProtocol"/> seam is real - not to be a complete TSPL implementation. It lays out
/// text and barcodes top-to-bottom and throws for elements TSPL handles very differently (images, cut).
/// </summary>
public sealed class TsplProtocol(int labelWidthMm = 50, int labelHeightMm = 30, int gapMm = 3) : IPrinterProtocol
{
    public string Name => "TSPL";


    public byte[] Encode(PrintDocument document, PrinterCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(document);

        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;
        sb.Append(ci, $"SIZE {labelWidthMm} mm, {labelHeightMm} mm\r\n");
        sb.Append(ci, $"GAP {gapMm} mm, 0 mm\r\n");
        sb.Append("CLS\r\n");

        var y = 10;        // current vertical cursor in dots
        const int x = 10;  // fixed left margin in dots

        foreach (var element in document.Elements)
        {
            switch (element)
            {
                case TextElement t when !string.IsNullOrEmpty(t.Text):
                    sb.Append(ci, $"TEXT {x},{y},\"3\",0,1,1,\"{Escape(t.Text)}\"\r\n");
                    y += 32;
                    break;

                case TextElement:
                case AlignElement:
                case BoldElement:
                case UnderlineElement:
                case SizeElement:
                case ResetStyleElement:
                    // No-op / styling not modelled in this minimal stub.
                    break;

                case FeedElement f:
                    y += 32 * Math.Max(1, f.Lines);
                    break;

                case BarcodeElement bc:
                    sb.Append(ci, $"BARCODE {x},{y},\"128\",{bc.Height},1,0,2,2,\"{Escape(bc.Data)}\"\r\n");
                    y += bc.Height + 20;
                    break;

                case RawElement r:
                    sb.Append(Encoding.ASCII.GetString(r.Data));
                    break;

                case QrCodeElement qr:
                    sb.Append(ci, $"QRCODE {x},{y},M,{qr.ModuleSize},A,0,\"{Escape(qr.Data)}\"\r\n");
                    y += qr.ModuleSize * 25;
                    break;

                default:
                    throw new NotSupportedException(
                        $"TSPL stub does not implement element '{element.GetType().Name}'. " +
                        "Use the ESC/POS protocol or extend TsplProtocol.");
            }
        }

        sb.Append("PRINT 1,1\r\n");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }


    static string Escape(string value) => value.Replace("\"", "\\\"");
}
