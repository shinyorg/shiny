using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;


namespace Sample.Create
{
    public partial class LocationPage : ContentPage
    {
        public LocationPage()
        {
            this.InitializeComponent();
            this.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FormSheet);
        }
    }
}