using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.CallDirectory;
using Shiny.Hosting;

namespace Sample.CallDirectory;


public static class Program
{
    static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}


[Register(nameof(AppDelegate))]
public class AppDelegate : ShinyAppDelegate
{
    protected override IHost CreateShinyHost()
    {
        var builder = HostBuilder.Create();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddProvider(new AppLogProvider());
        builder.Services.AddCallDirectory();
        return builder.Build();
    }

}


[Register(nameof(SceneDelegate))]
public class SceneDelegate : UIWindowSceneDelegate
{
    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        this.Window = new UIWindow((UIWindowScene)scene)
        {
            RootViewController = new UINavigationController(new CallDirectoryViewController())
        };
        this.Window.MakeKeyAndVisible();
    }
}
