using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;


namespace Sample.Create
{
    public partial class SchedulePage : SampleContentPage
    {
        public SchedulePage()
        {
            this.InitializeComponent();
            this.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.FormSheet);
        }
    }
}