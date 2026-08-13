// Progress for an image download, which the DOM alone cannot give you.
//
// An <img> element reports "started" and "finished" and nothing in between - there is no progress
// event for it in any browser. The only way to know that 40% of a photo has arrived is to fetch it
// yourself, read the body as a stream, and count the bytes. That is all this module does: fetch,
// pump, report, then hand back a blob URL for the <img> to display.
//
// The trade is CORS. A plain <img> may load a cross-origin image with no server cooperation at all;
// fetch() may not. So every failure here is treated as "let the browser do it instead" rather than
// as an error - the image still appears, just without a percentage.

const MIN_REPORT_INTERVAL_MS = 100;
const MIN_REPORT_DELTA = 0.01;

const pending = new Map();

/** Cancels an in-flight load. Safe to call for an id that has already finished. */
export function abort(requestId) {
    const controller = pending.get(requestId);
    if (controller) {
        controller.abort();
        pending.delete(requestId);
    }
}

/** Releases a blob URL. Called for every URL this module handed out, or the tab leaks one per image. */
export function revoke(url) {
    if (typeof url === 'string' && url.startsWith('blob:')) {
        URL.revokeObjectURL(url);
    }
}

/** Wraps raw bytes from C# in a blob URL - the path taken when a custom downloader fetched them. */
export function createBlobUrl(bytes, contentType) {
    const blob = contentType ? new Blob([bytes], { type: contentType }) : new Blob([bytes]);
    return URL.createObjectURL(blob);
}

/**
 * Resolves true once the URL has decoded as an image, false if it cannot.
 *
 * This exists instead of Blazor's @onload/@onerror on the <img>. The load and error events do not
 * bubble, so they do not survive Blazor's delegated event plumbing reliably - and a component that
 * silently never learns its image arrived stays on the spinner forever. Decoding here is also
 * strictly cheaper than it looks: the URL is either a local blob or already sitting in the HTTP
 * cache from the fetch that produced it.
 */
export function preload(url) {
    return new Promise(resolve => {
        if (!url) {
            resolve(false);
            return;
        }

        const img = new Image();
        img.onload = () => resolve(true);
        img.onerror = () => resolve(false);
        img.src = url;
    });
}

/**
 * Streams an image, reporting progress, and returns a blob URL.
 *
 * Returns { url, contentLength, deferToBrowser, error }. `deferToBrowser` means the caller should
 * put the original URL straight into the <img> - the usual cause is a cross-origin server that
 * sends no CORS headers, which blocks fetch but not <img>.
 */
export async function load(dotNetRef, url, requestId) {
    const controller = new AbortController();
    pending.set(requestId, controller);

    try {
        const response = await fetch(url, { signal: controller.signal, credentials: 'same-origin' });

        if (!response.ok) {
            return { url: null, contentLength: 0, deferToBrowser: false, error: `HTTP ${response.status}` };
        }

        const header = response.headers.get('Content-Length');
        const total = header ? parseInt(header, 10) : 0;

        // No readable body (older browsers, or an opaque response) means nothing to count, so fall
        // straight through to a normal <img> load rather than buffering blind.
        if (!response.body || !response.body.getReader) {
            return { url: null, contentLength: total, deferToBrowser: true, error: null };
        }

        const reader = response.body.getReader();
        const chunks = [];
        let received = 0;
        let lastReport = 0;
        let lastPercent = -1;

        // Reporting every chunk means a render pass per network packet. One percent or 100ms -
        // whichever comes first - is smooth to the eye and leaves the interop channel alone.
        const report = () => {
            const now = performance.now();
            const percent = total > 0 ? received / total : -1;

            if (now - lastReport < MIN_REPORT_INTERVAL_MS && (percent < 0 || percent - lastPercent < MIN_REPORT_DELTA)) {
                return;
            }

            lastReport = now;
            lastPercent = percent;
            dotNetRef.invokeMethodAsync('OnProgress', received, total);
        };

        for (;;) {
            const { done, value } = await reader.read();
            if (done) {
                break;
            }

            chunks.push(value);
            received += value.length;
            report();
        }

        const blob = new Blob(chunks, { type: response.headers.get('Content-Type') || '' });
        return { url: URL.createObjectURL(blob), contentLength: received, deferToBrowser: false, error: null };
    }
    catch (ex) {
        if (ex && ex.name === 'AbortError') {
            return { url: null, contentLength: 0, deferToBrowser: false, error: 'aborted' };
        }

        // Almost always CORS. The browser can still render this image through a plain <img>, so
        // losing the percentage is the whole cost of the failure.
        return { url: null, contentLength: 0, deferToBrowser: true, error: String(ex) };
    }
    finally {
        pending.delete(requestId);
    }
}
