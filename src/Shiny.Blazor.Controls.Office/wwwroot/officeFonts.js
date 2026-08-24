// Fetches a font file and hands its bytes to .NET.
//
// Blazor marshals a Uint8Array to byte[] directly, so the font never becomes a base64 string on the
// way through - which for ~3MB of TTF would cost both the encode and a third again in size.
export async function fetchFont(url) {
    const response = await fetch(url, { cache: 'force-cache' });
    if (!response.ok)
        throw new Error(`${response.status} ${response.statusText}`);

    return new Uint8Array(await response.arrayBuffer());
}
