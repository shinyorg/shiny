using System;


namespace Shiny.Locations
{
    static class PlatformExtensions
    {
        internal static long ToMillis(this TimeSpan ts)
            => Convert.ToInt64(ts.TotalMilliseconds);


        internal static long ToMillis(this TimeSpan? ts, long defaultValue)
            => Convert.ToInt64(ts?.TotalMilliseconds ?? defaultValue);
    }
}
