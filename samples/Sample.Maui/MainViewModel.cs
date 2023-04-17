namespace Sample;


public class MainViewModel : ReactiveObject, IPageLifecycleAware
{
    readonly IDeviceDisplay display;


    public MainViewModel(INavigationService navigator, IDeviceDisplay display)
    {
        this.display = display;

        this.Navigate = ReactiveCommand.CreateFromTask<string>(async uri =>
        {
            await navigator.Navigate("NavigationPage/" + uri);
            this.IsMenuVisible = false;
        });
    }


    [Reactive] public bool IsMenuVisible { get; private set; }
    public ICommand Navigate { get; }

    public void OnAppearing() => this.display.KeepScreenOn = true;
    public void OnDisappearing() { }
}
