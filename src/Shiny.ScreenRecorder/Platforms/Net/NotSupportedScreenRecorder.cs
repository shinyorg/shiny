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
/// <para>A Linux desktop wants <c>Shiny.ScreenRecorder.Linux</c> and a Blazor WebAssembly app
/// wants <c>Shiny.ScreenRecorder.Blazor</c>; both register a real implementation of this same
/// interface over the top of this one.</para>
/// </remarks>
public class NotSupportedScreenRecorder(ILogger<NotSupportedScreenRecorder> logger) : AbstractScreenRecorder(logger)
{
    public override ScreenRecorderCapabilities Capabilities => ScreenRecorderCapabilities.None;

    protected override string PlatformReason =>
        "this is the plain .NET target, which has no screen capture API. Reference Shiny.ScreenRecorder.Linux on a Linux desktop, or Shiny.ScreenRecorder.Blazor in a browser";


    public override Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default)
        => Task.FromResult(AccessState.NotSupported);


    protected override Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    )
        => throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.Recording, this.PlatformReason);
}
