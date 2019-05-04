using System;
using Android.App;
using Android.Runtime;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public abstract class ShinyAndroidApplication : Application
    {
        protected ShinyAndroidApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        public override void OnCreate()
        {
            base.OnCreate();
            AndroidShinyHost.Init(this, null, this.OnBuildApplication);
        }


        protected virtual void OnBuildApplication(IServiceCollection builder) { }
    }



    public abstract class ShinyAndroidApplication<T> : Application where T : IStartup, new()
    {
        protected ShinyAndroidApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {}


        public override void OnCreate()
        {
            base.OnCreate();
            AndroidShinyHost.Init(this, new T(), this.OnBuildApplication);
        }


        protected virtual void OnBuildApplication(IServiceCollection builder) {}
    }
}
