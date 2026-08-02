using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls.Splash;

/// <summary>
/// Default <see cref="ISplashScreen"/>, talking to the global <c>shinySplash</c> object.
/// </summary>
/// <remarks>
/// Deliberately calls the global rather than importing an ES module: the script has to be a
/// classic script so it can paint before Blazor boots, and by the time managed code runs the
/// global already exists. Every call is a best-effort no-op if the script was not referenced -
/// forgetting the &lt;script&gt; tag should not take the app down at startup.
/// </remarks>
public sealed class SplashScreenService : ISplashScreen
{
    readonly IJSRuntime js;
    bool unavailable;

    public SplashScreenService(IJSRuntime js) => this.js = js;

    public async ValueTask<bool> IsVisibleAsync()
    {
        if (this.unavailable)
            return false;

        try
        {
            return await this.js.InvokeAsync<bool>("shinySplash.isVisible");
        }
        catch (JSException)
        {
            this.unavailable = true;
            return false;
        }
        catch (InvalidOperationException)
        {
            // prerendering - no JS interop available yet
            return false;
        }
    }

    public ValueTask SetStatusAsync(string? text)
        => this.InvokeAsync("shinySplash.status", text);

    public ValueTask SetProgressAsync(double? value)
        => this.InvokeAsync("shinySplash.progress", value);

    public ValueTask HideAsync(int? fadeMs = null)
        => this.InvokeAsync("shinySplash.hide", fadeMs);

    async ValueTask InvokeAsync(string identifier, object? arg)
    {
        if (this.unavailable)
            return;

        try
        {
            await this.js.InvokeVoidAsync(identifier, arg);
        }
        catch (JSException)
        {
            this.unavailable = true;
        }
        catch (InvalidOperationException)
        {
            // prerendering - no JS interop available yet
        }
    }
}
