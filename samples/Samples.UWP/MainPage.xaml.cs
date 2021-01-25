using System;
using Xamarin;

namespace Samples.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.LoadApplication(new Samples.App());
        }
    }
}
