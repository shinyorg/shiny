namespace Sample;


public class SampleContentPage : ContentPage
{
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        (this.BindingContext as ViewModel)?.OnNavigatedTo();
    }


    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        (this.BindingContext as ViewModel)?.OnNavigatedFrom();
    }
}