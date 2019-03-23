using AppKit;
using Foundation;
using Xamarin.Forms;
using Xamarin.Forms.Platform.MacOS;


namespace Samples.macOS
{
    [Register("AppDelegate")]
    public class AppDelegate : FormsApplicationDelegate
    {
        readonly NSWindow window;


        public AppDelegate()
        {
            var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled;

            var rect = new CoreGraphics.CGRect(200, 1000, 1024, 768);
            this.window = new NSWindow(rect, style, NSBackingStore.Buffered, false);
            this.window.Title = "Samples";
            this.window.TitleVisibility = NSWindowTitleVisibility.Hidden;
        }


        public override NSWindow MainWindow => this.window;

        public override void DidFinishLaunching(NSNotification notification)
        {
            Forms.Init();
            this.LoadApplication(new App(null));
            base.DidFinishLaunching(notification);
        }
    }
}
