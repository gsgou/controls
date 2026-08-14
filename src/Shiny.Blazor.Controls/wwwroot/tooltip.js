// Placement for Shiny tooltips and walkthrough callouts.
//
// The rules are the same ones TooltipPlacementSolver applies on MAUI - try the preferred side, flip to
// the opposite if it does not fit, clamp along the cross axis to stay on screen, then slide the tail so
// it still points at the target. It lives here rather than in C# because placement needs the bubble's
// rendered size, and measuring that from .NET would mean a round trip per candidate side; here it is
// one call that measures, decides, and applies.

const AUTO_ORDER = ['bottom', 'top', 'right', 'left'];
const OPPOSITE = { top: 'bottom', bottom: 'top', left: 'right', right: 'left' };

const observed = new Map();

function clamp(value, min, max) {
    // A bubble wider than the viewport inverts the range; pin to the low edge rather than throwing.
    if (max < min)
        return min;

    return value < min ? min : (value > max ? max : value);
}

function rectOf(target) {
    const el = resolve(target);
    if (!el)
        return null;

    let r = el.getBoundingClientRect();

    // The anchor wrapper is `display: contents` so it does not disturb the page's layout, and an
    // element with no box of its own reports all zeros. Measure what it actually wraps instead.
    if (r.width === 0 && r.height === 0 && el.firstElementChild)
        r = el.firstElementChild.getBoundingClientRect();

    // Still nothing: the target is display:none. Report it as "nothing to point at" so the caller
    // centres the bubble rather than aiming it at the top-left corner.
    if (r.width === 0 && r.height === 0)
        return null;

    return { x: r.left, y: r.top, width: r.width, height: r.height };
}

function resolve(target) {
    if (!target)
        return null;

    if (typeof target === 'string')
        return document.querySelector(target);

    return target;
}

function available(side, target, gap, margin) {
    switch (side) {
        case 'top': return target.y - gap - margin;
        case 'bottom': return window.innerHeight - (target.y + target.height) - gap - margin;
        case 'left': return target.x - gap - margin;
        case 'right': return window.innerWidth - (target.x + target.width) - gap - margin;
        default: return 0;
    }
}

function fits(side, target, size, gap, margin) {
    const needed = (side === 'top' || side === 'bottom') ? size.height : size.width;
    return available(side, target, gap, margin) >= needed;
}

function chooseSide(preferred, target, size, gap, margin) {
    if (preferred && preferred !== 'auto') {
        if (fits(preferred, target, size, gap, margin))
            return preferred;

        const opposite = OPPOSITE[preferred];
        return (opposite && fits(opposite, target, size, gap, margin)) ? opposite : preferred;
    }

    for (const side of AUTO_ORDER) {
        if (fits(side, target, size, gap, margin))
            return side;
    }

    // Nothing fits: take the roomiest, so the clamping below eats as little of the bubble as it can.
    let best = AUTO_ORDER[0];
    let bestSpace = -Infinity;
    for (const side of AUTO_ORDER) {
        const space = available(side, target, gap, margin);
        if (space > bestSpace) {
            bestSpace = space;
            best = side;
        }
    }
    return best;
}

/**
 * Measures the bubble, decides which side of the target it goes on, writes the position onto it, and
 * returns what it decided so the component can render the tail on the matching edge.
 */
export function place(bubble, target, preferred, gap, margin, tailInset) {
    if (!bubble)
        return null;

    // A walkthrough places its callout against the spotlight cut-out rather than against an element,
    // so a plain rect is accepted here too.
    const rect = (target && typeof target === 'object' && !target.nodeType && 'width' in target)
        ? target
        : rectOf(target);
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    // Measured with the side already applied by the caller's previous render, which is close enough:
    // the only thing that changes between sides is which axis carries the tail, and tailInset covers it.
    const size = { width: bubble.offsetWidth, height: bubble.offsetHeight };

    if (!rect || preferred === 'center') {
        const left = clamp((vw - size.width) / 2, margin, vw - size.width - margin);
        const top = clamp((vh - size.height) / 2, margin, vh - size.height - margin);
        apply(bubble, left, top);
        return { placement: 'center', tailOffset: 0, left, top };
    }

    const side = chooseSide(preferred, rect, size, gap, margin);

    let left;
    let top;
    switch (side) {
        case 'top':
            left = rect.x + (rect.width / 2) - (size.width / 2);
            top = rect.y - gap - size.height;
            break;
        case 'bottom':
            left = rect.x + (rect.width / 2) - (size.width / 2);
            top = rect.y + rect.height + gap;
            break;
        case 'left':
            left = rect.x - gap - size.width;
            top = rect.y + (rect.height / 2) - (size.height / 2);
            break;
        default:
            left = rect.x + rect.width + gap;
            top = rect.y + (rect.height / 2) - (size.height / 2);
            break;
    }

    left = clamp(left, margin, vw - size.width - margin);
    top = clamp(top, margin, vh - size.height - margin);

    // Keep the tail on the target's centre line now that the bubble has been clamped away from it,
    // pulled in from the corners so it always meets a straight edge.
    const horizontal = side === 'top' || side === 'bottom';
    const center = horizontal ? rect.x + (rect.width / 2) : rect.y + (rect.height / 2);
    const start = horizontal ? left : top;
    const length = horizontal ? size.width : size.height;
    const tailOffset = length <= tailInset * 2
        ? length / 2
        : clamp(center - start, tailInset, length - tailInset);

    apply(bubble, left, top);
    return { placement: side, tailOffset, left, top };
}

function apply(bubble, left, top) {
    bubble.style.left = `${Math.round(left)}px`;
    bubble.style.top = `${Math.round(top)}px`;
}

/**
 * Puts the bubble in the browser's top layer.
 *
 * Without this a tooltip is clipped by any `overflow: hidden` ancestor and loses every z-index
 * argument on the page - the two things that make in-tree popovers unusable. `position: fixed` alone
 * does not help, because an ancestor with a transform becomes the containing block. The popover API
 * escapes both properly; where it is missing the bubble is still fixed-positioned and merely subject
 * to the old rules.
 */
export function open(bubble) {
    if (!bubble)
        return false;

    if (typeof bubble.showPopover !== 'function')
        return false;

    try {
        bubble.showPopover();
        return true;
    }
    catch {
        // Already open, or not connected to the document yet.
        return false;
    }
}

export function close(bubble) {
    if (!bubble || typeof bubble.hidePopover !== 'function')
        return;

    try {
        bubble.hidePopover();
    }
    catch {
        /* already closed */
    }
}

/**
 * Re-places the bubble while it is open. A tooltip on a control inside a scroller is pointing at a
 * moving thing, and one left at the old coordinates is worse than none.
 */
export function observe(id, dotnet) {
    unobserve(id);

    const state = { dotnet, frame: 0 };
    state.handler = () => {
        cancelAnimationFrame(state.frame);
        state.frame = requestAnimationFrame(() => {
            // Ignore the callback that fires after Blazor has already disposed the reference.
            try {
                state.dotnet.invokeMethodAsync('OnViewportChangedJs');
            }
            catch {
                unobserve(id);
            }
        });
    };

    // Capture phase so scrolling in any nested container counts, not just the document.
    window.addEventListener('scroll', state.handler, true);
    window.addEventListener('resize', state.handler);
    observed.set(id, state);
}

export function unobserve(id) {
    const state = observed.get(id);
    if (!state)
        return;

    cancelAnimationFrame(state.frame);
    window.removeEventListener('scroll', state.handler, true);
    window.removeEventListener('resize', state.handler);
    observed.delete(id);
}

/** Brings a walkthrough target into view before it is highlighted. */
export function scrollIntoView(target) {
    const el = resolve(target);
    if (!el)
        return;

    el.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'nearest' });
}

/** The target's rect, for the walkthrough's spotlight. */
export function measure(target) {
    return rectOf(target);
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
        /* nothing to do - persistence is best-effort */
    }
}
