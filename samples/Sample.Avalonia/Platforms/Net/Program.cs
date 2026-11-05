using Avalonia;
using Shiny;

namespace Sample.Avalonia;


public static class Program
{
    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);


    // also used by the Avalonia previewer
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseShiny(ShinyStartup.Configure);
}
