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
