using Platform.Maui.Linux.Gtk4.Platform;

namespace Sample.Linux;

public class Program : GtkMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    static void Main(string[] args) => new Program().Run(args);
}
