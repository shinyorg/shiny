using Sample.Shared.Maui.Services;

namespace Sample.Maui;

public partial class App : Application
{
    readonly AppStateTracker appState;

    public App(AppStateTracker appState)
    {
        this.appState = appState;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        this.appState.Track(window);
        return window;
    }
}
