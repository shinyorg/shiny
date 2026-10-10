// One recording at a time, matching the IScreenRecorder contract. Module-level state is the
// simplest thing that can hold a MediaStream, a MediaRecorder and the chunks they produce, and it
// makes an accidental second start impossible rather than merely discouraged.
let recorder = null;
let displayStream = null;
let micStream = null;
let audioContext = null;
let chunks = [];
let blob = null;
let mimeType = '';
let dimensions = { width: 0, height: 0 };
let dotNetRef = null;

// Safari and recent Chrome produce fragmented MP4; Firefox does not and needs WebM. The order is
// preference order, and the result reports which one was actually used - an upload endpoint
// usually cares.
const CANDIDATE_TYPES = [
    'video/mp4;codecs=avc1.42E01E',
    'video/mp4',
    'video/webm;codecs=vp9',
    'video/webm;codecs=vp8',
    'video/webm'
];

function pickMimeType() {
    if (typeof MediaRecorder === 'undefined') return null;
    for (const type of CANDIDATE_TYPES) {
        if (MediaRecorder.isTypeSupported(type)) return type;
    }
    return null;
}

export function probe() {
    const hasDisplayMedia = !!(navigator.mediaDevices && navigator.mediaDevices.getDisplayMedia);
    const type = pickMimeType();

    return {
        supported: hasDisplayMedia && type !== null,
        mimeType: type || '',
        hasMicrophone: !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia),
        // only Chromium exposes tab/system audio through getDisplayMedia; Firefox and Safari
        // silently return a video-only stream when audio is asked for
        hasSystemAudio: hasDisplayMedia && !!window.chrome
    };
}

export async function start(objRef, options) {
    if (recorder) throw new Error('A recording is already in progress');

    dotNetRef = objRef;
    chunks = [];
    blob = null;

    const video = { frameRate: options.frameRate || 30 };
    if (options.maxWidth) video.width = { max: options.maxWidth };

    displayStream = await navigator.mediaDevices.getDisplayMedia({
        video: video,
        audio: !!options.includeSystemAudio
    });

    const videoTrack = displayStream.getVideoTracks()[0];
    if (!videoTrack) {
        await cleanup();
        throw new Error('The browser returned no video track');
    }

    const settings = videoTrack.getSettings();
    dimensions = { width: settings.width || 0, height: settings.height || 0 };

    // the browser's own "Stop sharing" bar ends the track without telling the page any other way
    videoTrack.addEventListener('ended', onTrackEnded);

    let tracks = [videoTrack];
    const audioTracks = await buildAudioTracks(options);
    tracks = tracks.concat(audioTracks);

    const recordStream = new MediaStream(tracks);
    mimeType = pickMimeType();

    const recorderOptions = { mimeType: mimeType };
    if (options.videoBitrate) recorderOptions.videoBitsPerSecond = options.videoBitrate;

    recorder = new MediaRecorder(recordStream, recorderOptions);
    recorder.ondataavailable = e => { if (e.data && e.data.size > 0) chunks.push(e.data); };

    // a timeslice means chunks arrive as the recording runs rather than all at the end, which keeps
    // a long recording from sitting entirely in one unbounded buffer
    recorder.start(1000);

    return { width: dimensions.width, height: dimensions.height, mimeType: mimeType };
}

// display audio and the microphone are separate MediaStreams and MediaRecorder will only encode one
// audio track, so when both are wanted they are summed through a WebAudio graph first
async function buildAudioTracks(options) {
    const displayAudio = displayStream.getAudioTracks();

    if (options.includeMicrophone) {
        micStream = await navigator.mediaDevices.getUserMedia({ audio: true });
    }

    const micAudio = micStream ? micStream.getAudioTracks() : [];

    if (displayAudio.length === 0) return micAudio;
    if (micAudio.length === 0) return displayAudio;

    audioContext = new AudioContext();
    const destination = audioContext.createMediaStreamDestination();
    audioContext.createMediaStreamSource(new MediaStream(displayAudio)).connect(destination);
    audioContext.createMediaStreamSource(new MediaStream(micAudio)).connect(destination);

    return destination.stream.getAudioTracks();
}

function onTrackEnded() {
    if (dotNetRef) dotNetRef.invokeMethodAsync('OnTrackEnded');
}

export function pause() {
    if (recorder && recorder.state === 'recording') recorder.pause();
}

export function resume() {
    if (recorder && recorder.state === 'paused') recorder.resume();
}

export async function stop() {
    if (!recorder) return null;

    if (recorder.state !== 'inactive') {
        await new Promise(resolve => {
            recorder.onstop = resolve;
            recorder.stop();
        });
    }

    blob = new Blob(chunks, { type: mimeType });
    chunks = [];

    await cleanup();

    return {
        byteSize: blob.size,
        width: dimensions.width,
        height: dimensions.height,
        mimeType: mimeType
    };
}

export async function cancel() {
    if (recorder && recorder.state !== 'inactive') {
        recorder.onstop = null;
        recorder.stop();
    }
    chunks = [];
    blob = null;
    await cleanup();
}

// returns the recording to .NET as a stream reference rather than base64 - a few minutes of screen
// capture is tens of megabytes, and base64 would inflate it by a third and copy it twice
export function read() {
    if (!blob) throw new Error('There is no recording to read');
    return DotNet.createJSStreamReference(blob);
}

export function download(fileName) {
    if (!blob) throw new Error('There is no recording to download');

    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);

    // revoking immediately can cancel the download in some browsers; a tick is enough
    setTimeout(() => URL.revokeObjectURL(url), 10000);
}

async function cleanup() {
    if (displayStream) {
        displayStream.getTracks().forEach(t => {
            t.removeEventListener('ended', onTrackEnded);
            t.stop();
        });
        displayStream = null;
    }
    if (micStream) {
        micStream.getTracks().forEach(t => t.stop());
        micStream = null;
    }
    if (audioContext) {
        try { await audioContext.close(); } catch { /* already closed */ }
        audioContext = null;
    }
    recorder = null;
    dotNetRef = null;
}
