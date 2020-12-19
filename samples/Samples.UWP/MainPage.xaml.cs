using System;
using Xamarin;

namespace Samples.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();
            //XF.Material.Forms.Material.Init();
            this.LoadApplication(new Samples.App());
        }
    }
}
