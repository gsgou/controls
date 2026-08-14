using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Remembers which walkthroughs a user has already been through, so a tour with a
/// <c>RememberRunKey</c> runs once and then stays out of the way.
/// </summary>
/// <remarks>
/// Register your own implementation in DI to keep the flag with the rest of your user state — a server
/// profile, say — rather than in one browser's local storage.
/// </remarks>
public interface IWalkthroughStore
{
    ValueTask<bool> HasRunAsync(string key);

    ValueTask SetHasRunAsync(string key, bool value);
}


/// <summary>The default store, backed by <c>localStorage</c>.</summary>
public sealed class LocalStorageWalkthroughStore(IJSRuntime js) : IWalkthroughStore, IAsyncDisposable
{
    const string Prefix = "shiny.walkthrough.";

    IJSObjectReference? module;


    public async ValueTask<bool> HasRunAsync(string key)
    {
        var module = await this.GetModuleAsync();
        if (module is null)
            return false;

        try
        {
            var value = await module.InvokeAsync<string?>("load", Prefix + key);
            return string.Equals(value, "1", StringComparison.Ordinal);
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
    }


    public async ValueTask SetHasRunAsync(string key, bool value)
    {
        var module = await this.GetModuleAsync();
        if (module is null)
            return;

        try
        {
            await module.InvokeVoidAsync("save", Prefix + key, value ? "1" : "0");
        }
        catch (JSDisconnectedException)
        {
        }
    }


    async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (this.module is not null)
            return this.module;

        try
        {
            this.module = await js.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Shiny.Blazor.Controls/walkthrough.js"
            );
        }
        catch (JSDisconnectedException)
        {
            // Prerendering, or a circuit that has already gone. Nothing is remembered, which means the
            // tour shows again — the safe direction to fail in.
        }
        return this.module;
    }


    public async ValueTask DisposeAsync()
    {
        if (this.module is null)
            return;

        try
        {
            await this.module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }
}


/// <summary>A store that never remembers anything — useful in tests and while designing a tour.</summary>
public sealed class InMemoryWalkthroughStore : IWalkthroughStore
{
    readonly Dictionary<string, bool> values = new();

    public ValueTask<bool> HasRunAsync(string key)
        => ValueTask.FromResult(this.values.TryGetValue(key, out var value) && value);

    public ValueTask SetHasRunAsync(string key, bool value)
    {
        this.values[key] = value;
        return ValueTask.CompletedTask;
    }
}
