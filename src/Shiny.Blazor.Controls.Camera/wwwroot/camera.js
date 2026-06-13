// Shiny Blazor CameraView interop.
// All frame analysis runs here in JS; only flat Detection DTOs cross back to .NET.

const states = new WeakMap();

export async function start(video, overlay, dotnetRef, facingMode, enableBarcode, showOverlay) {
    if (!navigator.mediaDevices?.getUserMedia)
        throw new Error('getUserMedia is unavailable (requires a secure context / HTTPS).');

    const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode },
        audio: false
    });
    video.srcObject = stream;
    await video.play();

    const state = {
        video, overlay, dotnet: dotnetRef, stream,
        enableBarcode, showOverlay,
        running: true,
        rafId: null,
        detector: null,
        busy: false
    };
    states.set(video, state);

    if (enableBarcode && 'BarcodeDetector' in globalThis) {
        try { state.detector = new globalThis.BarcodeDetector(); }
        catch { state.detector = null; }
    }
    else if (enableBarcode) {
        // No native detector (Firefox/Safari). Report once; a ZXing-js module can be slotted in here.
        try { await dotnetRef.invokeMethodAsync('OnJsError', 'BarcodeDetector not supported in this browser'); }
        catch { /* ignore */ }
    }

    const ctx = overlay.getContext('2d');
    const loop = async () => {
        if (!state.running) return;
        syncOverlaySize(state);

        if (state.detector && !state.busy) {
            state.busy = true;
            try {
                const codes = await state.detector.detect(video);
                const dets = codes.map(c => toDetection(c, video));
                if (state.showOverlay) drawOverlay(ctx, overlay, dets);
                await state.dotnet.invokeMethodAsync('OnDetections', dets);
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


export function capture(video) {
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d').drawImage(video, 0, 0);
    const dataUrl = canvas.toDataURL('image/jpeg', 0.92);
    const base64 = dataUrl.split(',')[1];
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
    return bytes;
}


function toDetection(code, video) {
    // BarcodeDetector boundingBox is in video pixel space; normalize to 0..1.
    const w = video.videoWidth || 1;
    const h = video.videoHeight || 1;
    const b = code.boundingBox;
    return {
        type: 'Barcode',
        x: b.x / w,
        y: b.y / h,
        w: b.width / w,
        h: b.height / h,
        label: code.format,
        value: code.rawValue,
        confidence: 1
    };
}


function syncOverlaySize(state) {
    const { overlay, video } = state;
    if (overlay.width !== video.clientWidth || overlay.height !== video.clientHeight) {
        overlay.width = video.clientWidth;
        overlay.height = video.clientHeight;
    }
}


function drawOverlay(ctx, overlay, dets) {
    ctx.clearRect(0, 0, overlay.width, overlay.height);
    ctx.lineWidth = 3;
    ctx.strokeStyle = '#22D3EE';
    ctx.fillStyle = '#22D3EE';
    ctx.font = '16px sans-serif';
    for (const d of dets) {
        const x = d.x * overlay.width;
        const y = d.y * overlay.height;
        const w = d.w * overlay.width;
        const h = d.h * overlay.height;
        ctx.strokeRect(x, y, w, h);
        if (d.value) ctx.fillText(d.value, x, Math.max(14, y - 6));
    }
}
