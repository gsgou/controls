// Measures the intrinsic width of every toolbar item (via an off-screen mirror row)
// and reports back to .NET how many fit before the overflow ("hamburger") button is needed.
const states = new WeakMap();

export function init(root, dotnet) {
    const inner = root.querySelector('.shiny-toolbar-inner');
    if (!inner) return;

    const state = { root, inner, dotnet, lastCount: -1 };
    states.set(root, state);

    const ro = new ResizeObserver(() => measure(state));
    ro.observe(inner);
    state.ro = ro;

    measure(state);
}

// Called from .NET after each render so item/label changes are picked up
// (ResizeObserver only fires on size changes of the bar itself).
export function remeasure(root) {
    const state = states.get(root);
    if (state) measure(state);
}

export function dispose(root) {
    const state = states.get(root);
    if (!state) return;
    state.ro?.disconnect();
    states.delete(root);
}

function measure(state) {
    const { inner, root, dotnet } = state;

    const measureRow = root.querySelector('.shiny-toolbar-measure');
    if (!measureRow) return;

    const items = Array.from(measureRow.querySelectorAll('[data-measure-item]'));
    const total = items.length;
    if (total === 0) return;

    const toggleEl = measureRow.querySelector('[data-measure-toggle]');

    // width already consumed by everything in the bar that ISN'T the items region
    // (title / start / child content). Absolutely-positioned children (the measure
    // row) are out of flex flow and naturally excluded.
    const innerStyle = getComputedStyle(inner);
    const padL = parseFloat(innerStyle.paddingLeft) || 0;
    const padR = parseFloat(innerStyle.paddingRight) || 0;
    const innerGap = parseFloat(innerStyle.gap) || 0;

    let otherWidth = 0;
    let siblingCount = 0;
    for (const child of inner.children) {
        if (child.classList.contains('shiny-toolbar-items')) continue;
        if (child.classList.contains('shiny-toolbar-measure')) continue;
        otherWidth += child.getBoundingClientRect().width;
        siblingCount++;
    }

    const available = inner.clientWidth - padL - padR - otherWidth - innerGap * siblingCount;

    const itemsRow = root.querySelector('.shiny-toolbar-items');
    const itemGap = itemsRow ? (parseFloat(getComputedStyle(itemsRow).gap) || 0) : 0;

    const widths = items.map(el => el.getBoundingClientRect().width);
    const toggleWidth = toggleEl ? toggleEl.getBoundingClientRect().width : 0;

    // first pass: do they all fit with no overflow button?
    let count = fit(widths, available, itemGap, 0);
    if (count < total) {
        // they don't — reserve room for the overflow button and refit
        count = fit(widths, available, itemGap, toggleWidth + itemGap);
    }
    if (count < 0) count = 0;

    if (count !== state.lastCount) {
        state.lastCount = count;
        dotnet.invokeMethodAsync('SetVisibleCount', count);
    }
}

function fit(widths, available, gap, reserve) {
    let used = 0;
    let count = 0;
    for (let i = 0; i < widths.length; i++) {
        const w = widths[i] + (count > 0 ? gap : 0);
        if (used + w + reserve <= available) {
            used += w;
            count++;
        } else {
            break;
        }
    }
    return count;
}
