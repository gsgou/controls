// Native text-input plumbing for the document editor.
//
// Blazor has no built-in `beforeinput` event, and registering a custom event type would need the
// consuming app to edit its index.html — so the listener is attached here instead and the library
// stays self-contained.
//
// `beforeinput` rather than key events on purpose: it is the only event that reports IME composition
// results, autocorrect, dictation and paste as ordinary text insertions. Handling keys works for a US
// keyboard and silently drops every other input method.
export function attach(element, dotNet) {
    if (!element)
        return null;

    const onBeforeInput = e => {
        // The contenteditable is only a keyboard target; the document model owns the text, so the
        // browser must never actually mutate this element.
        e.preventDefault();
        dotNet.invokeMethodAsync('HandleBeforeInput', e.inputType ?? '', e.data ?? null);
    };

    // Composition text arrives through beforeinput as insertCompositionText, but the element also has
    // to be cleared afterwards or the IME keeps appending to stale content.
    const onCompositionEnd = () => { element.textContent = ''; };

    element.addEventListener('beforeinput', onBeforeInput);
    element.addEventListener('compositionend', onCompositionEnd);

    // Wrapped for .NET: a plain object cannot be marshalled back as an IJSObjectReference, and the
    // resulting deserialisation failure surfaces as "the listener silently never attached".
    return DotNet.createJSObjectReference({
        dispose: () => {
            element.removeEventListener('beforeinput', onBeforeInput);
            element.removeEventListener('compositionend', onCompositionEnd);
        }
    });
}

/// Focuses the editor's hidden input. Done here rather than through ElementReference.FocusAsync so a
/// failure is visible in the console instead of vanishing into a catch.
export function focus(element) {
    element?.focus({ preventScroll: true });
}

export function detach(handle) {
    handle?.dispose?.();
    DotNet.disposeJSObjectReference?.(handle);
}
