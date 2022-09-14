using Xamarin.Forms;

namespace Sample
{
    public class SampleContentPage : ContentPage
    {
        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.TryFireOnAppearing();
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            this.TryFireOnDisappearing();
        }
    }
}
