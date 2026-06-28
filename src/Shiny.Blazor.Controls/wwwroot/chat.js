const states = new WeakMap();

export function init(messagesEl, dotnetRef) {
    const state = {
        messagesEl,
        dotnet: dotnetRef,
        autoScroll: true,
        topLatched: false
    };
    states.set(messagesEl, state);

    messagesEl.addEventListener('scroll', () => {
        const { scrollTop, scrollHeight, clientHeight } = messagesEl;
        state.autoScroll = scrollHeight - scrollTop - clientHeight < 50;

        // Fire a single load-older callback while near the top; re-arm once scrolled away.
        if (scrollTop < 60) {
            if (!state.topLatched) {
                state.topLatched = true;
                if (state.dotnet)
                    state.dotnet.invokeMethodAsync('OnScrolledToTop');
            }
        }
        else {
            state.topLatched = false;
        }
    });
}

export function scrollToEnd(messagesEl, animate) {
    const state = states.get(messagesEl);
    if (!state) return;

    messagesEl.scrollTo({
        top: messagesEl.scrollHeight,
        behavior: animate ? 'smooth' : 'instant'
    });
    state.autoScroll = true;
}

export function scrollToMessage(messagesEl, messageIndex) {
    const state = states.get(messagesEl);
    if (!state) return;

    const wraps = messagesEl.querySelectorAll('.shiny-chat-bubble-wrap');
    if (wraps[messageIndex]) {
        wraps[messageIndex].scrollIntoView({ behavior: 'smooth', block: 'start' });
        state.autoScroll = false;
    }
}

export function maintainScrollPosition(messagesEl, previousScrollHeight) {
    const state = states.get(messagesEl);
    if (!state) return;

    const newScrollHeight = messagesEl.scrollHeight;
    messagesEl.scrollTop = newScrollHeight - previousScrollHeight;
}

export function getScrollHeight(messagesEl) {
    return messagesEl ? messagesEl.scrollHeight : 0;
}

export function isNearBottom(messagesEl) {
    if (!messagesEl) return true;
    const { scrollTop, scrollHeight, clientHeight } = messagesEl;
    return scrollHeight - scrollTop - clientHeight < 50;
}

// Wrap the current textarea selection with markdown delimiters (or insert a placeholder
// when there is no selection). Returns the new full text so .NET can update its bound field.
export function wrapSelection(textareaEl, before, after, placeholder) {
    if (!textareaEl) return '';
    const value = textareaEl.value ?? '';
    const start = textareaEl.selectionStart ?? value.length;
    const end = textareaEl.selectionEnd ?? value.length;
    const selected = value.substring(start, end) || placeholder || '';
    const next = value.substring(0, start) + before + selected + after + value.substring(end);

    textareaEl.value = next;
    const caret = start + before.length;
    textareaEl.focus();
    textareaEl.setSelectionRange(caret, caret + selected.length);
    return next;
}

// Prompt for a URL (and optional text) and insert a markdown link. Returns the new full text.
export function insertLink(textareaEl) {
    if (!textareaEl) return '';
    const value = textareaEl.value ?? '';
    const start = textareaEl.selectionStart ?? value.length;
    const end = textareaEl.selectionEnd ?? value.length;
    const selected = value.substring(start, end);

    const url = window.prompt('Link URL', 'https://');
    if (!url) return value;
    const text = selected || window.prompt('Link text', '') || url;

    const link = '[' + text + '](' + url + ')';
    const next = value.substring(0, start) + link + value.substring(end);
    textareaEl.value = next;
    const caret = start + link.length;
    textareaEl.focus();
    textareaEl.setSelectionRange(caret, caret);
    return next;
}

export function copyText(text) {
    if (navigator.clipboard && navigator.clipboard.writeText)
        return navigator.clipboard.writeText(text ?? '');
    return Promise.resolve();
}

export function dispose(messagesEl) {
    states.delete(messagesEl);
}
