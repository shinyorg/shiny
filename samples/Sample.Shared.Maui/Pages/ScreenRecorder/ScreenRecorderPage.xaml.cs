namespace Sample.Shared.Maui.Pages.ScreenRecorder;

public partial class ScreenRecorderPage : ContentPage
{
    public ScreenRecorderPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (this.BindingContext is ScreenRecorderViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
