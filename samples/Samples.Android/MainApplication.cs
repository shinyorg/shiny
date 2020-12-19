using System;
using Android.App;
using Android.Runtime;
using Shiny;


namespace Samples.Droid
{
    [Application]
    public partial class MainApplication : Application
    {
        public MainApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }


        public override void OnCreate()
        {
            base.OnCreate();
            this.ShinyOnCreate(new SampleStartup());
        }
    }
}