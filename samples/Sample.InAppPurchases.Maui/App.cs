namespace Sample.InAppPurchases.Maui;


public class App(MainPage mainPage) : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
        => new(new NavigationPage(mainPage));
}
