/*
 * Shiny.Blazor.Controls - SplashScreen
 *
 * A pre-boot splash screen. This is deliberately a CLASSIC script (not an ES module) and
 * exposes a global `shinySplash` so it can run and paint BEFORE Blazor has started - the
 * whole point of the control is to be on screen while the framework is still booting.
 *
 * Load it in index.html BEFORE blazor.webassembly.js / blazor.webview.js.
 *
 * The host element must live OUTSIDE #app. Blazor clears #app's contents the moment it
 * attaches the root component, which happens well before the app has loaded its data - a
 * splash inside #app therefore vanishes too early and you get a blank flash.
 */
(function () {
    'use strict';

    var DEFAULTS = {
        hostId: 'shiny-splash',
        title: null,
        subtitle: null,
        logo: null,
        logoAlt: '',
        spinner: 'ring',            // ring | dots | bar | pulse | none
        status: null,
        progress: null,             // null/undefined => indeterminate
        background: null,
        foreground: null,
        accent: null,
        muted: null,
        cssClass: null,
        minDurationMs: 0,
        fadeMs: 250,
        failSafeMs: 30000,          // 0 disables. Guards against a boot failure pinning the splash forever.
        blazorLoadProgress: false,  // WASM only: mirror --blazor-load-percentage into the progress bar
        removeOnHide: true,
        lockScroll: true
    };

    var state = null;

    function num(value, fallback) {
        var n = parseFloat(value);
        return isNaN(n) ? fallback : n;
    }

    function bool(value, fallback) {
        if (value === null || value === undefined || value === '') return fallback;
        if (typeof value === 'boolean') return value;
        return value !== 'false' && value !== '0';
    }

    function el(tag, cls, parent) {
        var e = document.createElement(tag);
        if (cls) e.className = cls;
        if (parent) parent.appendChild(e);
        return e;
    }

    function merge(options) {
        var opts = {};
        for (var k in DEFAULTS) {
            if (Object.prototype.hasOwnProperty.call(DEFAULTS, k)) opts[k] = DEFAULTS[k];
        }
        if (options) {
            for (var j in options) {
                if (Object.prototype.hasOwnProperty.call(options, j) && options[j] !== undefined) opts[j] = options[j];
            }
        }
        return opts;
    }

    function setVar(host, name, value) {
        if (value) host.style.setProperty(name, value);
    }

    // Builds the stock content. Skipped entirely when the host already has markup in it -
    // that is the "bring your own splash" path; we only own show/status/progress/hide.
    function buildContent(host, opts) {
        var panel = el('div', 'shiny-splash-panel', host);

        if (opts.logo) {
            var img = el('img', 'shiny-splash-logo', panel);
            img.src = opts.logo;
            img.alt = opts.logoAlt || '';
        }

        if (opts.title) el('div', 'shiny-splash-title', panel).textContent = opts.title;
        if (opts.subtitle) el('div', 'shiny-splash-subtitle', panel).textContent = opts.subtitle;

        if (opts.spinner === 'ring') {
            el('div', 'shiny-splash-ring', panel);
        }
        else if (opts.spinner === 'dots') {
            var dots = el('div', 'shiny-splash-dots', panel);
            el('span', null, dots);
            el('span', null, dots);
            el('span', null, dots);
        }
        else if (opts.spinner === 'pulse') {
            el('div', 'shiny-splash-pulse', panel);
        }

        // A bar spinner and a determinate progress bar are the same element; the
        // indeterminate class on the host is what makes it sweep instead of fill.
        var wantsProgress = opts.spinner === 'bar'
            || bool(opts.blazorLoadProgress, false)
            || (opts.progress !== null && opts.progress !== undefined);

        if (wantsProgress) {
            var track = el('div', 'shiny-splash-progress', panel);
            track.setAttribute('data-shiny-splash-progress', '');
            var fill = el('div', 'shiny-splash-progress-fill', track);
            fill.setAttribute('data-shiny-splash-progress-fill', '');
        }

        var status = el('div', 'shiny-splash-status', panel);
        status.setAttribute('data-shiny-splash-status', '');
        if (opts.status) status.textContent = opts.status;
    }

    function applyProgress(value) {
        if (!state) return;
        var host = state.host;

        if (value === null || value === undefined || isNaN(value)) {
            host.classList.add('shiny-splash--indeterminate');
            host.style.removeProperty('--shiny-splash-progress');
            host.style.removeProperty('--shiny-splash-progress-pct');
            host.removeAttribute('aria-valuenow');
            if (state.percentEl) state.percentEl.textContent = '';
            if (state.fillEl) state.fillEl.style.removeProperty('width');
            return;
        }

        var v = Math.max(0, Math.min(1, value));
        var pct = Math.round(v * 100);
        host.classList.remove('shiny-splash--indeterminate');
        host.style.setProperty('--shiny-splash-progress', String(v));
        host.style.setProperty('--shiny-splash-progress-pct', pct + '%');
        host.setAttribute('aria-valuenow', String(pct));
        if (state.fillEl) state.fillEl.style.width = pct + '%';
        if (state.percentEl) state.percentEl.textContent = pct + '%';
    }

    // WASM only. Blazor writes --blazor-load-percentage onto :root while it downloads the
    // runtime + assemblies. There is no such phase in a Blazor Hybrid WebView, so this is a
    // no-op there (the variable never appears and progress stays indeterminate).
    function trackBlazorLoad() {
        if (!state) return;
        state.rafId = 0;

        var raw = getComputedStyle(document.documentElement).getPropertyValue('--blazor-load-percentage');
        var pct = parseFloat(raw);
        if (!isNaN(pct)) {
            applyProgress(pct / 100);
            // Download finished - the app owns the bar from here.
            if (pct >= 100) return;
        }
        state.rafId = requestAnimationFrame(trackBlazorLoad);
    }

    function stopTracking() {
        if (state && state.rafId) {
            cancelAnimationFrame(state.rafId);
            state.rafId = 0;
        }
    }

    function destroy() {
        if (!state) return;
        var host = state.host;

        if (state.rafId) cancelAnimationFrame(state.rafId);
        if (state.failSafeId) clearTimeout(state.failSafeId);
        if (state.lockScroll) document.documentElement.classList.remove('shiny-splash-locked');

        if (state.removeOnHide) {
            if (host.parentNode) host.parentNode.removeChild(host);
        }
        else {
            host.classList.remove('shiny-splash--hiding');
            host.style.display = 'none';
            host.setAttribute('hidden', '');
            host.setAttribute('aria-hidden', 'true');
        }

        state = null;
        document.dispatchEvent(new CustomEvent('shiny-splash-hidden'));
    }

    function show(options) {
        if (state) return;

        // Called from <head> before <body> exists - defer rather than throw.
        if (!document.body) {
            document.addEventListener('DOMContentLoaded', function () { show(options); });
            return;
        }

        var opts = merge(options);
        var host = document.getElementById(opts.hostId);
        if (!host) {
            host = document.createElement('div');
            host.id = opts.hostId;
            document.body.appendChild(host);
        }

        host.removeAttribute('hidden');
        host.removeAttribute('aria-hidden');
        host.style.removeProperty('display');
        host.classList.add('shiny-splash');
        host.classList.remove('shiny-splash--hiding');
        if (opts.cssClass) host.classList.add(opts.cssClass);

        host.setAttribute('role', 'progressbar');
        host.setAttribute('aria-live', 'polite');
        host.setAttribute('aria-busy', 'true');
        host.setAttribute('aria-label', opts.title || 'Loading');

        setVar(host, '--shiny-splash-bg', opts.background);
        setVar(host, '--shiny-splash-fg', opts.foreground);
        setVar(host, '--shiny-splash-accent', opts.accent);
        setVar(host, '--shiny-splash-muted', opts.muted);

        if (!host.firstElementChild) buildContent(host, opts);

        state = {
            host: host,
            shownAt: (performance && performance.now) ? performance.now() : Date.now(),
            minDurationMs: num(opts.minDurationMs, 0),
            fadeMs: num(opts.fadeMs, 250),
            removeOnHide: bool(opts.removeOnHide, true),
            lockScroll: bool(opts.lockScroll, true),
            statusEl: host.querySelector('[data-shiny-splash-status]'),
            fillEl: host.querySelector('[data-shiny-splash-progress-fill]'),
            percentEl: host.querySelector('[data-shiny-splash-percent]'),
            hiding: false,
            rafId: 0,
            failSafeId: 0
        };

        if (state.lockScroll) document.documentElement.classList.add('shiny-splash-locked');
        if (opts.status && state.statusEl) state.statusEl.textContent = opts.status;
        applyProgress(opts.progress);

        var failSafe = num(opts.failSafeMs, 0);
        if (failSafe > 0) {
            state.failSafeId = setTimeout(function () {
                if (!state) return;
                console.warn('[shinySplash] fail-safe fired after ' + failSafe + 'ms - hide() was never called. ' +
                    'The app most likely failed to start. Set failSafeMs: 0 to disable.');
                hide(0);
            }, failSafe);
        }

        if (bool(opts.blazorLoadProgress, false)) trackBlazorLoad();
    }

    function status(text) {
        if (state && state.statusEl) state.statusEl.textContent = text || '';
    }

    function progress(value) {
        // An explicit call always wins over the Blazor download tracker.
        stopTracking();
        applyProgress(value);
    }

    function hide(fadeMs) {
        if (!state || state.hiding) return;

        var now = (performance && performance.now) ? performance.now() : Date.now();
        var elapsed = now - state.shownAt;
        if (elapsed < state.minDurationMs) {
            // Too fast to be seen as anything but a flicker - hold, then hide.
            setTimeout(function () { hide(fadeMs); }, state.minDurationMs - elapsed);
            return;
        }

        state.hiding = true;
        if (state.rafId) { cancelAnimationFrame(state.rafId); state.rafId = 0; }
        if (state.failSafeId) { clearTimeout(state.failSafeId); state.failSafeId = 0; }

        var fade = (fadeMs === null || fadeMs === undefined) ? state.fadeMs : num(fadeMs, state.fadeMs);
        state.host.style.setProperty('--shiny-splash-fade-ms', fade + 'ms');
        state.host.setAttribute('aria-busy', 'false');
        state.host.classList.add('shiny-splash--hiding');

        if (fade > 0) setTimeout(destroy, fade);
        else destroy();
    }

    function isVisible() {
        return !!state && !state.hiding;
    }

    // Declarative bootstrap: <div id="shiny-splash" data-shiny-splash data-title="My App"></div>
    // means index.html needs no <script> block of its own.
    function autoStart() {
        var host = document.querySelector('[data-shiny-splash]');
        if (!host || state) return;

        var d = host.dataset;
        show({
            hostId: host.id || 'shiny-splash',
            title: d.title || null,
            subtitle: d.subtitle || null,
            logo: d.logo || null,
            logoAlt: d.logoAlt || '',
            spinner: d.spinner || DEFAULTS.spinner,
            status: d.status || null,
            background: d.background || null,
            foreground: d.foreground || null,
            accent: d.accent || null,
            muted: d.muted || null,
            cssClass: d.cssClass || null,
            minDurationMs: num(d.minDuration, DEFAULTS.minDurationMs),
            fadeMs: num(d.fade, DEFAULTS.fadeMs),
            failSafeMs: num(d.failSafe, DEFAULTS.failSafeMs),
            blazorLoadProgress: bool(d.blazorProgress, false),
            removeOnHide: bool(d.removeOnHide, true),
            lockScroll: bool(d.lockScroll, true)
        });
    }

    window.shinySplash = {
        show: show,
        status: status,
        progress: progress,
        hide: hide,
        isVisible: isVisible
    };

    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', autoStart);
    else
        autoStart();
})();
