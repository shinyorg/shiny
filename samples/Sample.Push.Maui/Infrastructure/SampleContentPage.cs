namespace Sample;


public class SampleContentPage : ContentPage
{
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        //this.TryFireOnAppearing();
    }


    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        //this.TryFireOnDisappearing();
    }
}
//public static void TryFireOnAppearing(this Page page)
//    => (page.BindingContext as ViewModel)?.OnNavigatedTo();

//public static void TryFireOnDisappearing(this Page page)
//    => (page.BindingContext as ViewModel)?.OnNavigatedFrom();