// Parallax scroll controller. One instance per parallax-list element.
const states = new WeakMap();

function clamp(v, min, max) { return Math.max(min, Math.min(max, v)); }

export function init(scrollEl, heroEl, dotnetRef, options) {
    if (!scrollEl || !heroEl) return;

    const state = {
        scrollEl,
        heroEl,
        dotnet: dotnetRef,
        factor: options.factor ?? 0.5,
        headerHeight: options.headerHeight ?? 240,
        minHeaderHeight: options.minHeaderHeight ?? 0,
        collapse: !!options.collapse,
        fade: !!options.fade,
        ticking: false,
        lastOffset: -1,
    };
    states.set(scrollEl, state);

    const onScroll = () => {
        if (state.ticking) return;
        state.ticking = true;
        requestAnimationFrame(() => {
            state.ticking = false;
            update(state);
        });
    };
    state.onScroll = onScroll;
    scrollEl.addEventListener('scroll', onScroll, { passive: true });
    update(state);
}

function update(state) {
    const offset = Math.max(0, state.scrollEl.scrollTop);
    if (offset === state.lastOffset) return;
    state.lastOffset = offset;

    let translation = -offset * state.factor;
    const travel = state.headerHeight - state.minHeaderHeight;
    if (state.collapse && travel > 0 && translation < -travel) {
        translation = -travel;
    }
    state.heroEl.style.transform = `translate3d(0,${translation}px,0)`;

    if (state.fade && state.headerHeight > 0) {
        const fade = clamp(1 - offset / state.headerHeight, 0, 1);
        state.heroEl.style.opacity = String(fade);
    }

    if (state.dotnet) {
        const visible = Math.max(state.minHeaderHeight, state.headerHeight + translation);
        state.dotnet.invokeMethodAsync('OnScrollFromJs', offset, translation, visible);
    }
}

export function update_(scrollEl, options) {
    const state = states.get(scrollEl);
    if (!state) return;

    const changed =
        (options.factor != null && options.factor !== state.factor) ||
        (options.headerHeight != null && options.headerHeight !== state.headerHeight) ||
        (options.minHeaderHeight != null && options.minHeaderHeight !== state.minHeaderHeight) ||
        (options.collapse != null && !!options.collapse !== state.collapse) ||
        (options.fade != null && !!options.fade !== state.fade);
    if (!changed) return;

    if (options.factor != null) state.factor = options.factor;
    if (options.headerHeight != null) state.headerHeight = options.headerHeight;
    if (options.minHeaderHeight != null) state.minHeaderHeight = options.minHeaderHeight;
    if (options.collapse != null) state.collapse = !!options.collapse;
    if (options.fade != null) state.fade = !!options.fade;
    state.lastOffset = -1;
    if (!state.fade) state.heroEl.style.opacity = '';
    update(state);
}

export function dispose(scrollEl) {
    const state = states.get(scrollEl);
    if (!state) return;
    scrollEl.removeEventListener('scroll', state.onScroll);
    states.delete(scrollEl);
}
