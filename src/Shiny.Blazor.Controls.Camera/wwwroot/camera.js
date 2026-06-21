// Shiny Blazor CameraView interop.
// All frame analysis runs here in JS; only flat overlay-box + barcode DTOs cross back to .NET.

const states = new WeakMap();

export async function listCameras() {
    if (!navigator.mediaDevices?.enumerateDevices)
        return [];
    // labels are only populated after camera permission has been granted (i.e. after start())
    const devices = await navigator.mediaDevices.enumerateDevices();
    return devices
        .filter(d => d.kind === 'videoinput')
        .map((d, i) => ({ id: d.deviceId, name: d.label || `Camera ${i + 1}` }));
}

export async function start(video, overlay, dotnetRef, facingMode, analyzerKind, showOverlay, deviceId, showBoundingBox, scanWindow) {
    if (!navigator.mediaDevices?.getUserMedia)
        throw new Error('getUserMedia is unavailable (requires a secure context / HTTPS).');

    // an exact deviceId pins a specific camera; otherwise fall back to the front/back facing hint
    const video_constraints = deviceId ? { deviceId: { exact: deviceId } } : { facingMode };
    const stream = await navigator.mediaDevices.getUserMedia({
        video: video_constraints,
        audio: false
    });
    video.srcObject = stream;
    await video.play();

    const state = {
        video, overlay, dotnet: dotnetRef, stream,
        analyzerKind, showOverlay,
        showBoundingBox: showBoundingBox !== false,
        scanWindow: scanWindow || null,   // [x, y, w, h] normalized, or null for the whole frame
        running: true,
        rafId: null,
        detector: null,
        busy: false,
        armed: false   // gated: a decoded barcode is only delivered to .NET while armed (see arm())
    };
    states.set(video, state);

    if (analyzerKind === 'barcode' && 'BarcodeDetector' in globalThis) {
        try { state.detector = new globalThis.BarcodeDetector(); }
        catch { state.detector = null; }
    }
    else if (analyzerKind === 'barcode') {
        // No native detector (Firefox/Safari). Report once; a ZXing-js module can be slotted in here.
        try { await dotnetRef.invokeMethodAsync('OnJsError', 'BarcodeDetector not supported in this browser'); }
        catch { /* ignore */ }
    }
    else if (analyzerKind) {
        // only barcode has an in-browser engine today; other kinds are placeholders
        try { await dotnetRef.invokeMethodAsync('OnJsError', `Analyzer '${analyzerKind}' is not supported in the browser`); }
        catch { /* ignore */ }
    }

    const ctx = overlay.getContext('2d');
    const loop = async () => {
        if (!state.running) return;
        syncOverlaySize(state);

        if (state.detector && !state.busy) {
            state.busy = true;
            try {
                let codes = await state.detector.detect(video);
                // restrict to the scan window (codes whose center falls inside it)
                if (state.scanWindow)
                    codes = codes.filter(c => inScanWindow(c, video, state.scanWindow));

                const boxes = state.showBoundingBox ? codes.map(c => toOverlayBox(c, video)) : [];
                // boxes always draw + flow to .NET (presentation); the decoded value is gated behind arm()
                if (state.showOverlay) drawOverlay(ctx, overlay, boxes, state.scanWindow);
                await state.dotnet.invokeMethodAsync('OnOverlays', boxes);
                if (state.armed && codes.length > 0) {
                    state.armed = false; // one delivery per arm; .NET re-arms to keep scanning
                    await state.dotnet.invokeMethodAsync('OnBarcodes', codes.map(c => toBarcode(c, video)));
                }
            }
            catch { /* transient detect error; keep looping */ }
            finally { state.busy = false; }
        }
        state.rafId = requestAnimationFrame(loop);
    };
    state.rafId = requestAnimationFrame(loop);
}


export function stop(video) {
    const state = states.get(video);
    if (!state) return;
    state.running = false;
    if (state.rafId) cancelAnimationFrame(state.rafId);
    state.stream?.getTracks().forEach(t => t.stop());
    video.srcObject = null;
    const ctx = state.overlay.getContext('2d');
    ctx.clearRect(0, 0, state.overlay.width, state.overlay.height);
    states.delete(video);
}


// Arm the detector to deliver the next frame's decoded barcodes to .NET (then it self-disarms). Boxes keep
// drawing every frame regardless; this only gates the OnBarcodes callback.
export function arm(video) {
    const state = states.get(video);
    if (state) state.armed = true;
}


export function disarm(video) {
    const state = states.get(video);
    if (state) state.armed = false;
}


export function setFilter(video, css) {
    video.style.filter = css || 'none';
}


export async function startRecording(video, includeAudio) {
    const state = states.get(video);
    if (!state) throw new Error('Camera is not started');

    let stream = state.stream;
    let extraAudio = null;
    if (includeAudio) {
        try {
            extraAudio = await navigator.mediaDevices.getUserMedia({ audio: true });
            stream = new MediaStream([...state.stream.getVideoTracks(), ...extraAudio.getAudioTracks()]);
        }
        catch { extraAudio = null; /* fall back to video-only */ }
    }

    const chunks = [];
    const rec = new MediaRecorder(stream);
    rec.ondataavailable = e => { if (e.data.size) chunks.push(e.data); };
    state.recorder = rec;
    state.recChunks = chunks;
    state.recExtraAudio = extraAudio;
    rec.start();
}


export function stopRecording(video) {
    return new Promise((resolve, reject) => {
        const state = states.get(video);
        if (!state?.recorder) { reject('Not recording'); return; }
        const rec = state.recorder;
        rec.onstop = async () => {
            const blob = new Blob(state.recChunks, { type: rec.mimeType || 'video/webm' });
            const buf = new Uint8Array(await blob.arrayBuffer());
            state.recExtraAudio?.getTracks().forEach(t => t.stop());
            state.recorder = null; state.recChunks = null; state.recExtraAudio = null;
            resolve(buf);
        };
        rec.stop();
    });
}


export function capture(video, filterCss) {
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const ctx = canvas.getContext('2d');
    if (filterCss && filterCss !== 'none')
        ctx.filter = filterCss;   // bake the preview filter into the still
    ctx.drawImage(video, 0, 0);
    const dataUrl = canvas.toDataURL('image/jpeg', 0.92);
    const base64 = dataUrl.split(',')[1];
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
    return bytes;
}


function toOverlayBox(code, video) {
    // BarcodeDetector boundingBox is in video pixel space; normalize to 0..1.
    const w = video.videoWidth || 1;
    const h = video.videoHeight || 1;
    const b = code.boundingBox;
    return {
        x: b.x / w,
        y: b.y / h,
        w: b.width / w,
        h: b.height / h,
        strokeColor: '#22C55E',
        text: code.rawValue,
        textColor: '#22C55E'
    };
}


// True when the code's center falls inside the normalized scan window [x, y, w, h].
function inScanWindow(code, video, win) {
    const vw = video.videoWidth || 1;
    const vh = video.videoHeight || 1;
    const b = code.boundingBox;
    const cx = (b.x + b.width / 2) / vw;
    const cy = (b.y + b.height / 2) / vh;
    return cx >= win[0] && cx <= win[0] + win[2] && cy >= win[1] && cy <= win[1] + win[3];
}


function toBarcode(code, video) {
    const w = video.videoWidth || 1;
    const h = video.videoHeight || 1;
    const b = code.boundingBox;
    return {
        format: code.format,
        value: code.rawValue,
        x: b.x / w,
        y: b.y / h,
        w: b.width / w,
        h: b.height / h
    };
}


function syncOverlaySize(state) {
    const { overlay, video } = state;
    if (overlay.width !== video.clientWidth || overlay.height !== video.clientHeight) {
        overlay.width = video.clientWidth;
        overlay.height = video.clientHeight;
    }
}


function drawOverlay(ctx, overlay, boxes, scanWindow) {
    ctx.clearRect(0, 0, overlay.width, overlay.height);

    // scan-window viewfinder: dim outside it and frame it with a reticle
    if (scanWindow) {
        const wx = scanWindow[0] * overlay.width;
        const wy = scanWindow[1] * overlay.height;
        const ww = scanWindow[2] * overlay.width;
        const wh = scanWindow[3] * overlay.height;
        ctx.fillStyle = 'rgba(0, 0, 0, 0.43)';
        ctx.fillRect(0, 0, overlay.width, wy);
        ctx.fillRect(0, wy + wh, overlay.width, overlay.height - (wy + wh));
        ctx.fillRect(0, wy, wx, wh);
        ctx.fillRect(wx + ww, wy, overlay.width - (wx + ww), wh);
        ctx.lineWidth = 2;
        ctx.strokeStyle = '#FFFFFF';
        ctx.strokeRect(wx, wy, ww, wh);
    }

    ctx.lineWidth = 3;
    ctx.font = '16px sans-serif';
    for (const b of boxes) {
        const x = b.x * overlay.width;
        const y = b.y * overlay.height;
        const w = b.w * overlay.width;
        const h = b.h * overlay.height;
        ctx.strokeStyle = b.strokeColor || '#22D3EE';
        ctx.fillStyle = b.textColor || b.strokeColor || '#22D3EE';
        ctx.strokeRect(x, y, w, h);
        if (b.text) ctx.fillText(b.text, x, Math.max(14, y - 6));
    }
}
