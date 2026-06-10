using Microsoft.JSInterop;
using Shiny.Blazor.Controls.Kiosk.Docking;

namespace Sample.Blazor;


public class LocalStorageDockLayoutStore(IJSRuntime js) : IDockLayoutStore
{
    const string Key = "shiny-dock-layout";

    public int SaveDebounceMs => 400;

    public async Task<DockRoot?> LoadAsync(CancellationToken ct = default)
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", ct, Key);
        return json is null ? null : DockSerialization.Deserialize(json);
    }

    public async Task SaveAsync(DockRoot root, CancellationToken ct = default)
        => await js.InvokeVoidAsync("localStorage.setItem", ct, Key, DockSerialization.Serialize(root));

    public async Task ClearAsync()
        => await js.InvokeVoidAsync("localStorage.removeItem", Key);
}
