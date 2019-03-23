using System;


namespace Shiny.BluetoothLE.Central
{
    public static class AndroidBleConfig
    {
        /// <summary>
        /// Allows you to disable the internal sync queue
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static bool UseInternalSyncQueue { get; set; } = true;

        /// <summary>
        /// This is only necessary on niche cases and thus must be enabled by default
        /// </summary>
        public static bool RefreshServices { get; set; }

        ///// <summary>
        ///// Suggests whether main thread is to be used
        ///// </summary>
        //public static bool IsMainThreadSuggested { get; internal set; }

        /// <summary>
        /// If you disable this, you need to manage serial/sequential access to ALL bluetooth operations yourself!
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static bool ShouldInvokeOnMainThread { get; set; } = true;

        /// <summary>
        /// This performs pauses between each operation helping android recover from itself
        /// </summary>
        public static TimeSpan PauseBetweenInvocations { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Time span to pause before service discovery (helps in combating GATT133 error) when service discovery is performed immediately after connection
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static TimeSpan PauseBeforeServiceDiscovery { get; set; } = TimeSpan.FromMilliseconds(750);
    }
}
