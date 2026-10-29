using Microsoft.Extensions.DependencyInjection;
using Shiny.Nfc;
using Shiny.Nfc.Infrastructure;

namespace Shiny;


public static class NfcServiceCollectionExtensions
{
#if PLATFORM
    /// <summary>
    /// Registers <see cref="INfcManager"/> for reading and writing NDEF tags.
    /// </summary>
    /// <remarks>
    /// <para><b>Android:</b> add <c>&lt;uses-permission android:name="android.permission.NFC" /&gt;</c>
    /// (a normal permission - granted at install, never prompted) and, unless the app is useless
    /// without NFC, <c>&lt;uses-feature android:name="android.hardware.nfc" android:required="false" /&gt;</c>.
    /// Scans run in reader mode on the current activity.</para>
    /// <para><b>iOS:</b> add <c>NFCReaderUsageDescription</c> to Info.plist and the
    /// <c>com.apple.developer.nfc.readersession.formats</c> entitlement with the value <c>TAG</c>
    /// (the App Store rejects <c>NDEF</c> since the iOS 18 SDK; <c>TAG</c> covers NDEF sessions).
    /// Needs an iPhone 7 or later; the iOS simulator and Mac Catalyst report no NFC.</para>
    /// <para><b>Windows and macOS:</b> NFC goes through a PC/SC contactless reader (an ACR122U,
    /// for example). A sandboxed macOS app also needs the <c>com.apple.security.smartcard</c> entitlement. A laptop's built-in NFC is usually a PC/SC
    /// reader too.</para>
    /// </remarks>
    public static IServiceCollection AddNfc(this IServiceCollection services)
    {
#if ANDROID
        services.AddSingleton<INfcManager, AndroidNfcManager>();
#elif IOS || MACCATALYST
        services.AddSingleton<INfcManager, AppleNfcManager>();
#else
        services.AddSingleton<INfcManager, PcscNfcManager>();
#endif
        return services;
    }
#else
    /// <summary>
    /// Registers <see cref="INfcManager"/> for reading and writing NDEF tags, choosing the backend
    /// at runtime: Web NFC on Blazor WebAssembly, and a PC/SC contactless reader on Linux, Windows
    /// and macOS.
    /// </summary>
    /// <remarks>
    /// <para><b>Blazor WebAssembly:</b> Web NFC is Chrome on Android only, in a secure context, and
    /// a scan has to start from a user gesture. Other browsers report
    /// <see cref="NfcCapabilities.None"/>. Writing waits for the next tag tapped rather than
    /// targeting the tag just read - in practice the same tag, still in the field.</para>
    /// <para><b>Linux:</b> needs <c>pcscd</c> running and <c>libpcsclite.so.1</c> installed
    /// (<c>pcscd</c> and <c>libpcsclite1</c> on Debian, Ubuntu and Raspberry Pi OS), plus the
    /// reader's CCID driver. Check with <c>pcsc_scan</c>.</para>
    /// </remarks>
    public static IServiceCollection AddNfc(this IServiceCollection services)
    {
        if (OperatingSystem.IsBrowser())
            services.AddSingleton<INfcManager, WebNfcManager>();
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            services.AddSingleton<INfcManager, PcscNfcManager>();
        else
            services.AddNotSupportedNfc();

        return services;
    }
#endif


    /// <summary>
    /// Registers an <see cref="INfcManager"/> that reports no NFC, for server, console and test hosts
    /// that resolve it from a shared library.
    /// </summary>
    public static IServiceCollection AddNotSupportedNfc(this IServiceCollection services)
    {
        services.AddSingleton<INfcManager, NotSupportedNfcManager>();
        return services;
    }
}
