using System;
using System.Threading.Tasks;
using Shiny.Logging;
using Android.App;
using Android.OS;
using Android.Content;
using Android.Content.PM;
using Java.Util;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class PlatformExtensions
    {
        public static T GetService<T>(this Context context, string name) where T : Java.Lang.Object
            => (T)context.GetSystemService(name);


        public static bool IsAtLeastAndroid10(this AndroidContext context)
            => context.IsMinApiLevel(29);


        static Handler? handler;
        public static void Dispatch(this Action action)
        {
            if (handler == null || handler.Looper != Looper.MainLooper)
                handler = new Handler(Looper.MainLooper);

            handler.Post(action);
        }

        public static bool IsNull(this Java.Lang.Object obj)
            => obj == null || obj.Handle == IntPtr.Zero;


        public static long ToEpochMillis(this DateTime sendTime)
            => new DateTimeOffset(sendTime).ToUnixTimeMilliseconds();
    }
}
