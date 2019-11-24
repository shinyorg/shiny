using System;
using Shiny.Logging;
using Android.OS;
using Java.Util;


namespace Shiny
{
    public static class PlatformExtensions
    {
        static Handler? handler;


        //        public static void ConfigureJobs(this IServiceCollection services)
        //        {
        //            //public static int AndroidJobId { get; set; } = 100;
        //            //public static TimeSpan PeriodicRunTime { get; set; } = TimeSpan.FromMinutes(10);
        //        }


        public static void Dispatch(this Action action)
        {
            if (handler == null || handler.Looper != Looper.MainLooper)
                handler = new Handler(Looper.MainLooper);

            handler.Post(action);
        }


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
