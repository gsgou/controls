// Walkthrough's browser side. Placement, measuring and storage are the tooltip module's - a callout
// is a tooltip bubble that happens to point at a spotlight instead of a control - and re-exported here
// so the component only imports one module.

export { place, measure, scrollIntoView, observe, unobserve, load, save } from './tooltip.js';

const keyListeners = new Map();
const clickListeners = new Map();

/**
 * Keyboard for the tour: arrows and Enter move, Escape leaves.
 *
 * A guided tour that can only be driven by pointer is unusable with a keyboard or a switch, and the
 * backdrop swallows the page's own focus handling while it is up - so these keys are the only way out
 * for anyone not using a mouse.
 */
export function observeKeys(id, dotnet) {
    unobserveKeys(id);

    const handler = (e) => {
        let action = null;
        switch (e.key) {
            case 'Escape': action = 'skip'; break;
            case 'ArrowRight':
            case 'Enter':
            case ' ': action = 'next'; break;
            case 'ArrowLeft': action = 'back'; break;
            default: return;
        }

        e.preventDefault();
        e.stopPropagation();

        try {
            dotnet.invokeMethodAsync('OnKeyJs', action);
        }
        catch {
            unobserveKeys(id);
        }
    };

    // Capture, so the page's own handlers do not see the key first and act on it while the tour is up.
    window.addEventListener('keydown', handler, true);
    keyListeners.set(id, handler);
}

export function unobserveKeys(id) {
    const handler = keyListeners.get(id);
    if (!handler)
        return;

    window.removeEventListener('keydown', handler, true);
    keyListeners.delete(id);
}

/**
 * "Use the control to continue" - advance when the highlighted element is actually clicked.
 *
 * Listens on the element itself rather than delegating from the document, because the click has to
 * count even when the element stops the event from bubbling, which buttons inside forms routinely do.
 */
export function watchTargetClick(id, selector, dotnet) {
    unwatchTargetClick(id);

    const el = document.querySelector(selector);
    if (!el)
        return false;

    const handler = () => {
        try {
            dotnet.invokeMethodAsync('OnTargetClickedJs');
        }
        catch {
            unwatchTargetClick(id);
        }
    };

    el.addEventListener('click', handler);
    clickListeners.set(id, { el, handler });
    return true;
}

export function unwatchTargetClick(id) {
    const entry = clickListeners.get(id);
    if (!entry)
        return;

    entry.el.removeEventListener('click', entry.handler);
    clickListeners.delete(id);
}

/** The viewport, so the walkthrough can size its tap shields around the cut-out. */
export function viewport() {
    return { x: 0, y: 0, width: window.innerWidth, height: window.innerHeight };
}

/**
 * Stops the page scrolling underneath the tour.
 *
 * Returns what it replaced so it can be put back exactly - blanking the style instead would wipe an
 * app that sets its own overflow on the body.
 */
export function lockScroll(locked) {
    const body = document.body;
    if (locked) {
        const previous = body.style.overflow;
        body.dataset.shinyWtOverflow = previous;
        body.style.overflow = 'hidden';
        return previous;
    }

    body.style.overflow = body.dataset.shinyWtOverflow ?? '';
    delete body.dataset.shinyWtOverflow;
    return null;
}
