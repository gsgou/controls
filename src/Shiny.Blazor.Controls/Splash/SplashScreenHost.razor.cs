using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.Splash;

/// <summary>
/// Renders nothing - it owns the handoff from the pre-boot splash to the app.
/// Drop one into your layout (or App.razor) and the splash goes away once Blazor is up.
/// </summary>
public partial class SplashScreenHost
{
    /// <summary>
    /// Hide the splash automatically once the app has rendered. Set false to keep it up
    /// until <see cref="ISplashScreen.HideAsync"/> is called from your own code.
    /// </summary>
    [Parameter] public bool AutoHide { get; set; } = true;

    /// <summary>
    /// Startup work to await before hiding. Any exception is surfaced, but the splash is
    /// dismissed first so the error is actually visible.
    /// </summary>
    [Parameter] public Func<Task>? Until { get; set; }

    /// <summary>
    /// Extra milliseconds to hold the splash after the app is ready.
    /// </summary>
    [Parameter] public int Delay { get; set; }

    /// <summary>
    /// Overrides the fade duration configured in <c>shinySplash.show</c>.
    /// </summary>
    [Parameter] public int? FadeDuration { get; set; }

    /// <summary>
    /// Raised after the splash has been dismissed.
    /// </summary>
    [Parameter] public EventCallback Hidden { get; set; }

    bool handled;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !this.AutoHide || this.handled)
            return;

        this.handled = true;
        try
        {
            if (this.Until != null)
                await this.Until.Invoke();
        }
        finally
        {
            await this.HideAsync();
        }
    }

    /// <summary>
    /// Dismisses the splash. Exposed for <c>@ref</c> use when <see cref="AutoHide"/> is false.
    /// </summary>
    public async Task HideAsync()
    {
        this.handled = true;
        if (this.Delay > 0)
            await Task.Delay(this.Delay);

        await this.SplashScreen.HideAsync(this.FadeDuration);
        await this.Hidden.InvokeAsync();
    }
}
