using System;
using Java.Util;


namespace Shiny.BluetoothLE
{
    public static class Utils
    {
        public static string ToUuidString(string value)
        {
            if (value.Length == 4)
                value = $"0000{value}-0000-1000-8000-00805F9B34FB";

            return value;
        }


        public static UUID ToUuidType(string value)
            => UUID.FromString(Utils.ToUuidString(value));
    }
}
