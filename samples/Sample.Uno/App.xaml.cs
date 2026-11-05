using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Uno.Presentation;
using Shiny;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;

namespace Sample.Uno;


public partial class App : Application, IShinyUnoApplication
{
    public App() => this.InitializeComponent();


    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }


    // Everything the Uno template does between CreateBuilder(args) and NavigateAsync. Shiny calls it once per process -
    // on Android from Application.OnCreate, which can be a background start with no activity, so nothing here touches UI.
    public static void ConfigureApp(IApplicationBuilder builder) => builder
        .UseToolkitNavigation()
        .Configure(host => host
            .UseLogging(configure: (_, logging) => logging.SetMinimumLevel(LogLevel.Information))
            .UseNavigation(RegisterRoutes)
#if __WASM__
            .UseShinyWebAssembly()
#endif
            .UseShiny(ShinyStartup.Configure)
        );


    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // in place of this.CreateBuilder(args) - on Android this is the builder MainApplication already built
        var builder = this.GetShinyBuilder(args);
        this.MainWindow = builder.Window;
        this.Host = await builder.NavigateAsync<Shell>();
    }


    static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, MainViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault: true)
                ]
            )
        );
    }
}
