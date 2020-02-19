using System;
using Android.App;
using Android.Runtime;
using $ext_safeprojectname$;
using Shiny;


namespace $safeprojectname$
{
#if DEBUG
    [Application(Debuggable = true)]
#else
    [Application(Debuggable = false)]
#endif
    public class MainApplication : ShinyAndroidApplication<MyShinyStartup>
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }
    }
}