using System;
using Android.App;
using Android.Runtime;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public abstract class ShinyAndroidApplication<T> : Application where T : Startup, new()
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
