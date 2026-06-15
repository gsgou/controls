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

export async function start(video, overlay, dotnetRef, facingMode, enableBarcode, showOverlay, deviceId) {
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
                const boxes = codes.map(c => toOverlayBox(c, video));
                if (state.showOverlay) drawOverlay(ctx, overlay, boxes);
                await state.dotnet.invokeMethodAsync('OnOverlays', boxes);
                for (const c of codes)
                    await state.dotnet.invokeMethodAsync('OnBarcode', toBarcode(c, video));
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


function drawOverlay(ctx, overlay, boxes) {
    ctx.clearRect(0, 0, overlay.width, overlay.height);
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
