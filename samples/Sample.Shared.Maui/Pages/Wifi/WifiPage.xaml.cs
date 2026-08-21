namespace Sample.Shared.Maui.Pages.Wifi;

public partial class WifiPage : ContentPage
{
    public WifiPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (this.BindingContext is WifiViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
