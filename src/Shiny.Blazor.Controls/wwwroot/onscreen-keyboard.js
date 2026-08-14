// The on-screen keyboard's browser half.
//
// Two things make or break a touch OSK, and both live here.
//
// 1. The keys must never take focus. The host cancels pointerdown, so the caret stays exactly where
//    it was and every command below can simply act on document.activeElement. Without that, the
//    field loses its caret the instant a key is tapped - the single biggest cause of "the OSK does
//    nothing" reports.
// 2. Typing has to land at the caret AND replace the selection. execCommand('insertText') is
//    deprecated but still the only call that does both while leaving the undo stack intact, so it
//    is the primary path with a manual value splice behind it for the browsers that decline.
//
// Everything crossing the interop boundary here is a string, a bool or a number. No object DTOs -
// they get trimmed out from under you in a published WASM build.

const state = {
    dotnet: null,
    focusIn: null,
    focusOut: null,
    blurTimer: 0,
    target: null,
    bodyPadding: null
};

// The <input> types that take free text. Left deliberately narrow: date, colour and range pickers
// have their own UI and typing into them makes no sense.
const TEXT_TYPES = new Set(['', 'text', 'search', 'url', 'tel', 'email', 'password', 'number']);

const FOCUSABLE =
    'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),' +
    'textarea:not([disabled]),[tabindex]:not([tabindex="-1"])';

function isEditable(el) {
    if (!el || el.disabled || el.readOnly)
        return false;

    // Our own keys are buttons; they must never register as a typing target.
    if (el.closest && el.closest('[data-shiny-osk]'))
        return false;

    if (el.tagName === 'TEXTAREA')
        return true;

    if (el.tagName === 'INPUT')
        return TEXT_TYPES.has((el.getAttribute('type') || '').toLowerCase());

    return el.isContentEditable === true;
}

/** The field the keys type into: whatever holds focus, else the last field that did. */
function target() {
    const active = document.activeElement;
    if (isEditable(active))
        return active;

    if (state.target && state.target.isConnected && isEditable(state.target))
        return state.target;

    return null;
}

function claim() {
    const el = target();
    if (!el)
        return null;

    // Belt and braces: the pointerdown cancel should have kept focus here already.
    if (document.activeElement !== el)
        el.focus({ preventScroll: true });

    return el;
}

// ---- focus tracking -----------------------------------------------------------------------

export function observe(dotnet) {
    unobserve();
    state.dotnet = dotnet;

    state.focusIn = (e) => {
        if (!isEditable(e.target))
            return;

        state.target = e.target;
        window.clearTimeout(state.blurTimer);
        notify(true);
    };

    state.focusOut = (e) => {
        if (!isEditable(e.target))
            return;

        // Tapping a key never moves focus, so a blur really does mean the user left the field. Give
        // the next focusin a moment to arrive first, or tabbing between two fields flickers the
        // keyboard shut and open again.
        window.clearTimeout(state.blurTimer);
        state.blurTimer = window.setTimeout(() => {
            state.target = null;
            notify(false);
        }, 120);
    };

    document.addEventListener('focusin', state.focusIn, true);
    document.addEventListener('focusout', state.focusOut, true);

    // The field may already be focused when the host renders.
    if (isEditable(document.activeElement)) {
        state.target = document.activeElement;
        notify(true);
    }
}

function notify(focused) {
    if (!state.dotnet)
        return;

    try {
        state.dotnet.invokeMethodAsync('OnFocusChangedJs', focused);
    }
    catch {
        // The component went away mid-flight; stop before we spam a dead reference.
        unobserve();
    }
}

export function unobserve() {
    if (state.focusIn)
        document.removeEventListener('focusin', state.focusIn, true);

    if (state.focusOut)
        document.removeEventListener('focusout', state.focusOut, true);

    window.clearTimeout(state.blurTimer);
    state.focusIn = null;
    state.focusOut = null;
    state.dotnet = null;
    state.target = null;
    setInset(0);
}

// ---- typing -------------------------------------------------------------------------------

/**
 * Splice text in at the caret, replacing the selection, and tell Blazor about it.
 *
 * `deleteBefore` removes that many characters behind a collapsed caret first - that is backspace.
 */
function splice(el, text, deleteBefore) {
    const value = el.value ?? '';
    let start = el.selectionStart ?? value.length;
    const end = el.selectionEnd ?? start;

    if (start === end && deleteBefore > 0)
        start = Math.max(0, start - deleteBefore);

    el.value = value.slice(0, start) + text + value.slice(end);

    const caret = start + text.length;
    try { el.setSelectionRange(caret, caret); } catch { /* number inputs refuse this */ }

    // A programmatic value assignment does not set the element's dirty flag, so the browser will
    // never fire change on blur by itself. Raise both: input feeds @bind:event="oninput", change
    // feeds a plain @bind.
    el.dispatchEvent(new Event('input', { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
}

export function insert(text) {
    const el = claim();
    if (!el)
        return false;

    // The browser handles selection-replace, undo and the input event for us when this works.
    let handled = false;
    try { handled = document.execCommand('insertText', false, text); } catch { handled = false; }
    if (handled)
        return true;

    if (el.isContentEditable)
        return false;

    return splice(el, text, 0);
}

export function backspace() {
    const el = claim();
    if (!el)
        return false;

    // execCommand('delete') is reliable in contenteditable and patchy in form fields, so the
    // deterministic splice is the primary path for input/textarea rather than the fallback.
    if (el.isContentEditable) {
        try { return document.execCommand('delete'); } catch { return false; }
    }

    return splice(el, '', 1);
}

export function enter(insertNewLine) {
    const el = claim();
    if (!el)
        return false;

    const multiline = el.tagName === 'TEXTAREA' || el.isContentEditable;
    if (insertNewLine && multiline)
        return insert('\n');

    // On a single-line field Enter means "accept", not "type a newline". Dispatch the real key
    // events so the page's own handler sees them, then submit only if nobody cancelled.
    const live = el.dispatchEvent(new KeyboardEvent('keydown', {
        key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true, cancelable: true
    }));
    el.dispatchEvent(new KeyboardEvent('keyup', {
        key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true, cancelable: true
    }));

    if (live && !multiline && el.form && el.form.requestSubmit)
        el.form.requestSubmit();

    return true;
}

/** Tab moves focus for real - it is the one key whose whole job is to leave the current field. */
export function tab(backwards) {
    const el = target();
    const all = Array.from(document.querySelectorAll(FOCUSABLE))
        .filter(x => !x.closest('[data-shiny-osk]') && x.offsetParent !== null);

    if (all.length === 0)
        return false;

    const index = el ? all.indexOf(el) : -1;
    const next = index < 0
        ? all[0]
        : all[(index + (backwards ? -1 : 1) + all.length) % all.length];

    next.focus({ preventScroll: true });
    return true;
}

export function move(direction) {
    const el = claim();
    if (!el)
        return false;

    if (el.isContentEditable) {
        // Offset maths across text nodes is a rabbit hole; the selection API already knows how to
        // do this in every engine we support.
        const selection = window.getSelection();
        if (!selection || !selection.modify)
            return false;

        const granularity = (direction === 'up' || direction === 'down') ? 'line' : 'character';
        const side = (direction === 'up' || direction === 'left') ? 'backward' : 'forward';
        selection.modify('move', side, granularity);
        return true;
    }

    const value = el.value ?? '';
    const collapsed = el.selectionStart === el.selectionEnd;
    let pos = el.selectionStart ?? 0;

    if (direction === 'left') {
        pos = collapsed ? Math.max(0, pos - 1) : (el.selectionStart ?? 0);
    }
    else if (direction === 'right') {
        pos = collapsed ? Math.min(value.length, pos + 1) : (el.selectionEnd ?? 0);
    }
    else {
        const lineStart = value.lastIndexOf('\n', pos - 1) + 1;
        const column = pos - lineStart;

        if (direction === 'up') {
            if (lineStart === 0) {
                pos = 0;
            }
            else {
                const prevStart = value.lastIndexOf('\n', lineStart - 2) + 1;
                pos = Math.min(prevStart + column, lineStart - 1);
            }
        }
        else {
            const lineEnd = value.indexOf('\n', pos);
            if (lineEnd < 0) {
                pos = value.length;
            }
            else {
                const nextStart = lineEnd + 1;
                let nextEnd = value.indexOf('\n', nextStart);
                if (nextEnd < 0)
                    nextEnd = value.length;

                pos = Math.min(nextStart + column, nextEnd);
            }
        }
    }

    try { el.setSelectionRange(pos, pos); } catch { return false; }
    return true;
}

export function dismiss() {
    const el = target();
    if (el)
        el.blur();

    state.target = null;
    return true;
}

// ---- layout -------------------------------------------------------------------------------

/** PushContent: keep the tail of the page reachable by padding the body out from under the keys. */
export function setInset(px) {
    document.documentElement.style.setProperty('--shiny-osk-inset', px + 'px');

    if (px > 0) {
        if (state.bodyPadding === null)
            state.bodyPadding = document.body.style.paddingBottom;

        document.body.style.paddingBottom = px + 'px';
    }
    else if (state.bodyPadding !== null) {
        document.body.style.paddingBottom = state.bodyPadding;
        state.bodyPadding = null;
    }
}

/**
 * Scroll the focused field out from behind the keyboard.
 *
 * scrollIntoView is no use here - it has no idea part of the viewport is occluded - so measure the
 * overlap and scroll exactly that far in whichever container actually scrolls.
 */
export function reveal(keyboardHeight) {
    const el = target();
    if (!el)
        return;

    const limit = window.innerHeight - keyboardHeight - 12;
    const delta = el.getBoundingClientRect().bottom - limit;
    if (delta <= 0)
        return;

    let node = el.parentElement;
    while (node) {
        const style = window.getComputedStyle(node);
        if (/(auto|scroll)/.test(style.overflowY) && node.scrollHeight > node.clientHeight) {
            node.scrollBy({ top: delta, behavior: 'smooth' });
            return;
        }
        node = node.parentElement;
    }

    window.scrollBy({ top: delta, behavior: 'smooth' });
}
