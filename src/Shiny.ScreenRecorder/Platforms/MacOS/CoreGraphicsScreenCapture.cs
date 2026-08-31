using System.Runtime.InteropServices;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The two CoreGraphics calls that drive the macOS Screen Recording TCC prompt.
/// </summary>
/// <remarks>
/// Neither is bound in Microsoft.macOS.dll, and there is no ScreenCaptureKit equivalent -
/// <c>SCShareableContent</c> only tells you access is missing by failing, which is too late to
/// drive a permission flow with. Both are trivially marshalled (no arguments, a boolean return),
/// so a LibraryImport is the whole of the workaround.
/// </remarks>
internal static partial class CoreGraphicsScreenCapture
{
    const string CoreGraphicsLibrary = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";


    /// <summary>Whether the app already holds the Screen Recording grant. Does not prompt.</summary>
    [LibraryImport(CoreGraphicsLibrary, EntryPoint = "CGPreflightScreenCaptureAccess")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool Preflight();


    /// <summary>
    /// Prompts for the Screen Recording grant, once per app install.
    /// </summary>
    /// <remarks>
    /// Returns immediately with the state at the time of the call, which is false the first time
    /// even when the user then grants it - macOS requires the app be relaunched before the new
    /// grant takes effect. Callers should treat false as
    /// <see cref="AccessState.Denied"/> and tell the user to restart the app.
    /// </remarks>
    [LibraryImport(CoreGraphicsLibrary, EntryPoint = "CGRequestScreenCaptureAccess")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool Request();
}
