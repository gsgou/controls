// Window-level file drop.
//
// Everything here is attached to `window` with capture: true. That is the browser equivalent of the
// native side's "over top of any web view" - a capturing listener on the window runs before any
// element's own handler, so a drop lands here whatever it was dropped on, and no component in the
// page can quietly swallow it first.
//
// preventDefault on dragover AND drop is not optional. Without both, the browser's default action
// for a dropped file is to navigate to it, which unloads the app.

const scopes = new Map();

/**
 * Starts watching. `id` is the caller's scope id; call detach with the same one.
 */
export function attach(id, dotnet) {
    detach(id);

    const scope = {
        dotnet,
        // Files are kept here rather than sent to .NET, because a File cannot cross the interop
        // boundary. .NET gets metadata plus this key, and asks for the bytes later.
        files: new Map(),
        // dragenter/dragleave fire for every element the pointer crosses, not just the window, so a
        // naive leave handler flickers the overlay off and on constantly. Counting depth is the
        // standard fix - leave only counts when it returns to zero.
        depth: 0,
        nextKey: 0
    };

    const carriesFiles = (e) =>
        !!e.dataTransfer && Array.from(e.dataTransfer.types || []).includes('Files');

    const describe = (e) => {
        // During a drag the browser deliberately hides file names: dataTransfer.items has entries of
        // kind 'file' but getAsFile() returns null until drop. So hovering can report how many files
        // there are and their MIME types, and nothing else.
        const items = e.dataTransfer ? Array.from(e.dataTransfer.items || []) : [];
        return items
            .filter(x => x.kind === 'file')
            .map(x => ({ key: '', name: '', size: -1, contentType: x.type || '', lastModified: 0 }));
    };

    scope.onDragEnter = (e) => {
        if (!carriesFiles(e)) return;
        e.preventDefault();

        scope.depth++;
        if (scope.depth === 1)
            scope.dotnet.invokeMethodAsync('OnDragEnter', { files: describe(e), x: e.clientX, y: e.clientY });
    };

    scope.onDragOver = (e) => {
        if (!carriesFiles(e)) return;
        // Both of these matter: preventDefault stops the navigation, and dropEffect is what changes
        // the cursor from "no entry" to a copy badge.
        e.preventDefault();
        if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy';

        scope.dotnet.invokeMethodAsync('OnDragOver', { files: describe(e), x: e.clientX, y: e.clientY });
    };

    scope.onDragLeave = (e) => {
        if (!carriesFiles(e)) return;

        scope.depth = Math.max(0, scope.depth - 1);
        if (scope.depth === 0)
            scope.dotnet.invokeMethodAsync('OnDragLeave');
    };

    scope.onDrop = (e) => {
        if (!carriesFiles(e)) return;
        e.preventDefault();
        scope.depth = 0;

        const files = Array.from(e.dataTransfer.files || []).map(file => {
            const key = 'f' + (scope.nextKey++);
            scope.files.set(key, file);
            return {
                key,
                name: file.name,
                size: file.size,
                contentType: file.type || '',
                lastModified: file.lastModified || 0
            };
        });

        scope.dotnet.invokeMethodAsync('OnDrop', { files, x: e.clientX, y: e.clientY });
    };

    window.addEventListener('dragenter', scope.onDragEnter, true);
    window.addEventListener('dragover', scope.onDragOver, true);
    window.addEventListener('dragleave', scope.onDragLeave, true);
    window.addEventListener('drop', scope.onDrop, true);

    scopes.set(id, scope);
}

export function detach(id) {
    const scope = scopes.get(id);
    if (!scope) return;

    window.removeEventListener('dragenter', scope.onDragEnter, true);
    window.removeEventListener('dragover', scope.onDragOver, true);
    window.removeEventListener('dragleave', scope.onDragLeave, true);
    window.removeEventListener('drop', scope.onDrop, true);

    scope.files.clear();
    scopes.delete(id);
}

/**
 * Hands the bytes of one dropped file to .NET as a stream.
 *
 * A stream reference rather than a base64 round-trip: a 40MB drop turned into a string is 53MB of
 * JS heap plus the same again on the .NET side, and it has to be built before a single byte can be
 * read.
 */
export function read(id, key) {
    const scope = scopes.get(id);
    const file = scope && scope.files.get(key);
    if (!file)
        throw new Error(`Dropped file '${key}' is no longer available.`);

    // Returned raw, NOT wrapped in DotNet.createJSStreamReference. Blazor wraps the return value
    // itself when the .NET side asks for an IJSStreamReference, and wrapping it here first hands
    // that code a plain object instead of a Blob — which fails with "Supplied value is not a typed
    // array or blob", pointing at Blazor's internals rather than at this line.
    return file;
}

/**
 * Forgets a drop's files. Called once the .NET side is done with them, so a large drop does not sit
 * in JS memory for the life of the page.
 */
export function release(id, keys) {
    const scope = scopes.get(id);
    if (!scope) return;

    for (const key of keys)
        scope.files.delete(key);
}
