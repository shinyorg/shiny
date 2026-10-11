using System.Runtime.InteropServices;

namespace Shiny;


/// <summary>
/// The CoreGraphics calls behind the macOS Screen Recording prompt - neither is bound in Microsoft.macOS.dll.
/// </summary>
internal static partial class MacScreenCapture
{
    const string CoreGraphicsLibrary = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";


    /// <summary>Whether the app already holds the Screen Recording grant. Does not prompt.</summary>
    [LibraryImport(CoreGraphicsLibrary, EntryPoint = "CGPreflightScreenCaptureAccess")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool Preflight();


    /// <summary>
    /// Prompts once per install. Returns the state at the time of the call - false the first time even when the
    /// user goes on to grant it, because the grant applies only after the app relaunches.
    /// </summary>
    [LibraryImport(CoreGraphicsLibrary, EntryPoint = "CGRequestScreenCaptureAccess")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool Request();
}
