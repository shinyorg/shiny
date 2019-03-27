using System;


namespace Shiny
{
    public enum AccessState
    {
        /// <summary>
        /// The permission has not yet been checked or is in some unknown state within the OS
        /// </summary>
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

        /// <summary>
        /// The service has been disabled by the user
        /// </summary>
        Disabled,

        /// <summary>
        /// The permission has been granted in a limited fashion (ie. Location in foreground only, when background was requested)
        /// </summary>
        Restricted,

        /// <summary>
        /// The user denied permission to use the service
        /// </summary>
        Denied,

        /// <summary>
        /// All necessary permissions are granted to the service
        /// </summary>
        Available
    }
}
