// Carousel controller. One instance per viewport element.
// Handles native scroll-snap scaling + index detection (snap mode) and the
// auto-play timer with pause-on-hover / focus / tab-visibility for every mode.
const states = new WeakMap();

function clamp(v, min, max) { return Math.max(min, Math.min(max, v)); }

export function init(viewport, track, dotnetRef, options) {
    if (!viewport || !track) return;

    const state = {
        viewport,
        track,
        dotnet: dotnetRef,
        opts: normalize(options),
        ticking: false,
        activeIndex: options.startIndex ?? 0,
        paused: false,
        hovered: false,
        focused: false,
        timer: null,
        suppressUntil: 0,
    };
    states.set(viewport, state);

    if (state.opts.mode === 'snap') {
        state.onScroll = () => {
            // user interaction restarts the auto-play countdown
            bumpTimer(state);
            if (state.ticking) return;
            state.ticking = true;
            requestAnimationFrame(() => {
                state.ticking = false;
                onSnapScroll(state);
            });
        };
        track.addEventListener('scroll', state.onScroll, { passive: true });
        // jump to the starting item without animation, then paint scales
        centerOn(state, state.activeIndex, false);
        requestAnimationFrame(() => onSnapScroll(state, true));
    }

    wirePause(state);
    startTimer(state);
}

function normalize(o) {
    return {
        mode: o.mode || 'snap',
        orientation: o.orientation === 'v' ? 'v' : 'h',
        autoPlay: !!o.autoPlay,
        interval: Math.max(500, o.interval ?? 4000),
        pauseOnHover: !!o.pauseOnHover,
        loop: !!o.loop,
        enableScaling: !!o.enableScaling,
        focusedScale: o.focusedScale ?? 1,
        unfocusedScale: o.unfocusedScale ?? 0.82,
        count: o.count ?? 0,
    };
}

// ----- snap mode: scaling + active-index detection -----

function onSnapScroll(state, force) {
    const { track, opts } = state;
    const horizontal = opts.orientation === 'h';
    const center = horizontal
        ? track.scrollLeft + track.clientWidth / 2
        : track.scrollTop + track.clientHeight / 2;
    const half = (horizontal ? track.clientWidth : track.clientHeight) / 2 || 1;

    let nearest = 0;
    let nearestDist = Infinity;
    const children = track.children;

    for (let i = 0; i < children.length; i++) {
        const el = children[i];
        const elCenter = horizontal
            ? el.offsetLeft + el.offsetWidth / 2
            : el.offsetTop + el.offsetHeight / 2;
        const dist = Math.abs(elCenter - center);
        if (dist < nearestDist) { nearestDist = dist; nearest = i; }

        if (opts.enableScaling) {
            const t = clamp(dist / half, 0, 1);
            const scale = opts.focusedScale - (opts.focusedScale - opts.unfocusedScale) * t;
            el.style.transform = `scale(${scale})`;
        }
    }

    if ((force || nearest !== state.activeIndex) && performance.now() >= state.suppressUntil) {
        if (nearest !== state.activeIndex) {
            state.activeIndex = nearest;
            if (state.dotnet) state.dotnet.invokeMethodAsync('OnActiveIndexChanged', nearest);
        }
    } else if (nearest !== state.activeIndex && performance.now() < state.suppressUntil) {
        // mid programmatic scroll: track visual index but stay quiet
        state.activeIndex = nearest;
    }
}

function centerOn(state, index, smooth) {
    const { track, opts } = state;
    const el = track.children[index];
    if (!el) return;
    const horizontal = opts.orientation === 'h';
    const behavior = smooth ? 'smooth' : 'auto';
    if (horizontal) {
        const left = el.offsetLeft - (track.clientWidth - el.offsetWidth) / 2;
        track.scrollTo({ left, behavior });
    } else {
        const top = el.offsetTop - (track.clientHeight - el.offsetHeight) / 2;
        track.scrollTo({ top, behavior });
    }
}

// ----- auto-play timer -----

function startTimer(state) {
    stopTimer(state);
    if (!state.opts.autoPlay) return;
    state.timer = setInterval(() => {
        if (state.paused || document.hidden) return;
        if (state.dotnet) state.dotnet.invokeMethodAsync('OnAutoTick');
    }, state.opts.interval);
}

function stopTimer(state) {
    if (state.timer) { clearInterval(state.timer); state.timer = null; }
}

// restart the countdown so a slide gets its full dwell time after interaction
function bumpTimer(state) {
    if (state.opts.autoPlay && !state.paused) startTimer(state);
}

function wirePause(state) {
    const { viewport, opts } = state;

    const refresh = () => { state.paused = (opts.pauseOnHover && (state.hovered || state.focused)); };

    if (opts.pauseOnHover) {
        state.onEnter = () => { state.hovered = true; refresh(); };
        state.onLeave = () => { state.hovered = false; refresh(); };
        state.onFocusIn = () => { state.focused = true; refresh(); };
        state.onFocusOut = () => { state.focused = false; refresh(); };
        viewport.addEventListener('pointerenter', state.onEnter);
        viewport.addEventListener('pointerleave', state.onLeave);
        viewport.addEventListener('focusin', state.onFocusIn);
        viewport.addEventListener('focusout', state.onFocusOut);
    }

    state.onVisibility = () => { if (!document.hidden) bumpTimer(state); };
    document.addEventListener('visibilitychange', state.onVisibility);
}

// ----- public API called from C# -----

export function goTo(viewport, index) {
    const state = states.get(viewport);
    if (!state) return;
    if (state.opts.mode === 'snap') {
        state.suppressUntil = performance.now() + 700;
        state.activeIndex = index;
        centerOn(state, index, true);
    }
    bumpTimer(state); // manual move restarts the dwell countdown
}

export function update(viewport, options) {
    const state = states.get(viewport);
    if (!state) return;
    const wasScaling = state.opts.enableScaling;
    state.opts = normalize(options);
    if (state.opts.mode === 'snap') {
        if (wasScaling && !state.opts.enableScaling) {
            for (const el of state.track.children) el.style.transform = '';
        }
        onSnapScroll(state, true);
    }
    startTimer(state);
}

export function dispose(viewport) {
    const state = states.get(viewport);
    if (!state) return;
    stopTimer(state);
    if (state.onScroll) state.track.removeEventListener('scroll', state.onScroll);
    if (state.opts.pauseOnHover) {
        viewport.removeEventListener('pointerenter', state.onEnter);
        viewport.removeEventListener('pointerleave', state.onLeave);
        viewport.removeEventListener('focusin', state.onFocusIn);
        viewport.removeEventListener('focusout', state.onFocusOut);
    }
    document.removeEventListener('visibilitychange', state.onVisibility);
    states.delete(viewport);
}
