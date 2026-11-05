using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Sample.Avalonia;


public partial class App : global::Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);


    // UseShiny runs before this, so Shiny.Hosting.Host.Current is already available here
    public override void OnFrameworkInitializationCompleted()
    {
        switch (this.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new Window
                {
                    Title = "Shiny Avalonia",
                    Width = 480,
                    Height = 640,
                    Content = new MainView()
                };
                break;

            // Android: the process can start for a background job or push with no activity, so the view is
            // created only when an activity asks for it
            case IActivityApplicationLifetime activity:
                activity.MainViewFactory = () => new MainView();
                break;

            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView();
                break;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
