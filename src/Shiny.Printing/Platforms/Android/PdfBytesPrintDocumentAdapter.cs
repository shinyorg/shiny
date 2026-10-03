using Android.OS;
using Android.Print;

namespace Shiny.Printing;


/// <summary>
/// A <see cref="PrintDocumentAdapter"/> that streams a ready-made PDF byte buffer to the print spooler.
/// Images are converted to a single-page PDF before being handed here (see <see cref="AndroidPrintService"/>).
/// </summary>
sealed class PdfBytesPrintDocumentAdapter(string jobName, byte[] pdf) : PrintDocumentAdapter
{
    public override void OnLayout(
        PrintAttributes? oldAttributes,
        PrintAttributes? newAttributes,
        CancellationSignal? cancellationSignal,
        LayoutResultCallback? callback,
        Bundle? extras
    )
    {
        if (cancellationSignal?.IsCanceled == true)
        {
            callback?.OnLayoutCancelled();
            return;
        }

        var info = new PrintDocumentInfo
            .Builder(jobName)
            .SetContentType(PrintContentType.Document)
            .Build();

        callback?.OnLayoutFinished(info, changed: true);
    }


    public override void OnWrite(
        PageRange[]? pages,
        ParcelFileDescriptor? destination,
        CancellationSignal? cancellationSignal,
        WriteResultCallback? callback
    )
    {
        if (destination == null || callback == null)
            return;

        try
        {
            using (var output = new Java.IO.FileOutputStream(destination.FileDescriptor))
                output.Write(pdf);

            callback.OnWriteFinished([PageRange.AllPages!]);
        }
        catch (Exception ex)
        {
            callback.OnWriteFailed(ex.Message);
        }
    }
}
