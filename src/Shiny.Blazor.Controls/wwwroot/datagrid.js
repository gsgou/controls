// Sticky geometry for DataGrid: `position: sticky` needs a concrete left/right per pinned cell and
// a concrete top for the filter row under a fixed header, and only the browser knows how wide the
// preceding columns ended up. So we measure one reference row and write the offsets onto every cell.
// C# writes best-effort offsets first (see RefreshFrozenLayout); this corrects them.
const states = new WeakMap();

export function init(root) {
    if (!root) return;

    let state = states.get(root);
    if (state) {
        // init doubles as "re-measure": the grid calls it after every render.
        apply(root);
        return;
    }

    state = { raf: 0 };
    state.schedule = () => {
        if (state.raf) return;
        state.raf = requestAnimationFrame(() => {
            state.raf = 0;
            apply(root);
        });
    };

    if (typeof ResizeObserver === 'function') {
        state.observer = new ResizeObserver(state.schedule);
        state.observer.observe(root);
    }

    states.set(root, state);
    apply(root);
}

function apply(root) {
    const table = root.querySelector('.shiny-dg-table');
    if (!table) return;

    // The filter row sticks below the header row, so it needs the header's measured height.
    const headRow = table.querySelector('thead tr');
    if (headRow) {
        root.style.setProperty('--shiny-dg-head-h', headRow.getBoundingClientRect().height + 'px');
    }

    const rows = table.querySelectorAll('tr');

    // Reference row = the first full-width row (group headers and the no-records row use colspan
    // and would give a bogus column count).
    let widths = null;
    for (const row of rows) {
        if (row.querySelector('[colspan]')) continue;
        if (!row.querySelector('[data-dg-frozen]')) continue;
        widths = [];
        for (const cell of row.children) widths.push(cell.getBoundingClientRect().width);
        break;
    }
    if (!widths) return;

    const left = [];
    let acc = 0;
    for (let i = 0; i < widths.length; i++) {
        left.push(acc);
        acc += widths[i];
    }

    const right = new Array(widths.length);
    acc = 0;
    for (let i = widths.length - 1; i >= 0; i--) {
        right[i] = acc;
        acc += widths[i];
    }

    for (const row of rows) {
        const cells = row.children;
        if (cells.length !== widths.length) continue;
        for (let i = 0; i < cells.length; i++) {
            const side = cells[i].dataset.dgFrozen;
            if (side === 'start') {
                cells[i].style.left = left[i] + 'px';
                cells[i].style.right = '';
            }
            else if (side === 'end') {
                cells[i].style.right = right[i] + 'px';
                cells[i].style.left = '';
            }
        }
    }
}

export function dispose(root) {
    const state = states.get(root);
    if (!state) return;

    if (state.raf) cancelAnimationFrame(state.raf);
    state.observer?.disconnect();
    states.delete(root);
}
