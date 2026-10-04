package android.print;

import android.os.CancellationSignal;
import android.os.ParcelFileDescriptor;
import java.io.File;

/**
 * Drives a PrintDocumentAdapter (a WebView's) straight into a PDF file, with no print dialog. It lives in
 * android.print because the LayoutResultCallback / WriteResultCallback constructors are package-private,
 * so they cannot be subclassed from C#. Called from AndroidPrintService.HtmlToPdf via JNI.
 */
public final class ShinyPdfWriter {
    private ShinyPdfWriter() { }

    public static void write(final PrintDocumentAdapter adapter, final PrintAttributes attributes, final File file,
                             final Runnable onDone, final Runnable onError) {
        adapter.onStart();
        adapter.onLayout(null, attributes, new CancellationSignal(), new PrintDocumentAdapter.LayoutResultCallback() {
            @Override
            public void onLayoutFinished(PrintDocumentInfo info, boolean changed) {
                final ParcelFileDescriptor fd;
                try {
                    fd = ParcelFileDescriptor.open(file,
                        ParcelFileDescriptor.MODE_CREATE | ParcelFileDescriptor.MODE_TRUNCATE | ParcelFileDescriptor.MODE_READ_WRITE);
                } catch (Exception e) {
                    adapter.onFinish();
                    onError.run();
                    return;
                }
                adapter.onWrite(new PageRange[] { PageRange.ALL_PAGES }, fd, new CancellationSignal(),
                    new PrintDocumentAdapter.WriteResultCallback() {
                        @Override
                        public void onWriteFinished(PageRange[] pages) { finish(fd, adapter); onDone.run(); }

                        @Override
                        public void onWriteFailed(CharSequence error) { finish(fd, adapter); onError.run(); }

                        @Override
                        public void onWriteCancelled() { finish(fd, adapter); onError.run(); }
                    });
            }

            @Override
            public void onLayoutFailed(CharSequence error) { adapter.onFinish(); onError.run(); }

            @Override
            public void onLayoutCancelled() { adapter.onFinish(); onError.run(); }
        }, null);
    }

    private static void finish(ParcelFileDescriptor fd, PrintDocumentAdapter adapter) {
        try { fd.close(); } catch (Exception ignored) { }
        adapter.onFinish();
    }
}
