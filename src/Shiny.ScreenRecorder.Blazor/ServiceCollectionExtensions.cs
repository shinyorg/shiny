using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.ScreenRecorder;

namespace Shiny;


public static class BlazorScreenRecorderServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Blazor WebAssembly <see cref="IScreenRecorder"/>, backed by
    /// <c>getDisplayMedia</c> and <c>MediaRecorder</c>.
    /// </summary>
    /// <remarks>
    /// <para>The page must be served over HTTPS (or from localhost), and
    /// <see cref="IScreenRecorder.Start"/> must be reached from a user gesture such as a button
    /// click - browsers refuse <c>getDisplayMedia</c> otherwise.</para>
    /// <para>When the app is hosted in an iframe, the frame needs
    /// <c>allow="display-capture; microphone"</c> or the picker never appears.</para>
    /// <para>Support varies by browser, so call
    /// <see cref="BlazorScreenRecorder.Probe"/> once at startup - until it has run,
    /// <see cref="IScreenRecorder.Capabilities"/> reports
    /// <see cref="ScreenRecorderCapabilities.None"/> because feature detection needs a JS round
    /// trip that a synchronous property cannot make.</para>
    /// </remarks>
    public static IServiceCollection AddScreenRecorder(this IServiceCollection services)
    {
        services.AddSingleton<IScreenRecorder>(sp => new BlazorScreenRecorder(
            sp.GetRequiredService<IJSRuntime>(),
            sp.GetRequiredService<ILogger<BlazorScreenRecorder>>()
        ));

        return services;
    }
}


public static class BlazorScreenRecorderExtensions
{
    /// <summary>
    /// Hands the finished recording to the user as a browser download.
    /// </summary>
    /// <remarks>
    /// The browser is the only place a recording is not already a file the app can move, so this
    /// exists to close that gap. Give the file name an extension matching
    /// <see cref="ScreenRecordingResult.MimeType"/> - <c>.mp4</c> or <c>.webm</c> - since the
    /// container varies by browser.
    /// </remarks>
    public static async Task DownloadRecording(
        this IScreenRecorder recorder,
        ScreenRecordingResult result,
        string fileName,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(result);

        if (recorder is not BlazorScreenRecorder blazor)
            throw new ScreenRecorderException("DownloadRecording only works with the Blazor screen recorder");

        var module = await blazor.GetModule().ConfigureAwait(false);
        await module.InvokeVoidAsync("download", ct, fileName).ConfigureAwait(false);
    }
}
