using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// Draws the screen-edge glow as an overlay on the current page — the only way to get it on iOS and
/// Android, and what desktop falls back to where a click-through overlay above other windows is not
/// allowed.
/// </summary>
/// <remarks>
/// It rims the page rather than the display, which is the same thing on a phone and not on a desktop
/// with the app in a window. That difference is the entire reason the desktop presenter exists.
/// </remarks>
sealed class InAppScreenGlowPresenter : IScreenGlowPresenter
{
    ContentPage? page;
    PageOverlay.ScreenGlowLayer? layer;
    ScreenGlowView? glow;
    bool visible;

    public QuickEntryPresentation Kind => QuickEntryPresentation.InApp;

    public bool IsSupported => PageOverlay.CurrentPage() != null;

    public async Task ShowAsync(ScreenGlowOptions options)
    {
        this.Attach(options);
        if (this.layer == null || this.glow == null)
            return;

        this.visible = true;
        this.layer.IsVisible = true;
        this.glow.Opacity = 0;
        this.glow.Start();

        // Guarded the same way as the popup's entrance: a host whose ticker never ticks would leave
        // the glow at zero opacity, present and invisible.
        var fade = this.glow.FadeToAsync(1, (uint)options.FadeDuration.TotalMilliseconds);
        await Task.WhenAny(fade, Task.Delay(options.FadeDuration + TimeSpan.FromMilliseconds(250))).ConfigureAwait(true);
        if (this.visible)
            this.glow.Opacity = 1;
    }

    public async Task HideAsync(ScreenGlowOptions options)
    {
        if (this.layer == null || this.glow == null)
            return;

        this.visible = false;
        var fade = this.glow.FadeToAsync(0, (uint)options.FadeDuration.TotalMilliseconds);
        await Task.WhenAny(fade, Task.Delay(options.FadeDuration + TimeSpan.FromMilliseconds(250))).ConfigureAwait(true);

        // A Show that landed during the fade must not be undone by this Hide.
        if (this.visible)
            return;

        this.glow.Opacity = 0;
        this.glow.Stop();
        this.layer.IsVisible = false;
    }

    public void Teardown()
    {
        this.glow?.Stop();
        if (this.layer != null)
        {
            this.layer.Children.Clear();
            this.layer.IsVisible = false;
        }

        this.layer = null;
        this.glow = null;
        this.page = null;
    }

    void Attach(ScreenGlowOptions options)
    {
        var current = PageOverlay.CurrentPage();
        if (current == null)
            return;

        if (ReferenceEquals(current, this.page) && this.layer != null)
            return;

        if (this.layer != null)
        {
            this.layer.Children.Clear();
            this.layer.IsVisible = false;
        }

        this.page = current;
        this.layer = PageOverlay.GetOrCreateLayer<PageOverlay.ScreenGlowLayer>(current, PageOverlay.Layers.ScreenGlow);
        this.layer!.IsVisible = false;
        this.layer.Children.Clear();

        // InputTransparent all the way down: the glow is decoration over live content and must never
        // take a tap meant for what is underneath it.
        this.glow = new ScreenGlowView(options)
        {
            Opacity = 0,
            InputTransparent = true
        };
        AbsoluteLayout.SetLayoutBounds(this.glow, new Rect(0, 0, 1, 1));
        AbsoluteLayout.SetLayoutFlags(this.glow, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
        this.layer.Children.Add(this.glow);
    }
}
