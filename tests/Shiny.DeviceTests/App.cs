namespace Shiny.DeviceTests;


public class App(MainPage page) : Application
{
    protected override Window CreateWindow(IActivationState? activationState) => new(page);
}
