// Sheet drag/snap controller. One instance per sheet element.
const states = new WeakMap();

function clamp(v, min, max) { return Math.max(min, Math.min(max, v)); }

// Every move of the sheet also republishes how much of it is on screen. The CSS sizes
// .shiny-sheet-viewport off this, so content stays inside the visible band while dragging and not
// just at the snapped detents.
function applyTransform(sheet, y) {
    sheet.style.transform = `translateY(${y}px)`;
    const state = states.get(sheet);
    if (state && state.height > 0)
        sheet.style.setProperty('--sheet-visible-height', `${clamp(state.height - Math.abs(y), 0, state.height)}px`);
}

function offScreenY(state) {
    return state.isBottom ? state.height : -state.height;
}

function detentY(state, ratio) {
    return (state.isBottom ? 1 : -1) * state.height * (1 - ratio);
}

function minimizedY(state) {
    if (!state.headerEl) return offScreenY(state);
    const headerHeight = state.headerEl.offsetHeight || 0;
    if (headerHeight <= 0) return offScreenY(state);
    return state.isBottom
        ? state.height - headerHeight
        : -(state.height - headerHeight);
}

function backdropOpacityFor(y, height) {
    const progress = 1 - (Math.abs(y) / height);
    return clamp(progress * 0.5, 0, 0.5);
}

// ---- Foldables -----------------------------------------------------------------------------------
// Tabletop posture: a horizontal fold splits the viewport into a top and a bottom segment (the Viewport
// Segments API - Chromium on Android foldables; undefined everywhere else, which makes all of this a
// no-op). The fold detent is the ratio that puts the sheet's edge exactly on the fold.
// A simulated tabletop (class shiny-fold-sim-tabletop on <html>, with --shiny-fold-sim-top/-bottom) reads
// the same way, so the behaviour can be tested on a device that does not fold.
function viewportSegments() {
    const html = document.documentElement;
    if (html.classList.contains('shiny-fold-sim-tabletop')) {
        const css = getComputedStyle(html);
        const top = parseFloat(css.getPropertyValue('--shiny-fold-sim-top'));
        const bottom = parseFloat(css.getPropertyValue('--shiny-fold-sim-bottom'));
        if (!isNaN(top) && !isNaN(bottom))
            return [{ top: 0, bottom: top }, { top: bottom, bottom: window.innerHeight }];
    }
    return window.viewport && window.viewport.segments;
}

function foldRatio(state) {
    if (!state.foldDetent) return null;
    const segments = viewportSegments();
    if (!segments || segments.length !== 2) return null;
    const [first, second] = segments;
    // Side by side is a book, not a tabletop - a bottom sheet across both halves is what the system does.
    if (first.bottom > second.top) return null;
    const root = state.root.getBoundingClientRect();
    const h = state.height;
    if (!(h > 0)) return null;
    const px = state.isBottom ? root.bottom - second.top : first.bottom - root.top;
    if (px <= 0 || px >= h) return null;
    return px / h;
}

// The declared detents plus the fold one. Remembers which is which so a posture change can leave a
// detent that no longer exists.
function applyFold(state) {
    const fold = foldRatio(state);
    const ratios = [...state.declared];
    if (fold !== null && !ratios.some(r => Math.abs(r - fold) < 0.001))
        ratios.push(fold);
    state.ratios = ratios.sort((a, b) => a - b);
    state.fold = fold;
    return fold;
}

function snapInternal(state, ratio, animate) {
    const targetY = detentY(state, ratio);
    state.currentY = targetY;
    state.snappedRatio = ratio;
    state.sheet.style.transition = animate ? `transform ${state.duration}ms cubic-bezier(.2,.8,.2,1)` : 'none';
    applyTransform(state.sheet, targetY);
    state.dotnet.invokeMethodAsync('OnBackdropOpacity', backdropOpacityFor(targetY, state.height));
    state.dotnet.invokeMethodAsync('OnDetentChanged', ratio);
}

export function init(root, sheet, handle, dotnetRef, ratios, duration, locked, isBottom, headerEl, foldDetent) {
    const state = {
        root, sheet, handle,
        dotnet: dotnetRef,
        ratios: [...ratios].sort((a, b) => a - b),
        declared: [...ratios].sort((a, b) => a - b),
        foldDetent: foldDetent !== false,
        fold: null,
        duration,
        locked: !!locked,
        isBottom: isBottom !== false,
        headerEl: headerEl && headerEl.nodeType ? headerEl : null,
        height: root.clientHeight || window.innerHeight,
        currentY: 0,
        dragStartY: 0,
        dragStartTransform: 0,
        dragging: false,
        pointerId: null,
        // The detent the sheet is resting at, or null when it is hidden/minimized. Tracked so a
        // viewport resize can re-derive the transform instead of keeping a stale pixel offset.
        snappedRatio: null,
    };
    states.set(sheet, state);

    state.height = root.clientHeight || window.innerHeight;
    // Start hidden
    applyTransform(sheet, offScreenY(state));
    state.currentY = offScreenY(state);

    const onResize = () => {
        state.height = root.clientHeight || window.innerHeight;
        const wasFold = state.fold;
        const atFold = wasFold !== null && state.snappedRatio !== null && Math.abs(state.snappedRatio - wasFold) < 0.001;
        const fold = applyFold(state);
        // Into tabletop: an open sheet moves to the fold. Out of it: off the fold detent, which no
        // longer exists, to the nearest declared one.
        if (state.snappedRatio !== null && fold !== null && (atFold || wasFold === null))
            snapInternal(state, fold, true);
        else if (atFold && fold === null) {
            const current = state.snappedRatio;
            const nearest = state.declared.reduce((best, r) => Math.abs(r - current) < Math.abs(best - current) ? r : best, state.declared[0]);
            snapInternal(state, nearest, true);
        }
        // Re-derive the offset against the new viewport, otherwise the sheet keeps a pixel offset
        // measured against the old one and lands at the wrong detent.
        else if (state.snappedRatio !== null)
            snapInternal(state, state.snappedRatio, false);
        else if (state.minimized)
            minimizeInternal(state);
        else {
            state.currentY = offScreenY(state);
            applyTransform(sheet, state.currentY);
        }
    };
    window.addEventListener('resize', onResize);
    state.onResize = onResize;
    // A posture change does not always resize the window (the segments move, the viewport does not).
    if (navigator.devicePosture && navigator.devicePosture.addEventListener)
        navigator.devicePosture.addEventListener('change', onResize);
    // A simulated fold changes no window size; whoever toggles it raises this instead.
    window.addEventListener('shiny-foldchange', onResize);
    applyFold(state);

    if (state.locked) handle.style.display = 'none';

    const onDown = (ev) => {
        if (state.locked || state.pointerId !== null) return;
        state.pointerId = ev.pointerId;
        state.dragging = true;
        state.dragStartY = ev.clientY;
        state.dragStartTransform = state.currentY;
        // Between pointerdown and the snap on pointerup the sheet rests at no detent at all.
        state.snappedRatio = null;
        sheet.style.transition = 'none';
        handle.setPointerCapture(ev.pointerId);
    };
    const onMove = (ev) => {
        if (!state.dragging || ev.pointerId !== state.pointerId) return;
        const delta = ev.clientY - state.dragStartY;
        const highest = detentY(state, state.ratios[state.ratios.length - 1]);
        const off = offScreenY(state);
        let newY;
        if (state.isBottom) {
            newY = clamp(state.dragStartTransform + delta, highest - 20, off);
        } else {
            newY = clamp(state.dragStartTransform + delta, off, highest + 20);
        }
        state.currentY = newY;
        applyTransform(sheet, newY);
        state.dotnet.invokeMethodAsync('OnBackdropOpacity', backdropOpacityFor(newY, state.height));
    };
    const onUp = (ev) => {
        if (!state.dragging || ev.pointerId !== state.pointerId) return;
        state.dragging = false;
        state.pointerId = null;
        try { handle.releasePointerCapture(ev.pointerId); } catch {}

        const totalDelta = ev.clientY - state.dragStartY;
        const lowest = detentY(state, state.ratios[0]);

        // Dismiss check
        const swipeDismiss = state.isBottom ? totalDelta > 50 : totalDelta < -50;
        const pastThreshold = state.isBottom
            ? state.currentY > lowest + state.height * 0.1
            : state.currentY < lowest - state.height * 0.1;

        if (swipeDismiss && pastThreshold) {
            state.dotnet.invokeMethodAsync('OnOpenChanged', false);
            return;
        }

        // Find closest detent
        let bestRatio = state.ratios[0];
        let bestDist = Infinity;
        for (const r of state.ratios) {
            const targetY = detentY(state, r);
            const dist = Math.abs(state.currentY - targetY);
            if (dist < bestDist) { bestDist = dist; bestRatio = r; }
        }
        snapInternal(state, bestRatio, true);
    };

    handle.addEventListener('pointerdown', onDown);
    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
    handle.addEventListener('pointercancel', onUp);
    state.onDown = onDown; state.onMove = onMove; state.onUp = onUp;
}

export function open(sheet, ratios, isBottom) {
    const state = states.get(sheet);
    if (!state) return;
    state.declared = [...ratios].sort((a, b) => a - b);
    if (isBottom !== undefined) state.isBottom = isBottom;
    state.height = state.root.clientHeight || window.innerHeight;
    const fold = applyFold(state);

    state.minimized = false;

    // If not already showing (minimized), start off-screen
    const minY = minimizedY(state);
    const isCurrentlyMinimized = Math.abs(state.currentY - minY) < 5;
    if (!isCurrentlyMinimized) {
        sheet.style.transition = 'none';
        applyTransform(sheet, offScreenY(state));
        state.currentY = offScreenY(state);
        void sheet.offsetHeight; // force reflow
    }
    // Tabletop opens to the fold; otherwise the first declared detent, as always.
    requestAnimationFrame(() => snapInternal(state, fold !== null ? fold : state.ratios[0], true));
}

export function close(sheet, shouldMinimize) {
    const state = states.get(sheet);
    if (!state) return;

    const targetY = shouldMinimize ? minimizedY(state) : offScreenY(state);
    state.sheet.style.transition = `transform ${state.duration}ms cubic-bezier(.4,.0,.2,1)`;
    applyTransform(sheet, targetY);
    state.currentY = targetY;
    state.snappedRatio = null;
    state.minimized = !!shouldMinimize;
    state.dotnet.invokeMethodAsync('OnBackdropOpacity', 0);
}

function minimizeInternal(state) {
    const targetY = minimizedY(state);
    state.sheet.style.transition = 'none';
    applyTransform(state.sheet, targetY);
    state.currentY = targetY;
    state.snappedRatio = null;
    state.minimized = true;
}

export function minimize(sheet) {
    const state = states.get(sheet);
    if (!state) return;
    state.height = state.root.clientHeight || window.innerHeight;
    minimizeInternal(state);
}

export function snapTo(sheet, ratio) {
    const state = states.get(sheet);
    if (!state) return;
    snapInternal(state, ratio, true);
}

export function setLocked(sheet, locked) {
    const state = states.get(sheet);
    if (!state) return;
    state.locked = locked;
    state.handle.style.display = locked ? 'none' : '';
}

export function setDirection(sheet, isBottom) {
    const state = states.get(sheet);
    if (!state) return;
    state.isBottom = isBottom;
}

export function dispose(sheet) {
    const state = states.get(sheet);
    if (!state) return;
    state.handle.removeEventListener('pointerdown', state.onDown);
    state.handle.removeEventListener('pointermove', state.onMove);
    state.handle.removeEventListener('pointerup', state.onUp);
    state.handle.removeEventListener('pointercancel', state.onUp);
    window.removeEventListener('resize', state.onResize);
    if (navigator.devicePosture && navigator.devicePosture.removeEventListener)
        navigator.devicePosture.removeEventListener('change', state.onResize);
    window.removeEventListener('shiny-foldchange', state.onResize);
    states.delete(sheet);
}
