---
name: shiny-screenrecorder
description: Generate code using Shiny.ScreenRecorder for cross-platform screen recording - capturing the screen to an MP4 with optional microphone and system audio, choosing a display or window, pausing and resuming, and handling the OS ending a recording on its own, on Android, iOS, Mac Catalyst, macOS, Windows, Linux, and Blazor WebAssembly
auto_invoke: true
triggers:
  - screen record
  - screen recorder
  - screen recording
  - record the screen
  - record my screen
  - screen capture
  - capture the screen
  - screencast
  - screen cast
  - record app video
  - record a video of the app
  - capture video of the screen
  - session replay video
  - bug report video
  - record a demo
  - record a tutorial
  - screen share
  - share my screen
  - IScreenRecorder
  - IScreenRecording
  - ScreenRecordingRequest
  - ScreenRecordingResult
  - ScreenRecorderCapabilities
  - ScreenRecordingFaultReason
  - CaptureTarget
  - ScreenRecorderNotSupportedException
  - ScreenRecorderPermissionException
  - AddScreenRecorder
  - Shiny.ScreenRecorder
  - ReplayKit
  - RPScreenRecorder
  - startCapture
  - RPSampleBufferType
  - broadcast upload extension
  - MediaProjection
  - MediaProjectionManager
  - createScreenCaptureIntent
  - getMediaProjection
  - VirtualDisplay
  - FOREGROUND_SERVICE_MEDIA_PROJECTION
  - AudioPlaybackCaptureConfiguration
  - ScreenCaptureKit
  - SCStream
  - SCStreamConfiguration
  - SCContentFilter
  - SCRecordingOutput
  - SCShareableContent
  - CGRequestScreenCaptureAccess
  - Windows.Graphics.Capture
  - GraphicsCaptureItem
  - Direct3D11CaptureFramePool
  - MediaStreamSource
  - MediaTranscoder
  - getDisplayMedia
  - MediaRecorder
  - xdg-desktop-portal
  - ScreenCast portal
  - pipewiresrc
  - x11grab
  - record system audio
  - record internal audio
  - record app audio
  - capture microphone while recording
  - pause a recording
  - stop sharing
  - recording stopped by itself
---

# Shiny.ScreenRecorder Skill

You are an expert in Shiny.ScreenRecorder, a cross-platform screen recording library covering
**video capture to a file**, **microphone and system audio**, **display/window selection**,
**pause and resume**, and **the OS ending a recording without being asked**.

## When to Use This Skill

Invoke this skill when the user wants to:
- Record the screen (or their app's screen) to a video file
- Attach a screen recording to a bug report or support ticket
- Record a demo, tutorial or session replay from inside an app
- Capture the microphone, the app's own audio, or the machine's audio alongside video
- Let the user pick which display or window to record
- Pause and resume a recording
- Handle the user or the OS stopping a recording partway through
- Ask why screen recording "only records my own app on iPhone"

## ⚠️ Read this before writing anything

**What "the screen" means is not the same on every platform, and the difference is visible to
users.** Three facts decide most of the design:

1. **iOS and Mac Catalyst record your own app's UI only.** ReplayKit's in-app path is all a NuGet
   package can offer - system-wide capture needs a Broadcast Upload Extension, which is a second
   app target the consumer must create. Never tell a user their iPhone app can record other apps.
2. **Windows has no audio at all.** `Windows.Graphics.Capture` captures pixels and nothing else.
   Asking for `IncludeMicrophone` or `IncludeSystemAudio` there **throws**.
3. **Capabilities differ within a platform, not just between them.** macOS 15 gains microphone
   capture and *loses* pause; macOS 12.3-14 is the other way round. Read
   `recorder.Capabilities` off the instance at runtime. Never infer it from the target framework.

A request asking for something outside `Capabilities` throws
`ScreenRecorderNotSupportedException` **before any native call happens** - by design, because a
recording that silently came out without the microphone is worse than one that refused to start.
**Branch on the flags.**

## Capability matrix

| | Android | iOS / Mac Catalyst | tvOS | macOS 15+ | macOS 12.3-14 | Windows | Linux | Blazor WASM |
|---|---|---|---|---|---|---|---|---|
| What is recorded | system screen | **this app only** | **this app only** | system screen | system screen | system screen | system screen | user's pick |
| Record | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Pause / Resume | ✅ | ✅ synth | ✅ synth | ❌ | ✅ synth | ✅ synth | ❌ | ✅ **native** |
| Microphone | ✅ | ✅ | ❌ **no mic** | ✅ | ❌ | ❌ | ✅ | ✅ |
| System audio | ✅ API 29+ (app audio) | ✅ (app audio) | ✅ (app audio) | ✅ | ✅ | ❌ | ✅ | ⚠️ Chromium, tab only |
| Pick a display | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | portal picker | browser picker |
| Pick a window | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | portal picker | browser picker |
| Hide the cursor | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Frame rate | ✅ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Bitrate | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Downscale | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Result has a file path | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ **null** |

**tvOS** is the same ReplayKit implementation as iOS. The only difference is the microphone: an Apple TV has none, `RPScreenRecorder` carries no `MicrophoneEnabled` on tvOS, so `ScreenRecorderCapabilities.Microphone` is not advertised and `IncludeMicrophone = true` is rejected by request validation. Never generate a tvOS recording request that sets it.

### The four things users most often ask for that cannot be done

1. **Recording other apps from an iPhone app.** ReplayKit's in-app capture is scoped to your own
   process. The only route to the system screen is a Broadcast Upload Extension target, which this
   package does not ship. Do not suggest a workaround; there isn't one.
2. **Audio on Windows.** `Windows.Graphics.Capture` has no audio path. Adding it means a
   hand-written WASAPI loopback capture, which this library does not do. `Capabilities` reports
   neither audio flag on Windows.
3. **Hiding the Android cast indicator or the Windows 11 capture border.** Both are OS-drawn
   recording indicators and neither is suppressible from a normal app. This is deliberate on the
   platforms' part and the library does not try to work around it.
4. **Silently starting a recording.** Every platform except iOS-recording-itself puts a consent
   step in front of it - Android's dialog, macOS's TCC grant, the Linux portal picker, the browser
   picker. There is no unattended screen recording here.

## Library Overview

| Item      | Value |
|-----------|-------|
| GitHub    | https://github.com/shinyorg/shiny |
| NuGet     | `Shiny.ScreenRecorder`, plus `Shiny.ScreenRecorder.Linux` on Linux and `Shiny.ScreenRecorder.Blazor` in the browser |
| Namespace | `Shiny.ScreenRecorder` (types); `Shiny` (registration extensions) |
| Platforms | Android, iOS, Mac Catalyst, macOS, Windows, Linux, Blazor WebAssembly |

### How each platform is backed

| Platform | Capture | Encoder |
|---|---|---|
| Android | `MediaProjection` → `VirtualDisplay` | `MediaCodec` (H.264 surface + AAC) → `MediaMuxer` |
| iOS / Mac Catalyst | `RPScreenRecorder.startCapture` | `AVAssetWriter` |
| macOS 15+ | `SCStream` | `SCRecordingOutput` (ScreenCaptureKit writes the file) |
| macOS 12.3-14 | `SCStream` + `ISCStreamOutput` | `AVAssetWriter` |
| Windows | `Direct3D11CaptureFramePool` | `MediaStreamSource` → `MediaTranscoder` |
| Linux | xdg-desktop-portal `ScreenCast` → PipeWire | `gst-launch-1.0`, or `ffmpeg -f x11grab` |
| Blazor WASM | `getDisplayMedia` | `MediaRecorder` |
| plain .NET | none - every call throws | none |

**Android uses MediaCodec rather than the much simpler MediaRecorder for one reason:**
`MediaRecorder.setAudioSource` takes a single source and playback capture is not one of them, so
app audio is only reachable through `AudioRecord` + `AudioPlaybackCaptureConfiguration`. Wanting
app audio at all forces the whole pipeline down.

## Registration

```csharp
builder.Services.AddScreenRecorder();     // IScreenRecorder, singleton
```

Same call on every platform. On Linux reference `Shiny.ScreenRecorder.Linux` and in a Blazor
WebAssembly app reference `Shiny.ScreenRecorder.Blazor` **instead of** the base package - each
registers its own implementation of the same interface.

On a plain .NET host - a server, console or test project with no screen - the base package offers
`AddNotSupportedScreenRecorder()` instead, which registers a recorder reporting
`ScreenRecorderCapabilities.None`. It is named differently on purpose: the Linux and Blazor packages
register a *real* implementation under `AddScreenRecorder` on that same target framework, so sharing
the name would make every call ambiguous in a project referencing one of them.

### Platform setup

**Android** - `AndroidManifest.xml`:
```xml
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_MEDIA_PROJECTION" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />   <!-- only if capturing audio -->
```
The foreground service and the consent activity are in the package and merge into your manifest
automatically. From Android 14 the service **must** be running before the projection is obtained;
the library does that ordering for you.

**iOS / Mac Catalyst** - no entitlement to record your own app. Add
`NSMicrophoneUsageDescription` to `Info.plist` if using the microphone. The app must be in the
**foreground**.

**macOS** - the Screen Recording grant in System Settings. `RequestAccess` prompts for it, but
**macOS only applies a new grant on the next launch** - the first `RequestAccess` after the user
approves still reports `Denied`, and you must tell them to restart the app. Add
`NSMicrophoneUsageDescription`, and `com.apple.security.device.audio-input` when sandboxed.

**Windows** - Windows 10 1903 or later. Packaged apps declare the `graphicsCapture` capability.

**Linux** - a desktop session with `xdg-desktop-portal` implementing ScreenCast, plus
`gstreamer1.0-tools gstreamer1.0-plugins-good gstreamer1.0-plugins-bad gstreamer1.0-pipewire`, or
`ffmpeg` on X11. Audio needs `pactl`. Flatpak sandboxes are **not** supported. All of this is
probed at runtime - a machine missing the pieces reports `ScreenRecorderCapabilities.None`.

**Blazor WebAssembly** - HTTPS (or localhost) and a **user gesture**. `getDisplayMedia` is refused
from `OnInitializedAsync`; it must run from a button click. In an iframe, add
`allow="display-capture; microphone"`.

## The basic recording

```csharp
public class BugReportRecorder(IScreenRecorder recorder, ILogger<BugReportRecorder> logger)
{
    IScreenRecording? session;

    public async Task Start(CancellationToken ct)
    {
        var request = new ScreenRecordingRequest
        {
            // null lets the library name a file in the platform cache directory
            OutputPath = null,
            IncludeMicrophone = recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.Microphone),
            MaxWidth = 1280,                       // a phone screen at native resolution is enormous
            MaxDuration = TimeSpan.FromMinutes(2)
        };

        var access = await recorder.RequestAccess(request, ct);
        if (access is AccessState.Denied or AccessState.NotSupported)
            throw new InvalidOperationException("Screen recording is not available");

        // does not return until frames are genuinely being written - the consent dialog, the
        // compositor picker and the Android foreground service all complete first
        this.session = await recorder.Start(request, ct);

        this.session.Faulted += (_, e) =>
            logger.LogWarning("The recording ended on its own: {Reason}", e.Reason);
    }

    public async Task<ScreenRecordingResult> Stop(CancellationToken ct)
    {
        var result = await this.session!.Stop(ct);
        this.session = null;

        return result;
    }
}
```

**`MaxWidth` is worth setting on almost every recording.** A modern phone or Retina display at
native resolution produces very large files for very little visible gain.

**`RequestAccess` cannot always answer.** Android's consent dialog is bound to the projection it
authorises and cannot be pre-granted, and the Linux portal and browser pickers grant per call - all
three report `AccessState.Unknown`. Treat anything other than `Denied` / `NotSupported` as "worth
trying", and let `Start` surface the real answer.

## Reading the result

```csharp
var result = await session.Stop(ct);

logger.LogInformation(
    "{Duration} of {Width}x{Height} {MimeType}, {Bytes} bytes",
    result.Duration, result.Width, result.Height, result.MimeType, result.ByteSize
);

// portable - works on every platform including the browser
await using var stream = await result.OpenRead(ct);
await UploadAsync(stream, result.MimeType, ct);
```

**`FilePath` is null in the browser** - there is no filesystem. Use `OpenRead()` when you want the
bytes and do not care where they came from. **`MimeType` genuinely varies**: native platforms all
produce `video/mp4`, but Firefox produces `video/webm;codecs=vp9`. Do not hardcode `.mp4` when
uploading or naming a download.

On Android the file is in app-private cache - move or share it before the OS reclaims it. On Apple
platforms it is inside the app container and is **not** in Photos until you put it there.

## Pausing

```csharp
if (recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.PauseResume))
{
    await session.Pause(ct);
    // ... user does something private ...
    await session.Resume(ct);
}
```

Both calls are idempotent. Only the browser pauses natively; elsewhere the capture keeps running,
frames are dropped, and later timestamps are shifted back so the output has **no frozen stretch** -
which also means a long pause still costs battery. `Elapsed` excludes the paused span and matches
the duration of the finished file.

## Choosing a display or window

```csharp
if (recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.DisplaySelection))
{
    var targets = await recorder.GetTargets(ct);

    var display = targets.FirstOrDefault(t => t.Kind == CaptureTargetKind.Display && t.IsPrimary)
        ?? targets.First();

    await recorder.Start(new ScreenRecordingRequest { Target = display }, ct);
}
else
{
    // mobile has no concept of a target; Linux and the browser show their own picker during Start
    await recorder.Start(new ScreenRecordingRequest(), ct);
}
```

`CaptureTarget.Id` is the platform's own handle and is **not stable** across reboots or across a
window being closed and reopened. Re-enumerate rather than persisting one. Window titles are often
empty or duplicated - pair `Name` with `ApplicationName` when showing a list.

**`GetTargets` throws on Linux and in the browser.** Their compositors insist on running their own
picker, which appears during `Start`. `Target` must be left null there.

## When the OS ends it for you

This is not an edge case - it is the normal way a screen recording ends on several platforms.

```csharp
session.Faulted += (_, e) =>
{
    // by now the session is finished; Stop() will return what was salvaged rather than continuing
    switch (e.Reason)
    {
        case ScreenRecordingFaultReason.RevokedByUser:
            // Android's cast notification, the browser's "Stop sharing" bar, macOS's menu-bar stop
            break;

        case ScreenRecordingFaultReason.InterruptedBySystem:
            // an incoming call on iOS, an Android foreground-service timeout, the screen locking
            break;

        case ScreenRecordingFaultReason.MaxDurationReached:
            // stopped cleanly - e.Result always carries a complete file
            break;

        case ScreenRecordingFaultReason.TargetLost:
            // a monitor unplugged, a recorded window closed
            break;

        case ScreenRecordingFaultReason.EncoderFailed:
            // e.Result is usually null and the file is unusable
            break;
    }

    if (e.Result != null)
        Save(e.Result);
};
```

`Faulted` fires on a **native callback thread**. Marshal before touching UI. The same is true of
`IScreenRecorder.StateChanged`.

## Lifecycle rules

- **One recording at a time.** `Start` throws `ScreenRecorderException` while another is in
  flight - every platform underneath has the same restriction.
- **Stop or dispose. Never just drop it.** Disposing without `Stop` cancels and **deletes the
  partial file**.
- `Stop` twice returns the same result. `Stop` after `Cancel` throws - there is no output.
- Stopping is not instant. Flushing the encoder and writing the container index takes a moment on a
  long recording, and killing the process during it leaves a file with no index that will not play.

## Blazor specifics

```razor
<button @onclick="StartRecording">Record</button>   @* must be a user gesture *@

@code {
    [Inject] IScreenRecorder Recorder { get; set; } = null!;
    IScreenRecording? session;

    protected override async Task OnInitializedAsync()
    {
        // Capabilities is synchronous but browser feature detection is not, so it reports None
        // until this has run
        await ((BlazorScreenRecorder)this.Recorder).Probe();
    }

    async Task StartRecording()
        => this.session = await this.Recorder.Start(new ScreenRecordingRequest());

    async Task StopRecording()
    {
        var result = await this.session!.Stop();

        // the browser is the only place the recording is not already a file the app can move
        var extension = result.MimeType.Contains("mp4") ? "mp4" : "webm";
        await this.Recorder.DownloadRecording(result, $"recording.{extension}");
    }
}
```

## Common mistakes

| Mistake | What happens | Do instead |
|---|---|---|
| Assuming iOS records the whole screen | Only your app is in the file | Say so in the UI; use Android/desktop for full-screen capture |
| Asking for audio on Windows | `ScreenRecorderNotSupportedException` | Check `Capabilities` first |
| Reading `Capabilities` off the TFM | Wrong on macOS, where 15+ and 12.3-14 differ | Read it off the instance at runtime |
| Recording at native resolution on a phone | Enormous files | Set `MaxWidth` |
| Calling `Start` from `OnInitializedAsync` in Blazor | The browser refuses it | Call it from a button click |
| Hardcoding `.mp4` in the browser | Firefox produces WebM | Read `result.MimeType` |
| Ignoring `Faulted` | A recording the user stopped looks like a hang | Subscribe before the first frame |
| Dropping the session without stopping | The file is deleted | `Stop()`, or `Cancel()` if you mean to discard it |
| Persisting a `CaptureTarget.Id` | Stale handle on the next run | Re-enumerate with `GetTargets` |
| Expecting `RequestAccess` to settle Android consent | It reports `Unknown` | Let `Start` show the dialog |

## Exceptions

| Exception | Meaning |
|---|---|
| `ScreenRecorderNotSupportedException` | The platform cannot do it at all. Retrying never helps; the message names the limit. Check the matching capability flag first. |
| `ScreenRecorderPermissionException` | Fixable - a missing manifest entry, entitlement or usage description, or the user declined a consent dialog. |
| `ScreenRecorderException` | Base type, and everything else: a recording already in flight, an encoder failure, a file that never got written. |
