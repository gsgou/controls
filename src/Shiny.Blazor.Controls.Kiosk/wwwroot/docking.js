// Dock interaction controller: splitter resize, tab drag (reorder / re-dock / tear-off),
// floating window move + resize. One delegated instance per DockHost root element.
//
// All visual feedback elements (ghost, zone overlay) are created by JS with inline
// styles — Blazor scoped CSS cannot style JS-created elements.
const states = new WeakMap();

const DRAG_THRESHOLD = 5;
const EDGE_BAND = 0.28; // fraction of group size that counts as an edge dock zone

export function init(hostEl, dotnetRef, options) {
    if (!hostEl || states.has(hostEl)) return;
    const state = {
        hostEl,
        dotnet: dotnetRef,
        locked: !!(options && options.locked),
        pointerDown: null,
        drag: null,
        resizeObserver: null,
    };
    states.set(hostEl, state);

    state.onPointerDown = e => onPointerDown(state, e);
    state.onPointerMove = e => onPointerMove(state, e);
    state.onPointerUp = e => onPointerUp(state, e);
    state.onKeyDown = e => { if (e.key === 'Escape') cancelDrag(state); };

    hostEl.addEventListener('pointerdown', state.onPointerDown);
    window.addEventListener('pointermove', state.onPointerMove);
    window.addEventListener('pointerup', state.onPointerUp);
    window.addEventListener('keydown', state.onKeyDown);

    // floating windows are user-resizable (CSS resize) — commit bounds when they settle
    state.resizeObserver = new ResizeObserver(entries => {
        if (state.locked) return;
        for (const entry of entries) {
            const idx = entry.target.getAttribute('data-dock-float');
            if (idx == null) continue;
            clearTimeout(state.resizeTimer);
            state.resizeTimer = setTimeout(() => {
                const r = relativeRect(hostEl, entry.target);
                state.dotnet.invokeMethodAsync('OnFloatingResizedJs', parseInt(idx, 10), r.width, r.height);
            }, 250);
        }
    });
    observeFloating(state);
}

export function setLocked(hostEl, locked) {
    const state = states.get(hostEl);
    if (state) state.locked = !!locked;
}

export function refreshFloating(hostEl) {
    const state = states.get(hostEl);
    if (state) observeFloating(state);
}

export function dispose(hostEl) {
    const state = states.get(hostEl);
    if (!state) return;
    cancelDrag(state);
    hostEl.removeEventListener('pointerdown', state.onPointerDown);
    window.removeEventListener('pointermove', state.onPointerMove);
    window.removeEventListener('pointerup', state.onPointerUp);
    window.removeEventListener('keydown', state.onKeyDown);
    state.resizeObserver?.disconnect();
    states.delete(hostEl);
}

function observeFloating(state) {
    state.resizeObserver.disconnect();
    state.hostEl.querySelectorAll('[data-dock-float]').forEach(el => state.resizeObserver.observe(el));
}

function relativeRect(hostEl, el) {
    const h = hostEl.getBoundingClientRect();
    const r = el.getBoundingClientRect();
    return { x: r.left - h.left, y: r.top - h.top, width: r.width, height: r.height };
}

// ---------------------------------------------------------------- pointer down
function onPointerDown(state, e) {
    if (state.locked || e.button !== 0) return;

    const splitter = e.target.closest('[data-dock-splitter]');
    if (splitter) {
        startSplitterDrag(state, e, splitter);
        return;
    }

    const floatHeader = e.target.closest('[data-dock-float-header]');
    if (floatHeader && !e.target.closest('[data-dock-float-btn]')) {
        startFloatDrag(state, e, floatHeader.closest('[data-dock-float]'));
        return;
    }

    const tab = e.target.closest('[data-dock-tab]');
    if (tab && !e.target.closest('.shiny-dock-tab__close')) {
        // defer: becomes a drag only after the threshold, so plain clicks still activate
        state.pointerDown = { kind: 'tab', el: tab, x: e.clientX, y: e.clientY };
    }
}

// ---------------------------------------------------------------- splitters
function startSplitterDrag(state, e, splitterEl) {
    const container = splitterEl.parentElement;          // .shiny-dock-split
    const first = splitterEl.previousElementSibling;     // .shiny-dock-split__child
    const second = splitterEl.nextElementSibling;
    if (!first || !second) return;

    const horizontal = container.classList.contains('shiny-dock-split--h');
    state.drag = {
        kind: 'splitter',
        splitId: splitterEl.getAttribute('data-dock-splitter'),
        container, first, second, horizontal,
        ratio: null,
    };
    document.body.style.cursor = horizontal ? 'col-resize' : 'row-resize';
    document.body.style.userSelect = 'none';
    e.preventDefault();
}

function moveSplitter(state, e) {
    const d = state.drag;
    const rect = d.container.getBoundingClientRect();
    let ratio = d.horizontal
        ? (e.clientX - rect.left) / rect.width
        : (e.clientY - rect.top) / rect.height;
    ratio = Math.min(0.92, Math.max(0.08, ratio));
    d.ratio = ratio;
    // live visual update without .NET round-trips; ratio committed on pointer-up
    d.first.style.flex = `${ratio} 1 0%`;
    d.second.style.flex = `${1 - ratio} 1 0%`;
}

// ---------------------------------------------------------------- floating drag
function startFloatDrag(state, e, floatEl) {
    if (!floatEl) return;
    const r = relativeRect(state.hostEl, floatEl);
    state.drag = {
        kind: 'float',
        el: floatEl,
        index: parseInt(floatEl.getAttribute('data-dock-float'), 10),
        offsetX: e.clientX - (r.x + state.hostEl.getBoundingClientRect().left),
        offsetY: e.clientY - (r.y + state.hostEl.getBoundingClientRect().top),
        x: r.x, y: r.y,
    };
    document.body.style.userSelect = 'none';
    e.preventDefault();
}

function moveFloat(state, e) {
    const d = state.drag;
    const h = state.hostEl.getBoundingClientRect();
    d.x = Math.max(0, Math.min(h.width - 60, e.clientX - h.left - d.offsetX));
    d.y = Math.max(0, Math.min(h.height - 30, e.clientY - h.top - d.offsetY));
    d.el.style.left = `${d.x}px`;
    d.el.style.top = `${d.y}px`;
}

// ---------------------------------------------------------------- tab drag
function beginTabDrag(state) {
    const p = state.pointerDown;
    const tabRect = p.el.getBoundingClientRect();

    const ghost = document.createElement('div');
    ghost.textContent = p.el.textContent.replace('×', '').trim();
    Object.assign(ghost.style, {
        position: 'fixed', zIndex: 9999, pointerEvents: 'none',
        left: '0px', top: '0px',
        padding: '5px 12px', fontSize: '12px', fontWeight: '600',
        background: '#312e81', color: '#fff', borderRadius: '5px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.35)', opacity: '0.92',
        transform: `translate(${tabRect.left}px, ${tabRect.top}px)`,
    });
    document.body.appendChild(ghost);

    const overlay = document.createElement('div');
    Object.assign(overlay.style, {
        position: 'fixed', zIndex: 9998, pointerEvents: 'none', display: 'none',
        background: 'rgba(59, 130, 246, 0.28)', border: '2px solid rgba(59, 130, 246, 0.9)',
        borderRadius: '6px', transition: 'all 60ms linear',
    });
    document.body.appendChild(overlay);

    state.drag = {
        kind: 'tab',
        srcInstanceId: p.el.getAttribute('data-dock-tab'),
        srcGroupId: p.el.closest('[data-dock-group]')?.getAttribute('data-dock-group'),
        ghost, overlay,
        target: null,
    };
    state.pointerDown = null;
    document.body.style.userSelect = 'none';
    state.dotnet.invokeMethodAsync('OnDragStartedJs', state.drag.srcInstanceId);
}

function moveTabDrag(state, e) {
    const d = state.drag;
    d.ghost.style.transform = `translate(${e.clientX + 10}px, ${e.clientY + 8}px)`;

    d.target = hitTest(state, e);
    if (d.target && d.target.rect) {
        const r = d.target.rect;
        Object.assign(d.overlay.style, {
            display: 'block',
            left: `${r.left}px`, top: `${r.top}px`,
            width: `${r.width}px`, height: `${r.height}px`,
        });
    } else {
        d.overlay.style.display = 'none';
    }
}

function hitTest(state, e) {
    // tab strips first (reorder / merge at index)
    for (const strip of state.hostEl.querySelectorAll('[data-dock-tabstrip]')) {
        const r = strip.getBoundingClientRect();
        if (e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom) continue;
        const groupId = strip.getAttribute('data-dock-tabstrip');
        const tabs = [...strip.querySelectorAll('[data-dock-tab]')];
        let index = tabs.length;
        for (let i = 0; i < tabs.length; i++) {
            const tr = tabs[i].getBoundingClientRect();
            if (e.clientX < tr.left + tr.width / 2) { index = i; break; }
        }
        return { groupId, zone: 'TabStrip', index, rect: r };
    }

    // group bodies: edge bands → split zones, middle → merge
    for (const content of state.hostEl.querySelectorAll('[data-dock-group-content]')) {
        const r = content.getBoundingClientRect();
        if (e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom) continue;
        const groupId = content.getAttribute('data-dock-group-content');
        const fx = (e.clientX - r.left) / r.width;
        const fy = (e.clientY - r.top) / r.height;

        let zone = 'Center', zr = r;
        if (fx < EDGE_BAND) { zone = 'Left'; zr = sub(r, 0, 0, 0.5, 1); }
        else if (fx > 1 - EDGE_BAND) { zone = 'Right'; zr = sub(r, 0.5, 0, 0.5, 1); }
        else if (fy < EDGE_BAND) { zone = 'Top'; zr = sub(r, 0, 0, 1, 0.5); }
        else if (fy > 1 - EDGE_BAND) { zone = 'Bottom'; zr = sub(r, 0, 0.5, 1, 0.5); }
        return { groupId, zone, index: -1, rect: zr };
    }

    // inside host but no group → tear-off
    const hr = state.hostEl.getBoundingClientRect();
    if (e.clientX >= hr.left && e.clientX <= hr.right && e.clientY >= hr.top && e.clientY <= hr.bottom)
        return { groupId: null, zone: 'TearOff', index: -1, rect: null,
                 x: e.clientX - hr.left, y: e.clientY - hr.top };
    return null;
}

function sub(r, fx, fy, fw, fh) {
    return { left: r.left + r.width * fx, top: r.top + r.height * fy, width: r.width * fw, height: r.height * fh };
}

// ---------------------------------------------------------------- move / up / cancel
function onPointerMove(state, e) {
    if (state.pointerDown && !state.drag) {
        const p = state.pointerDown;
        if (Math.abs(e.clientX - p.x) + Math.abs(e.clientY - p.y) > DRAG_THRESHOLD)
            beginTabDrag(state);
    }
    if (!state.drag) return;
    switch (state.drag.kind) {
        case 'splitter': moveSplitter(state, e); break;
        case 'float': moveFloat(state, e); break;
        case 'tab': moveTabDrag(state, e); break;
    }
}

function onPointerUp(state) {
    state.pointerDown = null;
    const d = state.drag;
    if (!d) return;
    state.drag = null;
    document.body.style.cursor = '';
    document.body.style.userSelect = '';

    switch (d.kind) {
        case 'splitter':
            if (d.ratio != null)
                state.dotnet.invokeMethodAsync('OnSplitterRatioChangedJs', d.splitId, d.ratio);
            break;
        case 'float':
            state.dotnet.invokeMethodAsync('OnFloatingMovedJs', d.index, d.x, d.y);
            break;
        case 'tab': {
            d.ghost.remove();
            d.overlay.remove();
            const t = d.target;
            if (t)
                state.dotnet.invokeMethodAsync('OnTabDroppedJs',
                    d.srcInstanceId, t.groupId, t.zone, t.index, t.x ?? 0, t.y ?? 0);
            else
                state.dotnet.invokeMethodAsync('OnDragCancelledJs', d.srcInstanceId);
            break;
        }
    }
}

function cancelDrag(state) {
    const d = state.drag;
    if (!d) { state.pointerDown = null; return; }
    state.drag = null;
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
    if (d.kind === 'tab') {
        d.ghost.remove();
        d.overlay.remove();
        state.dotnet.invokeMethodAsync('OnDragCancelledJs', d.srcInstanceId);
    }
}
