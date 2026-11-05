using Uno.UI.Hosting;

namespace Sample.Uno.iOS;


// iOS needs no Shiny code here: Uno calls OnLaunched from didFinishLaunching, background launches included, and
// Shiny observes foreground/background and notification taps itself. Push methods
// (application:didRegisterForRemoteNotificationsWithDeviceToken: and friends) are [Export]ed on App - Uno's
// Application is the UIApplicationDelegate - ONLY in an app that uses push.
public static class EntryPoint
{
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAppleUIKit()
            .Build();

        host.Run();
    }
}
