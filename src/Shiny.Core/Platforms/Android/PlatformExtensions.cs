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

        public static void Execute(this BroadcastReceiver receiver, Func<Task> task)
        {
            var pendingResult = receiver.GoAsync();
            task().ContinueWith(x =>
            {
                if (x.IsFaulted)
                    Log.Write(x.Exception);

                pendingResult.Finish();
            });
        }

        static Handler? handler;
        public static void Dispatch(this Action action)
        {
            if (handler == null || handler.Looper != Looper.MainLooper)
                handler = new Handler(Looper.MainLooper);

            handler.Post(action);
        }

        public static bool IsNull(this Java.Lang.Object obj)
            => obj == null || obj.Handle == IntPtr.Zero;

        public static void ShinyInit(this Application app, IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
            => AndroidShinyHost.Init(app, startup, platformBuild);

        public static void ShinyRequestPermissionsResult(this Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
            => AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        public static void ShinyOnCreate(this Activity activity)
            => AndroidShinyHost.TryProcessIntent(activity.Intent);

        public static void ShinyOnNewIntent(this Activity activity, Intent intent)
            => AndroidShinyHost.TryProcessIntent(intent);

        public static long ToEpochMillis(this DateTime sendTime)
            => new DateTimeOffset(sendTime).ToUnixTimeMilliseconds();


        public static Guid ToGuid(this byte[] uuidBytes)
        {
            Array.Reverse(uuidBytes);
            var id = BitConverter
                .ToString(uuidBytes)
                .Replace("-", String.Empty);

            switch (id.Length)
            {
                case 4:
                    id = $"0000{id}-0000-1000-8000-00805f9b34fb";
                    return Guid.Parse(id);

                case 8:
                    id = $"{id}-0000-1000-8000-00805f9b34fb";
                    return Guid.Parse(id);

                case 16:
                case 32:
                    return Guid.Parse(id);

                default:
                    Log.Write("Android", "Invalid UUID Detected - " + id);
                    return Guid.Empty;
            }
        }


        public static Guid ToGuid(this UUID uuid) =>
            Guid.ParseExact(uuid.ToString(), "d");


        public static ParcelUuid ToParcelUuid(this Guid guid) =>
            ParcelUuid.FromString(guid.ToString());


        public static UUID ToUuid(this Guid guid)
            => UUID.FromString(guid.ToString());
    }
}
