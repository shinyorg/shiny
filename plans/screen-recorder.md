# Plan: Shiny.ScreenRecorder

Status: **proposal** — nothing here is committed work.
Last updated: 2026-08-31

## Summary

A cross-platform screen recording module covering **iOS, Mac Catalyst, Android, macOS (AppKit),
Windows, Linux and Blazor WebAssembly**. Every platform has a first-class native API for this and
every one of those APIs is already bound in the SDKs this repo targets — verified by dumping the
reference assemblies (see [Binding verification](#binding-verification)). There is no platform on
the list that needs a new native binding project.

Scope decisions taken up front:

| Decision | Choice | Consequence |
|---|---|---|
| Apple mobile capture scope | **In-app only** | `RPScreenRecorder` records the app's own UI. No Broadcast Upload Extension, so the module ships as a pure NuGet with no extra app target. API is shaped so a broadcast mode can be added later without breaking. |
| Linux encoder | **Portal + child process** | `Shiny.ScreenRecorder.Linux` drives the xdg-desktop-portal ScreenCast D-Bus API, then hands the PipeWire node to `gst-launch-1.0` (Wayland) or `ffmpeg -f x11grab` (X11). No GStreamer P/Invoke surface to maintain. |
| Audio | **Video + mic + app audio where the platform gives it natively** | Windows ships video-only in v1 (Windows.Graphics.Capture has no audio path at all); every other platform gets at least mic. Reported honestly through `ScreenRecorderCapabilities`. |

## Binding verification

Dumped from the reference assemblies installed on this machine
(`Microsoft.iOS.dll` / `Microsoft.macOS.dll` / `Microsoft.MacCatalyst.dll` 26.0.11017,
`Mono.Android.dll` 36.1.69, `Microsoft.Windows.SDK.NET.dll` 10.0.19041.57).

| Platform | Type | Present |
|---|---|---|
| iOS, Mac Catalyst | `ReplayKit.RPScreenRecorder`, `RPSampleBufferType`, `IRPScreenRecorderDelegate` | ✅ |
| Android | `Android.Media.Projection.MediaProjection` / `MediaProjectionManager` / `MediaProjectionConfig` | ✅ |
| Android | `Android.Hardware.Display.VirtualDisplay`, `Android.Media.MediaRecorder`, `MediaCodec` | ✅ |
| macOS, Mac Catalyst | `ScreenCaptureKit.SCStream`, `SCStreamConfiguration`, `SCContentFilter`, `SCShareableContent` | ✅ |
| macOS | `ScreenCaptureKit.SCRecordingOutput` + `SCRecordingOutputConfiguration` (macOS 15+) | ✅ |
| Windows | `Windows.Graphics.Capture.GraphicsCaptureItem` / `Direct3D11CaptureFramePool` / `GraphicsCaptureSession` | ✅ |
| Windows | `Windows.Media.Core.MediaStreamSource`, `Windows.Media.Transcoding.MediaTranscoder` | ✅ |

Two gaps found, both small and both handled below:

- **`CGRequestScreenCaptureAccess` / `CGPreflightScreenCaptureAccess` are not bound** in
  `Microsoft.macOS.dll`. macOS needs a two-line `LibraryImport` into
  `/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics` for the TCC prompt.
- **`IDirect3DDevice` cannot be created from CsWinRT alone.** Windows needs a small P/Invoke pair
  (`D3D11CreateDevice` from `d3d11.dll`, then `CreateDirect3D11DeviceFromDXGIDevice`) — the
  long-standing documented pattern. Requires `AllowUnsafeBlocks` on the Windows TFM, exactly as
  `Shiny.Net.Wifi` already does for `wlanapi.dll`.

Additionally, `SCStreamConfiguration` exposes **`CaptureMicrophone`** and **`MicrophoneCaptureDeviceId`**
(macOS 15+) alongside `CapturesAudio` / `ExcludesCurrentProcessAudio`, so macOS gets system audio
*and* mic with no manual mixing.

## Package split

Mirrors the `Shiny.Net.Wifi` / `Shiny.Net.Http` precedent exactly.

```
src/Shiny.ScreenRecorder/           net10.0 + android + ios + maccatalyst + macos + windows
src/Shiny.ScreenRecorder.Linux/     net10.0  (portal over Tmds.DBus.Protocol + gst/ffmpeg)
src/Shiny.ScreenRecorder.Blazor/    net10.0  (Microsoft.NET.Sdk.Razor, getDisplayMedia + MediaRecorder)
tests/Shiny.ScreenRecorder.Tests/   net10.0
```

Root namespace `Shiny.ScreenRecorder` on all three, so a consumer swapping the Linux or Blazor
package for the base one changes only the `ProjectReference`/`PackageReference` and the
`Add…` call — same as Wi-Fi.

`IsAotCompatible` + `EnableTrimAnalyzer` on, factory-based DI registration (no reflection),
per repo convention.

## Public API

### `IScreenRecorder`

```csharp
namespace Shiny.ScreenRecorder;

public interface IScreenRecorder
{
    /// <summary>What this platform can actually do. Check before offering a feature.</summary>
    ScreenRecorderCapabilities Capabilities { get; }

    ScreenRecorderState State { get; }
    event EventHandler<ScreenRecorderState>? StateChanged;

    /// <summary>Asks for whatever the platform gates recording behind.</summary>
    Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default);

    /// <summary>Displays and windows available to record. Desktop only.</summary>
    /// <exception cref="ScreenRecorderNotSupportedException">Mobile and Blazor, which choose the target themselves.</exception>
    Task<IReadOnlyList<CaptureTarget>> GetTargets(CancellationToken ct = default);

    Task<IScreenRecording> Start(ScreenRecordingRequest request, CancellationToken ct = default);
}
```

### `IScreenRecording`

The live session. Disposing it without `Stop` cancels and cleans up the partial file.

```csharp
public interface IScreenRecording : IAsyncDisposable
{
    TimeSpan Elapsed { get; }
    bool IsPaused { get; }

    Task Pause(CancellationToken ct = default);
    Task Resume(CancellationToken ct = default);
    Task<ScreenRecordingResult> Stop(CancellationToken ct = default);
    Task Cancel(CancellationToken ct = default);

    /// <summary>The OS or the user ended the recording out from under us — an incoming call,
    /// a revoked projection, the browser's own "Stop sharing" button.</summary>
    event EventHandler<ScreenRecordingFaultedEventArgs>? Faulted;
}
```

### Request, result, capabilities

```csharp
public record ScreenRecordingRequest
{
    public string? OutputPath { get; init; }         // null => a temp file the module names
    public CaptureTarget? Target { get; init; }      // null => primary display / the app itself
    public bool IncludeMicrophone { get; init; }
    public bool IncludeSystemAudio { get; init; }
    public bool ShowCursor { get; init; } = true;
    public int? FrameRate { get; init; }             // null => platform default (30)
    public int? VideoBitrate { get; init; }
    public int? MaxWidth { get; init; }              // downscale, preserving aspect
    public TimeSpan? MaxDuration { get; init; }
}

public record ScreenRecordingResult(
    string? FilePath,        // null on Blazor WASM — there is no filesystem
    TimeSpan Duration,
    long ByteSize,
    int Width,
    int Height,
    string MimeType
)
{
    /// <summary>Reads the recording back. Works everywhere, including where FilePath is null.</summary>
    public Task<Stream> OpenRead(CancellationToken ct = default);
}

[Flags]
public enum ScreenRecorderCapabilities
{
    None              = 0,
    Recording         = 1,
    PauseResume       = 2,
    Microphone        = 4,
    SystemAudio       = 8,
    DisplaySelection  = 16,
    WindowSelection   = 32,
    CursorToggle      = 64,
    FrameRateControl  = 128,
    BitrateControl    = 256,
    Downscaling       = 512
}

public enum ScreenRecorderState { Idle, Starting, Recording, Paused, Stopping }
```

Exceptions mirror the Wi-Fi module: `ScreenRecorderException` base,
`ScreenRecorderNotSupportedException` (this platform has no such concept — names the limit),
`ScreenRecorderPermissionException` (a grant is missing — names what to add).

`AbstractScreenRecorder` carries the state machine, request validation, capability gating
(a request asking for something outside `Capabilities` throws before touching native), the
`MaxDuration` timer, and throwing defaults for `GetTargets`.

**`ScreenRecordingResult.FilePath` is deliberately nullable.** Blazor WASM has no filesystem;
forcing a path there would mean either lying or excluding the platform. `OpenRead()` is the
portable accessor and the desktop/mobile implementations just open the file.

## Per-platform backends

### Android — `Platforms/Android/`

The most moving parts of any platform, because Android 14 tightened the ordering.

1. **Consent.** `MediaProjectionManager.CreateScreenCaptureIntent()` must go through
   `StartActivityForResult`. `AndroidPlatform.Handle(activity, requestCode, resultCode, intent)`
   is an empty stub in Core today, so rather than change Core this module ships its own
   translucent, `NoHistory`, non-exported `ScreenCapturePermissionActivity` that launches the
   intent and completes a `TaskCompletionSource`. Self-contained; no Core change needed.
2. **Foreground service before projection.** On Android 14+ (API 34) a foreground service of type
   `mediaProjection` must already be running before `GetMediaProjection(resultCode, data)` is
   called, or it throws. So: consent → `ScreenRecorderService` (a
   `ShinyAndroidForegroundService`, `ForegroundServiceType = ForegroundService.TypeMediaProjection`)
   → `GetMediaProjection`.
3. **Capture.** `MediaProjection.CreateVirtualDisplay(...)` onto the encoder's input `Surface`,
   sized from the request (`MaxWidth` downscale) and the display metrics.
4. **Encode.** Two phases:
   - **Phase 1 — `MediaRecorder`.** `VideoSource.Surface`, `OutputFormat.Mpeg4`,
     `VideoEncoder.H264`, `AudioSource.Mic` when requested. Native `Pause()`/`Resume()` (API 24+).
     `Capabilities`: everything except `SystemAudio`.
   - **Phase 2 — `MediaCodec` + `MediaMuxer`.** Needed for app audio: `MediaRecorder` takes a
     single audio source and playback capture only comes through
     `AudioRecord` + `AudioPlaybackCaptureConfiguration` (API 29+). A surface H.264 encoder and an
     AAC encoder feed one `MediaMuxer`; mic and app audio can be mixed into one track. This is
     what turns `SystemAudio` on. Phase 2 is a real chunk of work and is scheduled separately.
5. **Loss.** `MediaProjection.Callback.OnStop` → raise `Faulted` and tear down.

Manifest requirements documented on `AddScreenRecorder()`: `FOREGROUND_SERVICE`,
`FOREGROUND_SERVICE_MEDIA_PROJECTION`, `RECORD_AUDIO` when mic is used, and the service
declaration with `android:foregroundServiceType="mediaProjection"`.

### iOS / Mac Catalyst — `Platforms/Apple/`

`RPScreenRecorder.SharedRecorder` with **`StartCapture`**, not `StartRecording`. `StartRecording`
buries the file inside ReplayKit and only surrenders it through `RPPreviewViewController` — no
good for a library that promises a path. `StartCapture` hands over `CMSampleBuffer`s tagged
`RPSampleBufferType.Video` / `.AudioApp` / `.AudioMic`, which go straight into an `AVAssetWriter`.

- `AVAssetWriter` with an `AVAssetWriterInput` for video plus one for audio. When both app audio
  and mic are requested, app audio is the primary track and mic becomes a second audio track
  (MP4 tolerates it; most players use the first). Documented, and surfaced as a remark on
  `IncludeMicrophone`.
- **Pause/resume is synthesised.** ReplayKit has no pause. The writer drops buffers while paused
  and subtracts the paused span from every subsequent presentation timestamp, so the output has no
  gap. This is the same trick used for `Elapsed`.
- `IRPScreenRecorderDelegate.DidStopRecording` → `Faulted`.
- Constraint worth stating plainly in docs: **the app must be in the foreground**, and this
  records the app's own UI only.
- `RequestAccess` maps to `AVCaptureDevice.RequestAccessForMediaType(AVMediaTypes.Audio)` when mic
  is requested (`NSMicrophoneUsageDescription` needed); video capture itself needs no grant because
  the app is recording itself.

The `AVAssetWriter` plumbing is shared with macOS below via `Platforms/AppleShared/`, imported the
way `Shiny.Net.Wifi.csproj` already does it (`ImportDirectoryBuildTargets=false` plus an explicit
`Compile Include` for the three Apple TFMs).

### macOS (AppKit) — `Platforms/MacOS/`

ScreenCaptureKit, with a version split that saves a lot of code on modern macOS.

- **Targets.** `SCShareableContent.GetShareableContentAsync()` → `SCDisplay[]` / `SCWindow[]` /
  `SCRunningApplication[]`, projected onto `CaptureTarget`. This is where `DisplaySelection` and
  `WindowSelection` come from — macOS is the richest platform in the module.
- **Configuration.** `SCStreamConfiguration` covers the whole request surface directly:
  `Width`/`Height` (downscale), `MinimumFrameInterval` (frame rate), `ShowsCursor`,
  `CapturesAudio` + `ExcludesCurrentProcessAudio` (system audio), and `CaptureMicrophone` +
  `MicrophoneCaptureDeviceId` on macOS 15+.
- **macOS 15+ (`SCRecordingOutput`).** `SCRecordingOutputConfiguration { OutputUrl, OutputFileType,
  VideoCodecType }` added to the stream via `AddRecordingOutput`. ScreenCaptureKit writes the MP4
  itself — no writer, no sample pump, no timestamp arithmetic.
- **macOS 12.3–14 fallback.** `AddStreamOutput` with an `ISCStreamOutput`, buffers into the shared
  `AVAssetWriter` helper. Same code path as iOS.
- **Permission.** The Screen Recording TCC grant. `CGPreflightScreenCaptureAccess` /
  `CGRequestScreenCaptureAccess` via `LibraryImport` (not bound in the SDK), with the
  `SCShareableContent` failure as a backstop. Needs `NSMicrophoneUsageDescription` for mic and
  `com.apple.security.device.audio-input` when sandboxed.
- **Pause/resume.** No native pause on `SCStream`. Under `SCRecordingOutput` this means
  `RemoveRecordingOutput`/`AddRecordingOutput` is not viable mid-file, so macOS 15+ reports
  `PauseResume` **off** while the AVAssetWriter path (12.3–14) reports it **on**. Rather than
  degrade the modern path, the capability differs by OS version and says so.

### Windows — `Platforms/Windows/`

Windows.Graphics.Capture into a `MediaStreamSource`, transcoded by Media Foundation.

1. `GraphicsCaptureSession.IsSupported()` gates everything; `Capabilities` is `None` when false.
2. `GraphicsCaptureItem` from a monitor or window handle through the
   `IGraphicsCaptureItemInterop` COM interface (`CreateForMonitor` / `CreateForWindow`) — this is
   how `GetTargets` returns real displays and windows without a picker. `GraphicsCapturePicker`
   is offered as an opt-in alternative and needs `IInitializeWithWindow`.
3. D3D11 device via the `D3D11CreateDevice` + `CreateDirect3D11DeviceFromDXGIDevice` P/Invoke pair,
   then `Direct3D11CaptureFramePool.CreateFreeThreaded`.
4. Frames → `MediaStreamSample.CreateFromDirect3D11Surface` → `MediaStreamSource`
   (a single `VideoStreamDescriptor`) → `MediaTranscoder.PrepareMediaStreamSourceTranscodeAsync`
   with `MediaEncodingProfile.CreateMp4(...)` writing to the output file.
5. **No audio in v1.** Windows.Graphics.Capture is video-only; system audio needs a hand-written
   WASAPI loopback capture and an AAC encoder feeding a second `MediaStreamSource` descriptor.
   `Capabilities` reports neither `Microphone` nor `SystemAudio`, and requesting either throws
   `ScreenRecorderNotSupportedException` naming WASAPI as the missing piece. Tracked as v2.
6. Pause/resume by withholding samples from the `MediaStreamSource` and offsetting timestamps —
   the same synthesised approach as Apple.
7. `GraphicsCaptureItem.Closed` (monitor unplugged, window closed) → `Faulted`.
8. `AllowUnsafeBlocks` on the Windows TFM only. Packaged apps declare the `graphicsCapture`
   capability.

### Linux — `src/Shiny.ScreenRecorder.Linux/`

Follows `Shiny.Net.Wifi.Linux`: `Tmds.DBus.Protocol` on the session bus, no generated proxies.

1. **Portal.** `org.freedesktop.portal.ScreenCast`: `CreateSession` → `SelectSources`
   (with `types` for monitor/window and `cursor_mode`) → `Start`. Each returns a `Request` object
   path whose `Response` signal carries the result — the compositor shows its own picker, so the
   user chooses the target and **`GetTargets` throws `ScreenRecorderNotSupportedException`** here,
   pointing at the portal picker instead.
2. `OpenPipeWireRemote` returns a file descriptor plus the selected stream's PipeWire node id.
3. **Encode via a child process.**
   - Wayland (portal succeeded):
     `gst-launch-1.0 -e pipewiresrc fd=<fd> path=<node> ! videoconvert ! x264enc ! h264parse ! mp4mux ! filesink location=<out>`,
     with `pulsesrc ! audioconvert ! avenc_aac` teed into the muxer when audio is requested.
   - X11 (no portal, or portal refused): `ffmpeg -f x11grab -framerate N -i :0.0 ...`,
     with `-f pulse -i default` for audio.
4. **Stopping must be graceful.** `SIGINT` to the child (never `Kill`) so `mp4mux`/ffmpeg writes
   the `moov` atom; then await exit with a timeout before falling back to `SIGKILL` and reporting
   a corrupt file rather than pretending success.
5. **Capabilities are probed, not assumed.** `gst-launch-1.0` / `ffmpeg` presence and the
   portal's own `AvailableSourceTypes` decide the flags at construction. Missing both encoders →
   `Capabilities.None` and a `ScreenRecorderNotSupportedException` that names the packages to
   install.
6. No pause (`PauseResume` off).

The command-line construction and the portal response parsing are pure functions and are the
main unit-test surface for this package.

### Blazor WebAssembly — `src/Shiny.ScreenRecorder.Blazor/`

`Microsoft.NET.Sdk.Razor` with `wwwroot/screen-recorder.js`, imported as a JS module exactly like
`Shiny.Core.Blazor`'s `battery.js`.

- `navigator.mediaDevices.getDisplayMedia({ video: { frameRate }, audio: includeSystemAudio })`.
  The browser shows its own picker, so `GetTargets` throws.
- Mic via `getUserMedia({ audio: true })`, mixed with display audio through an `AudioContext`
  and a `MediaStreamAudioDestinationNode`.
- `MediaRecorder` preferring `video/mp4;codecs=avc1` and falling back to
  `video/webm;codecs=vp9` — `MimeType` on the result says which was used, since it genuinely
  varies by browser. Chrome-only for system audio, and only for tab audio at that; capability
  flags are set from `MediaRecorder.isTypeSupported` and a `getDisplayMedia` feature probe.
- Native `pause()`/`resume()` — the only platform where pause is free.
- The display track's `ended` event (the browser's own "Stop sharing" bar) → `Faulted`.
- `FilePath` is null; `OpenRead()` streams the `Blob` back through `IJSStreamReference`.
  A `DownloadRecording(fileName)` extension on the Blazor package triggers the browser save.

### Plain .NET base target

`NotSupportedScreenRecorder` — `Capabilities.None`, every call throws
`ScreenRecorderNotSupportedException`. Keeps server and console hosts compiling, and is what a
Linux or Blazor consumer replaces by referencing the platform package.

## Capability matrix

| | Record | Pause | Mic | System audio | Display pick | Window pick | Cursor | Scale |
|---|---|---|---|---|---|---|---|---|
| iOS / Mac Catalyst | ✅ app UI | ✅ synth | ✅ | ✅ app audio | ❌ | ❌ | ❌ | ✅ |
| Android | ✅ system | ✅ | ✅ | phase 2 | ❌ | ❌ | ❌ | ✅ |
| macOS 15+ | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| macOS 12.3–14 | ✅ | ✅ synth | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Windows | ✅ | ✅ synth | ❌ v2 | ❌ v2 | ✅ | ✅ | ✅ | ✅ |
| Linux | ✅ | ❌ | ✅ | ✅ | portal | portal | ✅ | ✅ |
| Blazor WASM | ✅ | ✅ native | ✅ | Chrome tab only | browser | browser | ✅ | ✅ |

## Build order

1. **Abstractions + base target + tests.** `IScreenRecorder`, `IScreenRecording`, request/result,
   capabilities, exceptions, `AbstractScreenRecorder` state machine, `NotSupportedScreenRecorder`,
   DI registration. Unit tests for validation, capability gating and the state machine.
2. **macOS.** The richest backend and the fastest to validate on this machine — proves the API
   shape against displays, windows, audio and both the `SCRecordingOutput` and `AVAssetWriter`
   paths.
3. **Apple mobile.** Reuses the `AVAssetWriter` helper from step 2 through `Platforms/AppleShared`.
4. **Android phase 1** (`MediaRecorder`: video + mic), including the consent activity and the
   foreground service.
5. **Blazor WASM.** Small, self-contained, and the only place pause is native.
6. **Windows.** Frame pool → `MediaStreamSource` → `MediaTranscoder`, plus the D3D11 P/Invoke.
   Build locally on macOS with `WindowsTarget` + `EnableWindowsTargeting` forced so the platform
   code actually compiles.
7. **Linux.** Portal + child process, with the command builder unit-tested.
8. **Android phase 2** (`MediaCodec` + `MediaMuxer`) to turn on `SystemAudio`.
9. **Windows v2** (WASAPI loopback) — deferred, tracked here rather than scheduled.

Each step is independently shippable: the capability flags mean a platform that has not landed
yet reports `None` rather than breaking the build or lying to callers.

## Repo integration

Per `CLAUDE.md`, a change is not done until these are in sync:

- **`readme.md`** — a new **Screen Recording** bullet in `## Modules`, in the house style: what it
  does, the per-platform backends, and where reach is uneven.
- **`skills/shiny-screenrecorder/SKILL.md`** — new skill with triggers (`screen record`,
  `screen recording`, `record the screen`, `screen capture`, `ReplayKit`, `MediaProjection`,
  `ScreenCaptureKit`, `getDisplayMedia`, `MediaRecorder`, `RPScreenRecorder`, …).
- **Docs site** (`~/Desktop/dev/documentation`) — a new `src/content/docs/screenrecorder/` folder
  with the feature page and `release-notes.mdx`, plus a node in `src/sidebar-topics.mjs` under
  **App Essentials**.
- **`Shiny.slnx`** — the three new `src/` projects and the test project; `Linux.slnf` gets the
  Linux package.
- **Samples** — `Sample.Maui` (iOS/Android/Windows/Mac Catalyst), `Sample.MacOS`, `Sample.Linux`
  and `Sample.Blazor` each get a recorder page. No release notes for sample-only changes.

## Open questions

- **Frame-stream access.** Should `IScreenRecording` also expose raw frames (for live streaming or
  on-device analysis) rather than only a file? Every backend has them in hand. Left out of v1 to
  keep the API honest, but it is the obvious v2 addition and the interface has room for it.
- **Screenshots.** `SCScreenshotManager` (macOS), `PixelCopy` (Android) and a single-frame grab on
  the other platforms would make a `CaptureScreenshot()` almost free. Worth deciding whether this
  module owns that or a sibling does — it affects the package name.
