using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;


namespace Sample.Create
{
    public partial class IntervalPage : ContentPage
    {
        public IntervalPage()
        {
            this.InitializeComponent();
            this.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FormSheet);
        }
    }
}