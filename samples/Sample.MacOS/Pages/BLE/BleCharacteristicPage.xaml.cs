namespace Sample.MacOS.Pages.BLE;

public partial class BleCharacteristicPage : ContentPage
{
    readonly BleCharacteristicViewModel viewModel;

    public BleCharacteristicPage(BleCharacteristicViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        this.BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this.viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.viewModel.Dispose();
    }
}
