using Microsoft.UI.Xaml;

namespace Sample.Maui.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
        Shiny.Jobs.ShinyJobsBackgroundTask.RegisterComServer();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
