namespace System;

public static class OperatingSystemShim
{
    public static bool IsAndroidVersionAtLeast(int apiLevel)
    {
#if ANDROID
        return (int)Android.OS.Build.VERSION.SdkInt >= apiLevel;
#else
        return false;
#endif
    }


    //macCatalyst 13.0 = macOS 10.15 (Catalina)
    //macCatalyst 13.4 = macOS 10.15.4
    //macCatalyst 14.0 = macOS 11.0 (Big Sur)
    //macCatalyst 14.7 = macOS 11.6
    //macCatalyst 15.0 = macOS 12.0 (Monterey)
    //macCatalyst 15.3 = macOS 12.2 and 12.2.1
    //macCatalyst 15.4 = macOS 12.3
    //macCatalyst 15.5 = macOS 12.4
    //macCatalyst 15.6 = macOS 12.5
    public static bool IsAppleVersionAtleast(int osMajor, int osMinor = 0)
    {
#if IOS || XAMARINIOS || MACCATALYST
        return IsIOSVersionAtLeast(osMajor, osMinor) || IsMacCatalystVersionAtLeast(osMajor, osMinor);
#else
        return false;
#endif
    }

    public static bool IsIOSVersionAtLeast(int osMajor, int osMinor = 0)
    {
#if IOS || XAMARINIOS
        return UIKit.UIDevice.CurrentDevice.CheckSystemVersion(osMajor, osMinor);
#else
        return false;
#endif
    }


    public static bool IsMacCatalystVersionAtLeast(int osMajor, int osMinor = 0)
    {
#if MACCATALYST
        return OperatingSystem.IsMacCatalystVersionAtLeast(osMajor, osMinor);
#else
        return false;
#endif
    }
}
