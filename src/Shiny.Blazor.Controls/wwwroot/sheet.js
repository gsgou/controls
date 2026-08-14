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

function snapInternal(state, ratio, animate) {
    const targetY = detentY(state, ratio);
    state.currentY = targetY;
    state.snappedRatio = ratio;
    state.sheet.style.transition = animate ? `transform ${state.duration}ms cubic-bezier(.2,.8,.2,1)` : 'none';
    applyTransform(state.sheet, targetY);
    state.dotnet.invokeMethodAsync('OnBackdropOpacity', backdropOpacityFor(targetY, state.height));
    state.dotnet.invokeMethodAsync('OnDetentChanged', ratio);
}

export function init(root, sheet, handle, dotnetRef, ratios, duration, locked, isBottom, headerEl) {
    const state = {
        root, sheet, handle,
        dotnet: dotnetRef,
        ratios: [...ratios].sort((a, b) => a - b),
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
        // Re-derive the offset against the new viewport, otherwise the sheet keeps a pixel offset
        // measured against the old one and lands at the wrong detent.
        if (state.snappedRatio !== null)
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
    state.ratios = [...ratios].sort((a, b) => a - b);
    if (isBottom !== undefined) state.isBottom = isBottom;
    state.height = state.root.clientHeight || window.innerHeight;

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
    requestAnimationFrame(() => snapInternal(state, state.ratios[0], true));
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
    states.delete(sheet);
}
