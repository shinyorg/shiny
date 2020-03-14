using System;
using Android.App;
using Android.Runtime;
using $ext_safeprojectname$;
using Shiny;


namespace $safeprojectname$
{
    [Application]
    public class MainApplication : ShinyAndroidApplication<MyShinyStartup>
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }
    }
}