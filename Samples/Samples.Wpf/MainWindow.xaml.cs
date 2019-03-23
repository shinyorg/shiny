using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;


namespace Samples.Wpf
{
    public partial class MainWindow : FormsApplicationPage
    {
        public MainWindow()
        {
            this.InitializeComponent();

            Forms.Init();
            this.LoadApplication(new Samples.App());
        }
    }
}