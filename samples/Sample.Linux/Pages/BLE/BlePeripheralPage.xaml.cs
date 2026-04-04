namespace Sample.Linux.Pages.BLE;

public partial class BlePeripheralPage : ContentPage
{
    readonly BlePeripheralViewModel viewModel;

    public BlePeripheralPage(BlePeripheralViewModel viewModel)
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
        this.viewModel.OnDisappearing();
    }
}
