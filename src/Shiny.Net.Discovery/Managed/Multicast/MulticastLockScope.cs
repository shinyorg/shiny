using Microsoft.Extensions.Logging;

#if ANDROID
using Android.Net.Wifi;
using Application = Android.App.Application;
using AndroidContext = Android.Content.Context;
#endif

namespace Shiny.Net.Discovery.Managed.Multicast;


/// <summary>
/// Holds an Android <c>WifiManager.MulticastLock</c> for as long as multicast sockets are open,
/// and does nothing at all on every other platform.
/// </summary>
/// <remarks>
/// <para>Without the lock the Wi-Fi driver filters out every packet not addressed to this device,
/// so sends succeed and absolutely nothing is ever received. That asymmetry is indistinguishable
/// from an empty network, which is why this is acquired automatically rather than left to the
/// consumer.</para>
/// <para>The lock costs battery, so it is scoped to the lifetime of the sockets - it goes away as
/// soon as the last browse or publication is disposed.</para>
/// <para>mDNS on Android never gets here; it goes through <c>NsdManager</c>, which manages its own
/// lock internally.</para>
/// </remarks>
sealed class MulticastLockScope : IDisposable
{
#if ANDROID
    readonly WifiManager.MulticastLock? nativeLock;
#endif

    MulticastLockScope(ILogger logger, string tag)
    {
#if ANDROID
        try
        {
            // deliberately not going through Shiny.Core's AndroidPlatform - this keeps the whole
            // managed stack free of a platform DI dependency, matching AndroidMdnsManager
            var wifi = (WifiManager?)Application.Context.GetSystemService(AndroidContext.WifiService);
            if (wifi == null)
            {
                logger.LogWarning("No WifiManager available - multicast receive will not work on Wi-Fi");
            }
            else
            {
                this.nativeLock = wifi.CreateMulticastLock(tag);
                this.nativeLock!.SetReferenceCounted(false);
                this.nativeLock.Acquire();
                logger.LogDebug("Acquired the Android multicast lock for {Tag}", tag);
            }
        }
        catch (Exception ex)
        {
            // CHANGE_WIFI_MULTICAST_STATE missing from the manifest lands here. Warn loudly rather
            // than throwing - discovery still works over Ethernet and on some OEM builds.
            logger.LogWarning(
                ex,
                "Could not acquire the Android multicast lock for {Tag}. Add <uses-permission android:name=\"android.permission.CHANGE_WIFI_MULTICAST_STATE\" /> to AndroidManifest.xml or nothing will be received over Wi-Fi",
                tag
            );
        }
#endif
    }


    /// <summary>
    /// Acquires the lock where the platform has one. Never throws.
    /// </summary>
    public static MulticastLockScope Acquire(ILogger logger, string tag) => new(logger, tag);


    public void Dispose()
    {
#if ANDROID
        try
        {
            if (this.nativeLock?.IsHeld == true)
                this.nativeLock.Release();

            this.nativeLock?.Dispose();
        }
        catch (Exception)
        {
            // releasing a lock the OS already reclaimed throws - nothing useful to do about it
        }
#endif
    }
}
