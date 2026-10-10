using Microsoft.JSInterop;
using Shiny.ScreenRecorder;

namespace Shiny;


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
