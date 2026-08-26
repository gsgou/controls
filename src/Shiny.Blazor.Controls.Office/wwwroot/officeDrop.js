// File drag-and-drop for the document and slide editors.
//
// Blazor's own @ondrop gives a DragEventArgs with no access to DataTransfer.files, so the file bytes
// are unreachable from C# — InputFile solves that for a file picker, but there is no equivalent for a
// drop onto an arbitrary element. So the listeners live here and hand the bytes over already read.
//
// dragover must call preventDefault or the drop event never fires at all: the default action for a
// dragover is "reject this drop", and the browser applies it before anything else gets a say.

/// Files larger than this are rejected rather than marshalled.
///
/// Everything crossing the JS/.NET boundary here goes as base64 in a JSON message, so a very large
/// image costs about a third more than its own size in string allocations on both sides. 32MB is far
/// above any plausible document image and far below the size at which that becomes a problem.
const MAX_BYTES = 32 * 1024 * 1024;

export function attach(element, dotNet, method) {
    if (!element)
        return null;

    // Nested elements fire dragleave as the pointer crosses between them, so a simple
    // enter/leave pair flickers the highlight. Counting depth is what makes it stable.
    let depth = 0;

    const highlight = on => {
        element.classList.toggle('shiny-office-drop-target', on);
    };

    const hasFiles = e =>
        Array.from(e.dataTransfer?.types ?? []).includes('Files');

    const onDragEnter = e => {
        if (!hasFiles(e))
            return;

        e.preventDefault();
        depth++;
        highlight(true);
    };

    const onDragOver = e => {
        if (!hasFiles(e))
            return;

        // Both required: preventDefault to allow the drop at all, and dropEffect to make the cursor
        // say "copy" rather than "move", since nothing is being taken out of the source.
        e.preventDefault();
        if (e.dataTransfer)
            e.dataTransfer.dropEffect = 'copy';
    };

    const onDragLeave = () => {
        depth = Math.max(0, depth - 1);
        if (depth === 0)
            highlight(false);
    };

    const onDrop = async e => {
        if (!hasFiles(e))
            return;

        e.preventDefault();
        depth = 0;
        highlight(false);

        // Relative to the element, so the caller can drop the object where the pointer was rather
        // than at a fixed spot.
        const box = element.getBoundingClientRect();
        const x = e.clientX - box.left;
        const y = e.clientY - box.top;

        for (const file of Array.from(e.dataTransfer?.files ?? [])) {
            if (file.size > MAX_BYTES) {
                await dotNet.invokeMethodAsync(method, file.name, file.type ?? '', null, x, y);
                continue;
            }

            const buffer = await file.arrayBuffer();
            await dotNet.invokeMethodAsync(method, file.name, file.type ?? '', base64(buffer), x, y);
        }
    };

    element.addEventListener('dragenter', onDragEnter);
    element.addEventListener('dragover', onDragOver);
    element.addEventListener('dragleave', onDragLeave);
    element.addEventListener('drop', onDrop);

    return DotNet.createJSObjectReference({
        dispose: () => {
            element.removeEventListener('dragenter', onDragEnter);
            element.removeEventListener('dragover', onDragOver);
            element.removeEventListener('dragleave', onDragLeave);
            element.removeEventListener('drop', onDrop);
            highlight(false);
        }
    });
}

export function detach(handle) {
    handle?.dispose?.();
    DotNet.disposeJSObjectReference?.(handle);
}

/// Encodes an ArrayBuffer as base64.
///
/// Chunked rather than one spread into String.fromCharCode: the argument list of a single call is
/// bounded by the engine's stack, and a few hundred kilobytes of image is enough to overflow it.
function base64(buffer) {
    const bytes = new Uint8Array(buffer);
    const chunk = 0x8000;
    let binary = '';

    for (let i = 0; i < bytes.length; i += chunk)
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunk));

    return btoa(binary);
}
