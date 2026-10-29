namespace Sample.Shared.Maui.Pages.Security;

public partial class NfcPage : ContentPage
{
    public NfcPage() => this.InitializeComponent();

    // a scan holds the radio (and the iOS sheet) - end it when the page goes
    protected override void OnDisappearing()
    {
        (this.BindingContext as NfcViewModel)?.StopScan();
        base.OnDisappearing();
    }
}
