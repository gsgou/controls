// Shiny carousel engine. One instance per viewport element.
//
// A transform-based, Embla-style engine: the container is translated by a single
// `location` scalar; slides are laid out with flexbox and measured from the DOM.
// Supports pointer drag (mouse + touch) with momentum, drag-free, alignment,
// slidesToScroll, variable widths, seamless looping (per-slide repetition offset),
// scroll-linked tweens (scale / opacity / parallax / fade), autoplay, auto-scroll,
// a scroll-position progress bar, and RTL.
//
// C# callbacks: OnEngineInit(snapCount, selectedIndex), OnSelect(selectedIndex).

const states = new WeakMap();

const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
const now = () => performance.now();

export function init(viewport, container, dotnetRef, options) {
    if (!viewport || !container) return;

    const state = {
        viewport,
        container,
        root: viewport.closest('.shiny-carousel') || viewport.parentElement,
        dotnet: dotnetRef,
        opts: normalize(options),
        // geometry (filled by measure())
        slides: [],
        positions: [],
        sizes: [],
        centers: [],
        snaps: [],
        contentSize: 0,
        loopSize: 0,
        viewportSize: 0,
        minScroll: 0,
        maxScroll: 0,
        // motion
        location: 0,
        target: 0,
        velocity: 0,
        selected: clamp(options.startIndex ?? 0, 0, 9999),
        // runtime flags
        raf: 0,
        mode: 'idle',        // 'idle' | 'settle' | 'momentum' | 'autoscroll'
        dragging: false,
        pointerId: null,
        dragStart: 0,
        dragStartLoc: 0,
        lastPointer: 0,
        lastMoveT: 0,
        moved: 0,
        timer: null,
        paused: false,
        hovered: false,
        focused: false,
    };
    states.set(viewport, state);

    measure(state);
    state.location = state.target = snapForIndex(state, state.selected);
    render(state);
    notifyInit(state);

    wireDrag(state);
    wirePause(state);
    startTimer(state);

    state.onResize = () => {
        const sel = state.selected;
        measure(state);
        state.selected = clamp(sel, 0, state.snaps.length - 1);
        state.location = state.target = snapForIndex(state, state.selected);
        render(state);
    };
    window.addEventListener('resize', state.onResize, { passive: true });
}

function normalize(o) {
    return {
        axis: o.orientation === 'v' ? 'y' : 'x',
        rtl: !!o.rtl && o.orientation !== 'v',
        effect: o.effect || 'none',
        align: o.align || 'center',
        slidesToScroll: Math.max(1, o.slidesToScroll ?? 1),
        loop: !!o.loop,
        draggable: o.draggable !== false,
        dragFree: !!o.dragFree,
        autoPlay: !!o.autoPlay,
        interval: Math.max(500, o.interval ?? 4000),
        pauseOnHover: o.pauseOnHover !== false,
        autoScroll: !!o.autoScroll,
        autoScrollSpeed: o.autoScrollSpeed ?? 40,   // px / second
        focusedScale: o.focusedScale ?? 1,
        unfocusedScale: o.unfocusedScale ?? 0.82,
        parallax: o.parallax ?? 0.4,
        minOpacity: o.minOpacity ?? 0.18,
        count: o.count ?? 0,
    };
}

// ---------- geometry ----------

function measure(state) {
    const { container, viewport, opts } = state;
    const horizontal = opts.axis === 'x';

    const children = Array.from(container.children);
    state.slides = children;

    state.viewportSize = horizontal ? viewport.clientWidth : viewport.clientHeight;
    const vp = state.viewportSize || 1;

    const cs = getComputedStyle(container);
    const gap = parseFloat(horizontal ? (cs.columnGap || cs.gap) : (cs.rowGap || cs.gap)) || 0;

    // raw main-axis position/size of each slide
    const sizes = [];
    const rawPos = [];
    for (const el of children) {
        sizes.push(horizontal ? el.offsetWidth : el.offsetHeight);
        rawPos.push(horizontal ? el.offsetLeft : el.offsetTop);
    }

    // normalise so position 0 = reading-start; mirror for RTL so the rest of the
    // engine can treat everything left-to-right / top-to-bottom.
    const containerSize = horizontal ? container.scrollWidth : container.scrollHeight;
    const positions = rawPos.map((p, i) => opts.rtl ? containerSize - p - sizes[i] : p);

    state.sizes = sizes;
    state.positions = positions;
    state.centers = positions.map((p, i) => p + sizes[i] / 2);

    const n = children.length;
    const contentSize = n ? positions[n - 1] + sizes[n - 1] - positions[0] : 0;
    state.contentSize = contentSize;
    state.loopSize = contentSize + gap;          // one repetition period (incl. trailing gap)
    state.minScroll = 0;
    state.maxScroll = Math.max(0, contentSize - vp);

    // per-slide snap target (location that brings slide i to the aligned position)
    const alignOffset = (i) => {
        switch (opts.align) {
            case 'start': return 0;
            case 'end': return vp - sizes[i];
            default: return (vp - sizes[i]) / 2;   // center
        }
    };
    const slideSnaps = positions.map((p, i) => p - alignOffset(i));

    // group into scroll snaps by slidesToScroll
    const snaps = [];
    for (let i = 0; i < n; i += opts.slidesToScroll) {
        let s = slideSnaps[i];
        if (!opts.loop) s = clamp(s, state.minScroll, state.maxScroll);
        if (!snaps.length || Math.abs(s - snaps[snaps.length - 1]) > 0.5) snaps.push(s);
    }
    if (!opts.loop && snaps.length && snaps[snaps.length - 1] < state.maxScroll - 0.5)
        snaps.push(state.maxScroll);

    state.snaps = snaps.length ? snaps : [0];
    state.slideSnaps = slideSnaps;
}

function snapForIndex(state, i) {
    return state.snaps[clamp(i, 0, state.snaps.length - 1)];
}

// circular distance helper for loop selection
function nearestSnapIndex(state, loc) {
    const { snaps, loopSize, opts } = state;
    let best = 0, bestD = Infinity;
    for (let i = 0; i < snaps.length; i++) {
        let d = Math.abs(snaps[i] - loc);
        if (opts.loop) d = Math.min(d, loopSize - d);
        if (d < bestD) { bestD = d; best = i; }
    }
    return best;
}

// ---------- rendering ----------

function render(state) {
    const { container, slides, opts } = state;
    const horizontal = opts.axis === 'x';
    const vp = state.viewportSize || 1;
    const loc = state.location;

    // fade keeps the container still and crossfades stacked slides
    if (opts.effect !== 'fade') {
        const t = opts.rtl ? loc : -loc;
        container.style.transform = horizontal
            ? `translate3d(${t}px,0,0)`
            : `translate3d(0,${t}px,0)`;
    }

    const viewportCenter = loc + vp / 2;
    for (let i = 0; i < slides.length; i++) {
        // nearest looped repetition of this slide to the viewport centre
        let loopOffset = 0;
        if (opts.loop && state.loopSize > 0) {
            const k = Math.round((viewportCenter - state.centers[i]) / state.loopSize);
            loopOffset = k * state.loopSize;
        }
        const diff = (state.centers[i] + loopOffset) - viewportCenter;
        const norm = diff / vp;     // 0 = centred, ±1 = one viewport away

        applySlide(state, slides[i], loopOffset, norm, horizontal);
    }

    updateProgress(state);
}

function applySlide(state, el, loopOffset, norm, horizontal) {
    const { opts } = state;
    const tween = el.firstElementChild || el;

    // loop repositioning lives on the outer slide element
    if (opts.effect === 'fade') {
        el.style.transform = '';
    } else if (loopOffset !== 0 || el.style.transform) {
        const o = opts.rtl ? -loopOffset : loopOffset;
        el.style.transform = horizontal
            ? `translate3d(${o}px,0,0)`
            : `translate3d(0,${o}px,0)`;
    }

    // tweens live on the inner wrapper so they compose with loop offset
    const a = Math.abs(norm);
    switch (opts.effect) {
        case 'scale': {
            const s = opts.focusedScale - (opts.focusedScale - opts.unfocusedScale) * clamp(a, 0, 1);
            tween.style.transform = `scale(${s})`;
            tween.style.opacity = '';
            break;
        }
        case 'opacity': {
            tween.style.opacity = `${clamp(1 - a, opts.minOpacity, 1)}`;
            tween.style.transform = '';
            break;
        }
        case 'parallax': {
            const px = -norm * state.viewportSize * opts.parallax;
            tween.style.transform = horizontal
                ? `translate3d(${px}px,0,0)`
                : `translate3d(0,${px}px,0)`;
            tween.style.opacity = '';
            break;
        }
        case 'fade': {
            tween.style.opacity = `${clamp(1 - a, 0, 1)}`;
            el.style.zIndex = a < 0.5 ? '1' : '0';
            el.style.pointerEvents = a < 0.5 ? 'auto' : 'none';
            tween.style.transform = '';
            break;
        }
        default:
            if (tween !== el) { tween.style.transform = ''; tween.style.opacity = ''; }
            break;
    }
}

function updateProgress(state) {
    if (!state.root) return;
    const bar = state.root.querySelector('.shiny-carousel-progress > .bar');
    if (!bar) return;
    let p;
    if (state.opts.loop && state.loopSize > 0) {
        p = ((state.location % state.loopSize) + state.loopSize) % state.loopSize / state.loopSize;
    } else {
        const span = state.maxScroll - state.minScroll;
        p = span > 0 ? clamp((state.location - state.minScroll) / span, 0, 1) : 0;
    }
    bar.style.transform = `scaleX(${p})`;
}

// ---------- selection ----------

function setSelected(state, index, fromUser) {
    if (index === state.selected) return;
    state.selected = index;
    if (state.dotnet) state.dotnet.invokeMethodAsync('OnSelect', index);
    if (fromUser) bumpTimer(state);
}

function notifyInit(state) {
    if (state.dotnet)
        state.dotnet.invokeMethodAsync('OnEngineInit', state.snaps.length, state.selected);
}

// ---------- animation ----------

function loop(state) {
    cancelAnimationFrame(state.raf);
    const step = () => {
        let keepGoing = true;
        if (state.mode === 'settle') {
            const d = state.target - state.location;
            state.location += d * 0.16;
            if (Math.abs(d) < 0.35) { state.location = state.target; state.mode = 'idle'; keepGoing = false; }
        } else if (state.mode === 'momentum') {
            state.location += state.velocity;
            state.velocity *= 0.9;
            if (!state.opts.loop) {
                if (state.location < state.minScroll) { state.location = state.minScroll; state.velocity = 0; }
                if (state.location > state.maxScroll) { state.location = state.maxScroll; state.velocity = 0; }
            }
            if (Math.abs(state.velocity) < 0.1) { state.mode = 'idle'; keepGoing = false; }
        } else if (state.mode === 'autoscroll') {
            state.location += state.velocity;
            wrapLoop(state);
        } else {
            keepGoing = false;
        }

        render(state);
        const sel = nearestSnapIndex(state, normalizedLocation(state));
        setSelected(state, sel, false);

        if (keepGoing) state.raf = requestAnimationFrame(step);
    };
    state.raf = requestAnimationFrame(step);
}

function normalizedLocation(state) {
    if (!state.opts.loop || state.loopSize <= 0) return state.location;
    return ((state.location % state.loopSize) + state.loopSize) % state.loopSize;
}

function wrapLoop(state) {
    if (!state.opts.loop || state.loopSize <= 0) return;
    // keep location numerically bounded so it never overflows
    if (state.location > state.loopSize) state.location -= state.loopSize;
    else if (state.location < -state.loopSize) state.location += state.loopSize;
}

function settleTo(state, index) {
    state.selected = clamp(index, 0, state.snaps.length - 1);
    let target = state.snaps[state.selected];
    if (state.opts.loop && state.loopSize > 0) {
        // choose the nearest repetition of the target snap to current location
        const base = state.location;
        const k = Math.round((base - target) / state.loopSize);
        target += k * state.loopSize;
    }
    state.target = target;
    state.mode = 'settle';
    if (state.dotnet) state.dotnet.invokeMethodAsync('OnSelect', state.selected);
    loop(state);
}

// ---------- drag ----------

function wireDrag(state) {
    const { viewport, opts } = state;
    if (!opts.draggable) return;

    state.onDown = (e) => {
        if (e.button != null && e.button !== 0) return;
        state.dragging = true;
        state.pointerId = e.pointerId;
        state.dragStart = axisOf(state, e);
        state.lastPointer = state.dragStart;
        state.dragStartLoc = state.location;
        state.lastMoveT = now();
        state.velocity = 0;
        state.moved = 0;
        state.mode = 'idle';
        cancelAnimationFrame(state.raf);
        viewport.setPointerCapture?.(e.pointerId);
        viewport.classList.add('is-dragging');
    };

    state.onMove = (e) => {
        if (!state.dragging || e.pointerId !== state.pointerId) return;
        const p = axisOf(state, e);
        const dxTotal = p - state.dragStart;
        state.moved = Math.abs(dxTotal);

        // reading-direction delta (mirror for RTL)
        const sign = opts.rtl ? 1 : -1;
        let newLoc = state.dragStartLoc + sign * dxTotal;

        // rubber-band beyond the edges when not looping
        if (!opts.loop) {
            if (newLoc < state.minScroll) newLoc = state.minScroll + (newLoc - state.minScroll) * 0.35;
            else if (newLoc > state.maxScroll) newLoc = state.maxScroll + (newLoc - state.maxScroll) * 0.35;
        }

        const t = now();
        const dt = Math.max(1, t - state.lastMoveT);
        state.velocity = sign * (p - state.lastPointer) / dt * 16; // px / frame
        state.lastPointer = p;
        state.lastMoveT = t;

        state.location = newLoc;
        render(state);
        const sel = nearestSnapIndex(state, normalizedLocation(state));
        setSelected(state, sel, false);
        if (state.moved > 6) e.preventDefault?.();
    };

    state.onUp = (e) => {
        if (!state.dragging || (state.pointerId != null && e.pointerId !== state.pointerId)) return;
        state.dragging = false;
        viewport.releasePointerCapture?.(e.pointerId);
        viewport.classList.remove('is-dragging');
        bumpTimer(state);

        if (state.moved > 6) suppressClick(state);

        if (opts.dragFree) {
            state.mode = 'momentum';
            loop(state);
        } else {
            const predicted = state.location + state.velocity * 6;
            settleTo(state, nearestSnapIndex(state, state.opts.loop
                ? ((predicted % state.loopSize) + state.loopSize) % state.loopSize
                : predicted));
        }
    };

    viewport.addEventListener('pointerdown', state.onDown);
    viewport.addEventListener('pointermove', state.onMove);
    viewport.addEventListener('pointerup', state.onUp);
    viewport.addEventListener('pointercancel', state.onUp);
}

function axisOf(state, e) {
    return state.opts.axis === 'x' ? e.clientX : e.clientY;
}

function suppressClick(state) {
    const block = (ev) => { ev.stopPropagation(); ev.preventDefault(); };
    state.viewport.addEventListener('click', block, { capture: true, once: true });
    setTimeout(() => state.viewport.removeEventListener('click', block, { capture: true }), 0);
}

// ---------- timers (autoplay + auto-scroll) ----------

function startTimer(state) {
    stopTimer(state);
    const { opts } = state;
    if (opts.autoScroll) {
        state.velocity = (opts.rtl ? -1 : 1) * 0; // direction handled by adding per frame
        state.mode = 'autoscroll';
        state.velocity = opts.autoScrollSpeed / 60; // px per frame at ~60fps
        loop(state);
        return;
    }
    if (opts.autoPlay) {
        state.timer = setInterval(() => {
            if (state.paused || document.hidden || state.dragging) return;
            scrollNextInternal(state);
        }, opts.interval);
    }
}

function stopTimer(state) {
    if (state.timer) { clearInterval(state.timer); state.timer = null; }
    if (state.mode === 'autoscroll') { cancelAnimationFrame(state.raf); state.mode = 'idle'; }
}

function bumpTimer(state) {
    if (state.opts.autoPlay && !state.opts.autoScroll && !state.paused) startTimer(state);
}

function wirePause(state) {
    const { viewport, opts } = state;
    const refresh = () => {
        state.paused = opts.pauseOnHover && (state.hovered || state.focused);
        if (opts.autoScroll) {
            if (state.paused) { cancelAnimationFrame(state.raf); }
            else if (state.mode !== 'autoscroll') { state.mode = 'autoscroll'; loop(state); }
        }
    };
    state.onEnter = () => { state.hovered = true; refresh(); };
    state.onLeave = () => { state.hovered = false; refresh(); };
    state.onFocusIn = () => { state.focused = true; refresh(); };
    state.onFocusOut = () => { state.focused = false; refresh(); };
    if (opts.pauseOnHover) {
        viewport.addEventListener('pointerenter', state.onEnter);
        viewport.addEventListener('pointerleave', state.onLeave);
        viewport.addEventListener('focusin', state.onFocusIn);
        viewport.addEventListener('focusout', state.onFocusOut);
    }
    state.onVisibility = () => { if (!document.hidden) { startTimer(state); } };
    document.addEventListener('visibilitychange', state.onVisibility);
}

// ---------- public API (called from C#) ----------

function scrollNextInternal(state) {
    const n = state.snaps.length;
    let next = state.selected + 1;
    if (next >= n) next = state.opts.loop ? 0 : n - 1;
    settleTo(state, next);
}

function scrollPrevInternal(state) {
    const n = state.snaps.length;
    let prev = state.selected - 1;
    if (prev < 0) prev = state.opts.loop ? n - 1 : 0;
    settleTo(state, prev);
}

export function scrollNext(viewport) { const s = states.get(viewport); if (s) { scrollNextInternal(s); bumpTimer(s); } }
export function scrollPrev(viewport) { const s = states.get(viewport); if (s) { scrollPrevInternal(s); bumpTimer(s); } }

export function scrollTo(viewport, snapIndex) {
    const s = states.get(viewport);
    if (!s) return;
    settleTo(s, snapIndex);
    bumpTimer(s);
}

export function scrollToSlide(viewport, slideIndex) {
    const s = states.get(viewport);
    if (!s) return;
    // map a slide index to the scroll snap that contains it
    const snapIndex = Math.floor(slideIndex / s.opts.slidesToScroll);
    settleTo(s, clamp(snapIndex, 0, s.snaps.length - 1));
    bumpTimer(s);
}

export function reInit(viewport, container, options) {
    const s = states.get(viewport);
    if (!s) return;
    const sel = s.selected;
    s.opts = normalize(options);
    measure(s);
    s.selected = clamp(sel, 0, s.snaps.length - 1);
    s.location = s.target = snapForIndex(s, s.selected);
    s.mode = 'idle';
    cancelAnimationFrame(s.raf);
    render(s);
    notifyInit(s);
    startTimer(s);
}

export function dispose(viewport) {
    const s = states.get(viewport);
    if (!s) return;
    cancelAnimationFrame(s.raf);
    stopTimer(s);
    const v = s.viewport;
    v.removeEventListener('pointerdown', s.onDown);
    v.removeEventListener('pointermove', s.onMove);
    v.removeEventListener('pointerup', s.onUp);
    v.removeEventListener('pointercancel', s.onUp);
    v.removeEventListener('pointerenter', s.onEnter);
    v.removeEventListener('pointerleave', s.onLeave);
    v.removeEventListener('focusin', s.onFocusIn);
    v.removeEventListener('focusout', s.onFocusOut);
    document.removeEventListener('visibilitychange', s.onVisibility);
    window.removeEventListener('resize', s.onResize);
    states.delete(viewport);
}
