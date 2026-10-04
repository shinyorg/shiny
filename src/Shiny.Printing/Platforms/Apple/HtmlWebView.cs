using System.Collections.Generic;
using CoreGraphics;
using Foundation;
using WebKit;

namespace Shiny.Printing;


/// <summary>
/// Loads HTML into an off-screen <see cref="WKWebView"/> and completes once it has finished laying out, so
/// its <c>ViewPrintFormatter</c> can be handed to AirPrint or paginated into a PDF. WebKit honours the
/// flexbox and CSS page-break rules that <see cref="UIKit.UIMarkupTextPrintFormatter"/> ignores.
/// Must be started on the main thread.
/// </summary>
sealed class HtmlWebView : WKNavigationDelegate
{
    // keeps each loader (and its web view) alive until navigation finishes
    static readonly HashSet<HtmlWebView> loading = [];

    readonly TaskCompletionSource<WKWebView> tcs = new();
    readonly WKWebView webView;


    HtmlWebView(CGRect frame)
    {
        this.webView = new WKWebView(frame, new WKWebViewConfiguration());
        this.webView.NavigationDelegate = this;
    }


    public static Task<WKWebView> LoadHtml(string html, CGRect frame, CancellationToken cancellationToken = default)
        => Load(frame, wv => wv.LoadHtmlString(new NSString(html), null), cancellationToken);


    public static Task<WKWebView> LoadUrl(Uri url, CGRect frame, CancellationToken cancellationToken = default)
        => Load(frame, wv => wv.LoadRequest(new NSUrlRequest(new NSUrl(url.AbsoluteUri))), cancellationToken);


    static Task<WKWebView> Load(CGRect frame, Action<WKWebView> start, CancellationToken cancellationToken)
    {
        var loader = new HtmlWebView(frame);
        lock (loading)
            loading.Add(loader);
        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() => loader.Complete(new OperationCanceledException(cancellationToken)));

        start(loader.webView);
        return loader.tcs.Task;
    }


    public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        => this.Complete(null);

    public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
        => this.Complete(new InvalidOperationException(error.LocalizedDescription));

    public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error)
        => this.Complete(new InvalidOperationException(error.LocalizedDescription));


    void Complete(Exception? error)
    {
        // cancellation can arrive off the main thread
        lock (loading)
            loading.Remove(this);

        if (error == null)
            this.tcs.TrySetResult(this.webView);
        else if (error is OperationCanceledException cancelled)
            this.tcs.TrySetCanceled(cancelled.CancellationToken);
        else
            this.tcs.TrySetException(error);
    }
}
