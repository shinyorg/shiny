using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Microsoft.Extensions.DependencyInjection;
#if ANDROIDX
using AndroidX.Lifecycle;
#endif


namespace Shiny
{
//#if ANDROIDX
//    public abstract class ShinyAndroidApplication : Application, ILifecycleEventObserver
//#else
    public abstract class ShinyAndroidApplication : Application
//#endif
    {
        protected ShinyAndroidApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        protected virtual void InitShiny()
        {
            AndroidShinyHost.Init(this, null, this.OnBuildApplication);
        }


//#if ANDROIDX

//        public override void OnCreate()
//        {
//            //ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
//            this.InitShiny();
//            base.OnCreate();
//        }


//        public void OnStateChanged(ILifecycleOwner owner, Lifecycle.Event @event)
//        {

//            Console.WriteLine(owner);
//            Console.Write(@event.Name());
//        }

//#else
        public override void OnCreate()
        {
            this.InitShiny();
            base.OnCreate();
        }


        public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
        {
            AndroidShinyHost.OnBackground(level);
            base.OnTrimMemory(level);
        }
//#endif

        protected virtual void OnBuildApplication(IServiceCollection builder) { }
    }


    public abstract class ShinyAndroidApplication<T> : ShinyAndroidApplication where T : IShinyStartup, new()
    {
        protected ShinyAndroidApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {}


        protected override void InitShiny()
            => AndroidShinyHost.Init(this, new T(), this.OnBuildApplication);
    }
}
