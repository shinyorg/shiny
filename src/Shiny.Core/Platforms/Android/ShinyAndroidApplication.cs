using System;
using Android.App;
using Android.Content;
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


        public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
        {
            AndroidShinyHost.OnBackground(level);
            base.OnTrimMemory(level);
        }


        public override void OnTerminate()
        {
            AndroidShinyHost.OnTerminate();
            base.OnTerminate();
        }


        protected virtual void OnBuildApplication(IServiceCollection builder) { }
    }


    //implementation "android.arch.lifecycle:extensions:1.0.0" and annotationProcessor "android.arch.lifecycle:compiler:1.0.0"
    //class AppLifecycleListener : LifecycleObserver
    //{

    //    @OnLifecycleEvent(Lifecycle.Event.ON_START)
    //    fun onMoveToForeground()
    //    { // app moved to foreground
    //    }

    //    @OnLifecycleEvent(Lifecycle.Event.ON_STOP)
    //    fun onMoveToBackground()
    //    { // app moved to background
    //    }
    //}

    //// register observer
    //ProcessLifecycleOwner.get().lifecycle.addObserver(AppLifecycleListener())
    public abstract class ShinyAndroidApplication<T> : Application where T : IShinyStartup, new()
    {
        protected ShinyAndroidApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {}


        public override void OnCreate()
        {
            base.OnCreate();
            AndroidShinyHost.Init(this, new T(), this.OnBuildApplication);
        }


        public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
        {
            AndroidShinyHost.OnBackground(level);
            base.OnTrimMemory(level);
        }


        public override void OnTerminate()
        {
            AndroidShinyHost.OnTerminate();
            base.OnTerminate();
        }


        protected virtual void OnBuildApplication(IServiceCollection builder) {}
    }
}
