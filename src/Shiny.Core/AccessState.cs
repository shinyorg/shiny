using System;


namespace Shiny
{
    public enum AccessState
    {
        Unknown,

        /// <summary>
        /// API requiring permission is not even supported on current peripheral or platform
        /// </summary>
        NotSupported,

        /// <summary>
        /// This implies you need to setup something in your:
        ///     Android: AndroidManifest.xml
        ///     iOS: Info.plist
        ///     UWP: appx
        /// </summary>
        NotSetup,

        Disabled,
        Restricted,
        Denied,
        Available
    }
}
