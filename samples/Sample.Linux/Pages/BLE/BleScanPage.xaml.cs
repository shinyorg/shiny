namespace Sample.Linux.Pages.BLE;

public partial class BleScanPage : ContentPage
{
    readonly BleScanViewModel viewModel;

    public BleScanPage(BleScanViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        this.BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.viewModel.Dispose();
    }
}
