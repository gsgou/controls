// Two unrelated jobs share this module because they share a component: drawing the local
// challenge, and driving the hosted providers (reCAPTCHA / hCaptcha / Turnstile), which differ
// only in a script URL, a global name and a couple of option keys.

const widgets = new WeakMap();
const scripts = new Map();

// ---------------------------------------------------------------------------
// local challenge
// ---------------------------------------------------------------------------

/**
 * Draws the challenge text with enough per-character jitter to beat naive OCR, while staying
 * readable to a person. The text never enters the DOM, so it cannot be scraped from the markup.
 */
export function drawChallenge(canvas, text, options) {
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const dark = options?.dark || (options?.followSystem &&
        window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);

    // draw at device resolution, lay out in CSS pixels
    const dpr = window.devicePixelRatio || 1;
    const w = options?.width || canvas.width;
    const h = options?.height || canvas.height;

    canvas.width = Math.round(w * dpr);
    canvas.height = Math.round(h * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, w, h);

    // read the widget's own CSS variables so the drawing cannot drift from the chrome around it —
    // theme pack, forced Theme="Dark" and prefers-color-scheme all land here already resolved
    const style = getComputedStyle(canvas);
    const bg = style.getPropertyValue('--shiny-captcha-well').trim() || (dark ? '#1F2937' : '#F3F4F6');
    const fg = style.getPropertyValue('--shiny-captcha-on-surface').trim() || (dark ? '#F9FAFB' : '#111827');

    ctx.fillStyle = bg;
    ctx.fillRect(0, 0, w, h);

    // globalAlpha rather than an rgba() literal, because fg arrives in whatever format the theme
    // wrote it in and there is nothing to parse an alpha channel into
    ctx.save();
    ctx.globalAlpha = 0.22;
    ctx.strokeStyle = fg;
    ctx.fillStyle = fg;

    // background streaks first, so they sit under the glyphs rather than obscuring them
    ctx.lineWidth = 1;
    for (let i = 0; i < 5; i++) {
        ctx.beginPath();
        ctx.moveTo(rand(0, w), rand(0, h));
        ctx.bezierCurveTo(rand(0, w), rand(0, h), rand(0, w), rand(0, h), rand(0, w), rand(0, h));
        ctx.stroke();
    }

    for (let i = 0; i < 40; i++)
        ctx.fillRect(rand(0, w), rand(0, h), 1.5, 1.5);

    ctx.restore();

    const chars = [...(text || '')];
    if (chars.length === 0) return;

    const slot = w / (chars.length + 1);
    const fontSize = Math.min(h * 0.62, slot * 1.15);

    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillStyle = fg;

    chars.forEach((ch, i) => {
        const x = slot * (i + 1);
        const y = h / 2 + rand(-h * 0.09, h * 0.09);

        ctx.save();
        ctx.translate(x, y);
        ctx.rotate(rand(-0.34, 0.34));
        ctx.font = `${rand(0, 1) > 0.5 ? '700' : '600'} ${fontSize + rand(-2, 3)}px ui-monospace, "SFMono-Regular", Menlo, monospace`;
        ctx.fillText(ch, 0, 0);
        ctx.restore();
    });

    // one stroke over the top, so a segmentation pass cannot cleanly split the glyphs
    ctx.globalAlpha = 0.5;
    ctx.strokeStyle = fg;
    ctx.lineWidth = 1.4;
    ctx.beginPath();
    ctx.moveTo(0, rand(h * 0.35, h * 0.65));
    ctx.bezierCurveTo(w * 0.33, rand(0, h), w * 0.66, rand(0, h), w, rand(h * 0.35, h * 0.65));
    ctx.stroke();
}

function rand(min, max) {
    return min + Math.random() * (max - min);
}

// ---------------------------------------------------------------------------
// hosted providers
// ---------------------------------------------------------------------------

export async function renderRemote(el, dotnetRef, opts) {
    if (!el) return;

    const scriptUrl = buildScriptUrl(opts);

    try {
        await loadScript(scriptUrl);
        const api = await waitForGlobal(opts.globalName, opts.useReady);

        const params = {
            sitekey: opts.siteKey,
            theme: resolveTheme(opts.theme),
            size: resolveSize(opts.size, opts.supportedSizes),
            callback: token => dotnetRef.invokeMethodAsync('OnSolvedFromJs', token || ''),
            'expired-callback': () => dotnetRef.invokeMethodAsync('OnExpiredFromJs'),
            'error-callback': () => dotnetRef.invokeMethodAsync('OnErrorFromJs', 'The captcha widget reported an error.')
        };

        if (opts.supportsBadge && params.size === 'invisible' && opts.badge)
            params.badge = opts.badge;

        if (opts.languageAsRenderOption && opts.language)
            params.language = opts.language;

        const id = api.render(el, params);
        widgets.set(el, { api, id, dotnetRef });
    }
    catch (e) {
        // a blocked script or a bad site key must surface as a component error, not a dead widget
        dotnetRef.invokeMethodAsync('OnErrorFromJs', e?.message || String(e));
    }
}

export function resetRemote(el) {
    const state = widgets.get(el);
    if (state?.api?.reset) state.api.reset(state.id);
}

export function executeRemote(el) {
    const state = widgets.get(el);
    if (state?.api?.execute) state.api.execute(state.id);
}

export function disposeRemote(el) {
    const state = widgets.get(el);
    if (!state) return;

    // Turnstile has an explicit remove; the others only have reset. Either way the element itself
    // is about to go, so failures here are not interesting.
    try {
        if (state.api?.remove) state.api.remove(state.id);
        else if (state.api?.reset) state.api.reset(state.id);
    }
    catch { /* widget already torn down */ }

    widgets.delete(el);
}

function buildScriptUrl(opts) {
    const url = opts.scriptUrl || '';
    const lang = !opts.languageAsRenderOption && opts.language ? `&hl=${encodeURIComponent(opts.language)}` : '';
    return url.replace('{lang}', lang);
}

function loadScript(url) {
    // one script per document no matter how many widgets are on the page
    let pending = scripts.get(url);
    if (pending) return pending;

    pending = new Promise((resolve, reject) => {
        const existing = document.querySelector(`script[src="${url}"]`);
        if (existing) {
            resolve();
            return;
        }

        const el = document.createElement('script');
        el.src = url;
        el.async = true;
        el.defer = true;
        el.onload = () => resolve();
        el.onerror = () => reject(new Error(`Could not load the captcha script from ${url}. It may be blocked by an extension, a firewall or a content security policy.`));
        document.head.appendChild(el);
    });

    scripts.set(url, pending);
    return pending;
}

function waitForGlobal(name, useReady) {
    return new Promise((resolve, reject) => {
        const started = Date.now();

        const tick = () => {
            const api = window[name];
            if (api && typeof api.render === 'function') {
                // reCAPTCHA defines the global before it is usable — ready() is the real gate
                if (useReady && typeof api.ready === 'function') api.ready(() => resolve(api));
                else resolve(api);
                return;
            }

            if (Date.now() - started > 15000) {
                reject(new Error(`The captcha script loaded but "${name}" never appeared.`));
                return;
            }

            setTimeout(tick, 50);
        };

        tick();
    });
}

function resolveTheme(theme) {
    if (theme === 'dark' || theme === 'light') return theme;
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function resolveSize(size, supported) {
    const list = supported && supported.length ? supported : ['normal', 'compact', 'invisible'];
    return list.includes(size) ? size : 'normal';
}
