// Shiny.Blazor.Controls MediaPickerButton
// Handles file/camera selection via hidden <input> elements and compresses/resizes/
// re-encodes the chosen image to PNG or JPEG on an offscreen canvas (same toBlob path
// the image-editor uses), returning the bytes as base64 to .NET.

const states = new Map();

export function init(root, galleryInput, cameraInput, dotnetRef, options) {
    const state = { dotnetRef, options, galleryInput, cameraInput };

    const handler = async e => {
        const file = e.target.files && e.target.files[0];
        e.target.value = ''; // allow re-picking the same file
        if (!file) return;
        try {
            const bitmap = await createImageBitmap(file);
            const result = renderToResult(bitmap, state.options);
            const payload = await result;
            await state.dotnetRef.invokeMethodAsync('OnFilePicked', payload);
        } catch (err) {
            console.error('[shiny-mediapicker] failed to process file', err);
        }
    };

    galleryInput.addEventListener('change', handler);
    cameraInput.addEventListener('change', handler);
    state.handler = handler;
    states.set(root, state);
}

export function updateOptions(root, options) {
    const state = states.get(root);
    if (state) state.options = options;
}

export function openGallery(root) {
    states.get(root)?.galleryInput.click();
}

export function openCamera(root) {
    states.get(root)?.cameraInput.click();
}

// Returns { width, height } for already-encoded base64 image bytes (used after editing).
export async function measure(root, base64, contentType) {
    const blob = new Blob([b64ToBytes(base64)], { type: contentType });
    const bitmap = await createImageBitmap(blob);
    return { width: bitmap.width, height: bitmap.height };
}

function renderToResult(bitmap, options) {
    let w = bitmap.width;
    let h = bitmap.height;
    const max = options.maxDimension || 0;
    if (max > 0 && Math.max(w, h) > max) {
        const scale = max / Math.max(w, h);
        w = Math.round(w * scale);
        h = Math.round(h * scale);
    }

    const canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    canvas.getContext('2d').drawImage(bitmap, 0, 0, w, h);

    const mime = options.format === 'png' ? 'image/png' : 'image/jpeg';
    return new Promise(resolve => {
        canvas.toBlob(async blob => {
            const buf = await blob.arrayBuffer();
            resolve({ dataBase64: abToB64(buf), width: w, height: h, contentType: mime });
        }, mime, options.quality);
    });
}

function abToB64(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    const chunk = 0x8000;
    for (let i = 0; i < bytes.length; i += chunk)
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunk));
    return btoa(binary);
}

function b64ToBytes(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++)
        bytes[i] = binary.charCodeAt(i);
    return bytes;
}

export function dispose(root) {
    const state = states.get(root);
    if (state) {
        state.galleryInput?.removeEventListener('change', state.handler);
        state.cameraInput?.removeEventListener('change', state.handler);
        states.delete(root);
    }
}
