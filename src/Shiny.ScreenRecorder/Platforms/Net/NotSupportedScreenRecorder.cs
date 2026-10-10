using Microsoft.Extensions.Logging;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The plain .NET implementation - there is no screen to record from a console or server host, so
/// every call throws.
/// </summary>
/// <remarks>
/// <para>This exists so that server, console and test hosts referencing a library that in turn
/// references Shiny.ScreenRecorder still compile and still resolve <see cref="IScreenRecorder"/>
/// from DI. <see cref="Capabilities"/> is <see cref="ScreenRecorderCapabilities.None"/>, so
/// well-behaved code branches around it before it ever throws.</para>
/// <para><c>AddScreenRecorder()</c> registers this on plain .NET for Windows and macOS; on Linux and
/// Blazor WebAssembly it registers the real portal or browser recorder instead.</para>
/// </remarks>
public class NotSupportedScreenRecorder(ILogger<NotSupportedScreenRecorder> logger) : AbstractScreenRecorder(logger)
{
    public override ScreenRecorderCapabilities Capabilities => ScreenRecorderCapabilities.None;

    protected override string PlatformReason =>
        "this is the plain .NET target outside Linux and the browser, which has no screen capture API";


    public override Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default)
        => Task.FromResult(AccessState.NotSupported);


    protected override Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    )
        => throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.Recording, this.PlatformReason);
}
