// Two things a motion icon cannot do from CSS alone: know that it has scrolled into view, and know
// that a cycle has finished. Everything else about the animation is pure CSS.
//
// The cycle callbacks are here rather than on @onanimationend in the component because Blazor has
// no built-in mapping for the CSS animation events. Writing @onanimationend compiles without
// complaint and then renders a literal attribute into the DOM: no handler, no error, and an icon
// that stays stuck in its playing state after a single tap.

const attached = new WeakMap();

let observer = null;

function ensureObserver() {
    if (observer) {
        return observer;
    }

    observer = new IntersectionObserver(entries => {
        for (const entry of entries) {
            if (!entry.isIntersecting) {
                continue;
            }

            const state = attached.get(entry.target);

            // One shot: an icon that replays every time it scrolls past is a distraction, and a
            // caller who wants that can bind IsPlaying instead.
            observer.unobserve(entry.target);

            state?.ref.invokeMethodAsync('OnAppear');
        }
    }, { threshold: 0.25 });

    return observer;
}

export function attach(element, dotNetRef, observeVisibility) {
    if (!element || !dotNetRef || attached.has(element)) {
        return;
    }

    const state = { ref: dotNetRef, pending: null };

    // An icon is several animated elements running in lockstep, so one cycle raises one event per
    // element. Coalescing them into a single call per frame keeps a page of icons from flooding the
    // interop channel — and the C# side is guarded anyway.
    const notify = method => {
        if (state.pending === method) {
            return;
        }

        state.pending = method;

        requestAnimationFrame(() => {
            state.pending = null;
            state.ref.invokeMethodAsync(method);
        });
    };

    state.onEnd = () => notify('OnAnimationEnded');
    state.onIteration = () => notify('OnAnimationIteration');

    // Both events bubble, so listening on the wrapper catches every part of the icon.
    element.addEventListener('animationend', state.onEnd);
    element.addEventListener('animationiteration', state.onIteration);

    attached.set(element, state);

    if (observeVisibility) {
        ensureObserver().observe(element);
    }
}

export function detach(element) {
    const state = element && attached.get(element);

    if (!state) {
        return;
    }

    element.removeEventListener('animationend', state.onEnd);
    element.removeEventListener('animationiteration', state.onIteration);

    observer?.unobserve(element);
    attached.delete(element);
}
