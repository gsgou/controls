// Two jobs, both of which need real measured pixels and so cannot be done from .NET:
//
//  1. decide which groups on the showing tab do not fit, and report their ids back so the ribbon can
//     fold them down to buttons;
//  2. raise an open dropdown into the browser's top layer and place it against the button it hangs
//     off, so a ribbon inside a panel or a scroller never clips its own menus.
//
// The ribbon works without either: with no module the groups all stay open and the body scrolls, and
// with no popover support the panels stay where the stylesheet put them.
const states = new WeakMap();

const MARGIN = 8;
const GAP = 2;
const SUB_GAP = 2;

// What a collapsed group is assumed to cost before one has ever been measured. Only the first pass
// uses it; after that each group's real collapsed width is cached and used instead.
const FALLBACK_COLLAPSED_WIDTH = 84;


export function init(root, dotnet) {
    // Re-initialising an existing root (groups arrived later) must not stack observers.
    dispose(root);

    const state = { root, dotnet, widths: new Map(), collapsedWidths: new Map(), reported: null };
    states.set(root, state);

    const ro = new ResizeObserver(() => measure(state));
    ro.observe(root);
    state.ro = ro;

    measure(state);
}


// Called from .NET after every render: the observer only fires when the *bar* changes size, and a
// render that swapped which items are in a group changes the widths without changing that.
export function remeasure(root) {
    const state = states.get(root);
    if (state) measure(state);
}


export function dispose(root) {
    const state = states.get(root);
    if (!state) return;

    if (state.scrollHook) {
        window.removeEventListener('scroll', state.scrollHook, true);
        window.removeEventListener('resize', state.scrollHook);
    }

    if (state.escapeHook)
        document.removeEventListener('keydown', state.escapeHook, true);

    state.ro?.disconnect();
    states.delete(root);
}


// ---- overflow --------------------------------------------------------------------------------

function measure(state) {
    const { root } = state;
    const body = root.querySelector('.shiny-ribbon__body');

    // A collapsed ribbon has no body to measure, and measuring a display:none subtree returns zeros
    // that would read as "nothing fits" and fold every group away.
    if (!body || body.classList.contains('is-hidden') || body.clientWidth === 0)
        return;

    const nodes = [...body.querySelectorAll('[data-ribbon-group]')];
    if (nodes.length === 0) return;

    // Cache each group's width in whichever form it is currently in. A collapsed group has no open
    // width left to read - and an open one no collapsed width - so without the cache a group could
    // never be told to go back. Both are per group rather than one shared figure: one wide collapsed
    // button would otherwise inflate the estimate for every other group and fold away far more of
    // the bar than actually had to go.
    for (const node of nodes) {
        const id = node.dataset.ribbonGroup;
        const width = Math.ceil(node.getBoundingClientRect().width);
        const map = node.classList.contains('shiny-ribbon-group__collapsed')
            ? state.collapsedWidths
            : state.widths;

        map.set(id, width);
    }

    const available = body.clientWidth;
    let total = nodes.reduce((sum, n) => sum + (state.widths.get(n.dataset.ribbonGroup) ?? 0), 0);

    // Lowest priority first, rightmost breaking ties - the order a ribbon has always given up its
    // groups in.
    const order = nodes
        .map((node, index) => ({ node, index }))
        .filter(x => x.node.dataset.ribbonCollapsible === 'true')
        .sort((a, b) => {
            const pa = parseInt(a.node.dataset.ribbonPriority ?? '0', 10);
            const pb = parseInt(b.node.dataset.ribbonPriority ?? '0', 10);
            return pa !== pb ? pa - pb : b.index - a.index;
        });

    const collapsed = [];
    for (const { node } of order) {
        if (total <= available) break;

        const id = node.dataset.ribbonGroup;
        collapsed.push(id);

        const shrunk = state.collapsedWidths.get(id) ?? FALLBACK_COLLAPSED_WIDTH;
        total -= Math.max(0, (state.widths.get(id) ?? 0) - shrunk);
    }

    const key = collapsed.slice().sort().join('|');
    if (key === state.reported) return;

    state.reported = key;
    state.dotnet.invokeMethodAsync('OnOverflow', collapsed);
}


// ---- menu placement --------------------------------------------------------------------------

export function placeMenus(root) {
    const state = states.get(root);
    hook(state);
    listenForEscape(state);

    // Document order is outermost first, which is also the order the top layer should stack them in.
    for (const menu of root.querySelectorAll('.shiny-ribbon-menu')) {
        const floating = raise(menu);
        menu.classList.toggle('is-floating', floating);
        if (!floating) continue;

        const submenu = menu.classList.contains('shiny-ribbon-submenu');
        const anchor = submenu
            ? menu.previousElementSibling
            : root.querySelector(`[data-ribbon-anchor="${cssEscape(menu.dataset.ribbonMenu)}"]`)
              ?? root.querySelector(`[data-ribbon-group="${cssEscape(menu.dataset.ribbonMenu)}"]`);

        position(menu, anchor, submenu);
    }
}


/*
    Escape closes the open panel.

    The panels are `popover=manual`, which is deliberate - `auto` light-dismisses on any outside click
    and would fight the backdrop that the ribbon already uses to close itself - but manual also opts
    out of the browser's own Escape handling, so it has to be put back by hand or the only way out of
    a menu is the mouse.
*/
function listenForEscape(state) {
    if (!state || state.escapeHook) return;

    const fn = e => {
        if (e.key !== 'Escape') return;
        if (!state.root.isConnected || !state.root.querySelector('.shiny-ribbon-menu')) return;

        e.stopPropagation();
        state.dotnet.invokeMethodAsync('OnDismiss');
    };

    state.escapeHook = fn;
    document.addEventListener('keydown', fn, true);
}


function cssEscape(value) {
    if (!value) return '';
    return window.CSS?.escape ? window.CSS.escape(value) : value;
}


function raise(el) {
    if (typeof el.showPopover !== 'function')
        return false;

    try {
        if (!el.matches(':popover-open'))
            el.showPopover();
    }
    catch {
        // Already open, or not connected to the document yet.
    }
    return el.matches(':popover-open');
}


function position(menu, anchor, submenu) {
    if (!anchor) return;

    const a = anchor.getBoundingClientRect();
    const m = menu.getBoundingClientRect();
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    let left;
    let top;

    if (submenu) {
        // Beside its row, flipping to the other side when that side is where the room is.
        left = a.right + SUB_GAP;
        if (left + m.width > vw - MARGIN && a.left - m.width - SUB_GAP >= MARGIN)
            left = a.left - m.width - SUB_GAP;

        top = a.top - 4;
        if (top + m.height > vh - MARGIN)
            top = a.bottom + 4 - m.height;
    }
    else {
        left = a.left;
        if (left + m.width > vw - MARGIN && a.right - m.width >= MARGIN)
            left = a.right - m.width;

        top = a.bottom + GAP;

        // No room below? Take above, but only when it is genuinely better.
        if (top + m.height > vh - MARGIN && a.top - m.height - GAP >= MARGIN)
            top = a.top - m.height - GAP;
    }

    // Written inline rather than through a class so nothing in the stylesheet - or the UA's own
    // popover rules - can win the specificity fight over where the panel sits.
    menu.style.position = 'fixed';
    menu.style.margin = '0';
    menu.style.right = 'auto';
    menu.style.bottom = 'auto';
    menu.style.left = Math.round(clamp(left, MARGIN, vw - m.width - MARGIN)) + 'px';
    menu.style.top = Math.round(clamp(top, MARGIN, vh - m.height - MARGIN)) + 'px';
}


function clamp(value, min, max) {
    // A panel bigger than the viewport inverts the range; pin to the low edge rather than jumping.
    if (max < min) return min;
    return value < min ? min : (value > max ? max : value);
}


// A panel in the top layer is out of flow, so nothing moves it when the page (or the scroller the bar
// is in) scrolls. Re-place it instead of leaving it pointing at where the button used to be.
function hook(state) {
    if (!state || state.scrollHook) return;

    const fn = () => {
        if (state.root.isConnected)
            placeMenus(state.root);
    };

    state.scrollHook = fn;
    window.addEventListener('scroll', fn, true);
    window.addEventListener('resize', fn);
}
