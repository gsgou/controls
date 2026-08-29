// Resolves the light/dark scheme in force at a given element.
//
// Controls that paint their own pixels (a Skia canvas, say) cannot inherit a CSS colour the way a
// div does, so they have to be told which scheme they are sitting in. Reading `color-scheme` off
// the element - rather than matchMedia - is what makes this work for a page that flips the theme
// with a class on a container rather than on <html>: color-scheme inherits, and the generated
// theme sets it on the same scope that carries the colour tokens.
export function resolveScheme(el) {
    const target = el || document.documentElement;
    const scheme = getComputedStyle(target).colorScheme || '';

    if (scheme.includes('dark') && !scheme.includes('light'))
        return 'dark';

    if (scheme.includes('light') && !scheme.includes('dark'))
        return 'light';

    // 'normal', 'light dark', or a UA that does not report it: fall back to the OS preference.
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

// Watches are held here rather than handed back as a JS object: a plain object does not marshal to
// an IJSObjectReference, and the failure surfaces from deep inside Blazor rather than here.
const watches = new Map();

// Calls back whenever the scheme at `el` changes - a theme class toggled anywhere above it, or the
// OS preference flipping. `token` is the caller's handle for unwatchScheme.
export function watchScheme(token, el, dotNetRef, method) {
    unwatchScheme(token);

    const target = el || document.documentElement;
    let last = resolveScheme(target);

    const check = () => {
        const next = resolveScheme(target);
        if (next === last)
            return;

        last = next;
        dotNetRef.invokeMethodAsync(method, next);
    };

    // A class swap on an ancestor changes the computed value without mutating this element, and
    // there is no event for that - so watch every ancestor's attributes plus the media query.
    const observer = new MutationObserver(check);
    for (let node = target; node; node = node.parentElement)
        observer.observe(node, { attributes: true, attributeFilter: ['class', 'style'] });

    const media = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;
    media?.addEventListener('change', check);

    watches.set(token, () => {
        observer.disconnect();
        media?.removeEventListener('change', check);
    });

    return last;
}

export function unwatchScheme(token) {
    const stop = watches.get(token);
    if (!stop)
        return;

    stop();
    watches.delete(token);
}


// The app's neutral colour tokens, resolved to concrete rgb() at `el`.
//
// Painted surfaces (a Skia grid, a document page) cannot consume a CSS custom property the way a div
// can, so the values have to be handed over. Reading them off a probe rather than with
// getPropertyValue is what makes them *resolved*: getPropertyValue returns whatever text was
// declared, which for a token defined in terms of another token is another var() expression. Setting
// the property on a real element and reading it back makes the browser do the resolution.
//
// The probe is parented inside `el` on purpose - the tokens are scoped to whatever container carries
// the theme class, so a probe on document.body would read the wrong theme for a control inside a
// panel that sets its own.
const SURFACE_TOKENS = [
    ['surface', '--shiny-color-surface', '#FFFFFF'],
    ['onSurface', '--shiny-color-on-surface', '#1B1B1F'],
    ['surfaceContainer', '--shiny-color-surface-container', '#EFEFF2'],
    ['surfaceContainerLow', '--shiny-color-surface-container-low', '#F7F7F8'],
    ['onSurfaceVariant', '--shiny-color-on-surface-variant', '#45464F'],
    ['outline', '--shiny-color-outline', '#767680'],
    ['outlineVariant', '--shiny-color-outline-variant', '#E5E7EB']
];

export function readSurface(el) {
    const host = el || document.documentElement;

    const probe = document.createElement('span');
    probe.style.cssText = 'position:absolute;width:0;height:0;visibility:hidden;pointer-events:none';
    host.appendChild(probe);

    try {
        const out = {};
        for (const [name, token, fallback] of SURFACE_TOKENS) {
            probe.style.color = `var(${token}, ${fallback})`;
            out[name] = getComputedStyle(probe).color;
        }

        return out;
    }
    finally {
        probe.remove();
    }
}
