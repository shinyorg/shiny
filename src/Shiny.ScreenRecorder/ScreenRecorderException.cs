namespace Shiny.ScreenRecorder;


/// <summary>
/// The base for every failure this library raises. Catch this to handle any recording problem.
/// </summary>
public class ScreenRecorderException : Exception
{
    public ScreenRecorderException(string message) : base(message) { }
    public ScreenRecorderException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// Thrown when the operation cannot be performed on this platform at all.
/// </summary>
/// <remarks>
/// This is a statement about the OS, not about the device or its current state - retrying will
/// never help. The message names the specific limit (Windows.Graphics.Capture having no audio
/// path, ReplayKit having no pause, a compositor that insists on running its own picker). Check
/// the matching <see cref="ScreenRecorderCapabilities"/> flag first if you would rather branch
/// than catch.
/// </remarks>
public class ScreenRecorderNotSupportedException : ScreenRecorderException
{
    public ScreenRecorderNotSupportedException(string message) : base(message) { }


    internal static ScreenRecorderNotSupportedException For(ScreenRecorderCapabilities capability, string reason)
        => new($"{capability} is not available on this platform - {reason}");
}


/// <summary>
/// Thrown when the OS refused the recording because a permission, entitlement or manifest entry is
/// missing, or because the user declined.
/// </summary>
/// <remarks>
/// Unlike <see cref="ScreenRecorderNotSupportedException"/> this is fixable: the message names the
/// exact permission, entitlement or capability. It also covers a declined consent dialog, which
/// is not a configuration problem but is worth handling the same way - offer the button again.
/// </remarks>
public class ScreenRecorderPermissionException : ScreenRecorderException
{
    public ScreenRecorderPermissionException(string message) : base(message) { }
    public ScreenRecorderPermissionException(string message, Exception innerException) : base(message, innerException) { }
}
