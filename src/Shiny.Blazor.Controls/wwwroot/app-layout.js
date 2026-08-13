// AppLayout interaction controller.
//
// Two independent pieces:
//   - initPanel: pointer-driven width dragging for one AppLayoutPanel. The listener is delegated
//     from the panel element (not the handle) so it survives the handle being re-rendered when the
//     panel changes state.
//   - observeHost: reports the layout's width back to .NET so panels can compact themselves.
//
// Widths are written straight to element.style during a drag; .NET is only told the final value on
// pointer-up, which is also what Blazor's render tree then agrees with.
const panels = new WeakMap();
const hosts = new WeakMap();

function clamp(v, min, max) {
    return Math.max(min, Math.min(max, v));
}

function numAttr(el, name, fallback) {
    const raw = parseFloat(el.getAttribute(name));
    return Number.isFinite(raw) ? raw : fallback;
}

export function initPanel(panelEl, dotnetRef) {
    if (!panelEl || panels.has(panelEl))
        return;

    const state = { panelEl, dotnet: dotnetRef, drag: null };
    panels.set(panelEl, state);

    state.onPointerDown = e => onPointerDown(state, e);
    state.onPointerMove = e => onPointerMove(state, e);
    state.onPointerUp = e => onPointerUp(state, e);

    panelEl.addEventListener('pointerdown', state.onPointerDown);
    window.addEventListener('pointermove', state.onPointerMove);
    window.addEventListener('pointerup', state.onPointerUp);
    window.addEventListener('pointercancel', state.onPointerUp);
}

export function disposePanel(panelEl) {
    const state = panels.get(panelEl);
    if (!state)
        return;

    endDrag(state, false);
    panelEl.removeEventListener('pointerdown', state.onPointerDown);
    window.removeEventListener('pointermove', state.onPointerMove);
    window.removeEventListener('pointerup', state.onPointerUp);
    window.removeEventListener('pointercancel', state.onPointerUp);
    panels.delete(panelEl);
}

function onPointerDown(state, e) {
    if (e.button !== 0)
        return;

    const handle = e.target.closest('[data-shiny-panel-resizer]');
    if (!handle || !state.panelEl.contains(handle))
        return;

    const el = state.panelEl;
    state.drag = {
        startX: e.clientX,
        startWidth: el.getBoundingClientRect().width,
        // left panels grow with the pointer, right panels grow against it
        sign: el.getAttribute('data-side') === 'right' ? -1 : 1,
        min: numAttr(el, 'data-min', 0),
        max: numAttr(el, 'data-max', Number.MAX_SAFE_INTEGER),
        pointerId: e.pointerId,
        handle
    };

    // Safari would otherwise start a text selection on mousedown that outlives the whole drag.
    e.preventDefault();
    document.getSelection()?.removeAllRanges();
    el.classList.add('is-resizing');
    document.body.style.cursor = 'col-resize';

    try { handle.setPointerCapture(e.pointerId); } catch { /* not captureable, window listeners cover it */ }
}

function onPointerMove(state, e) {
    const drag = state.drag;
    if (!drag)
        return;

    const width = clamp(drag.startWidth + (e.clientX - drag.startX) * drag.sign, drag.min, drag.max);
    state.panelEl.style.width = `${width}px`;
}

function onPointerUp(state) {
    endDrag(state, true);
}

function endDrag(state, commit) {
    const drag = state.drag;
    if (!drag)
        return;

    state.drag = null;
    state.panelEl.classList.remove('is-resizing');
    document.body.style.cursor = '';

    try { drag.handle.releasePointerCapture(drag.pointerId); } catch { /* already released */ }

    if (commit) {
        const width = state.panelEl.getBoundingClientRect().width;
        state.dotnet.invokeMethodAsync('OnResizedJs', width);
    }
}

export function observeHost(hostEl, dotnetRef) {
    if (!hostEl || hosts.has(hostEl))
        return;

    const state = { dotnet: dotnetRef, width: -1, frame: 0 };
    hosts.set(hostEl, state);

    state.observer = new ResizeObserver(entries => {
        const width = entries[0]?.contentRect?.width ?? hostEl.clientWidth;
        if (width === state.width)
            return;

        state.width = width;
        // coalesce the burst of callbacks a live window drag produces
        cancelAnimationFrame(state.frame);
        state.frame = requestAnimationFrame(() => state.dotnet.invokeMethodAsync('OnHostResizedJs', width));
    });
    state.observer.observe(hostEl);
}

export function disposeHost(hostEl) {
    const state = hosts.get(hostEl);
    if (!state)
        return;

    cancelAnimationFrame(state.frame);
    state.observer.disconnect();
    hosts.delete(hostEl);
}

export function load(key) {
    try {
        return localStorage.getItem(key);
    }
    catch {
        // private-mode Safari and disabled storage both throw here
        return null;
    }
}

export function save(key, value) {
    try {
        localStorage.setItem(key, value);
    }
    catch {
        /* nothing to do — persistence is best-effort */
    }
}
