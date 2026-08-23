using Shiny.Maui.Controls.FloatingPanel;
using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.QuickEntry;

namespace Shiny.Maui.Controls;

/// <summary>
/// A full-screen overlay control that integrates with OverlayHost/ShinyContentPage.
/// Place inside ShinyContentPage.Panels or an OverlayHost. When IsShown is true,
/// the backdrop dims (with optional blur) and custom content is displayed centered.
/// </summary>
public partial class Overlay : ContentView
{
    readonly ContentView overlayContainer;
    FrostedGlassView? blurBackdrop;
    ScreenGlowView? edgeGlow;
    bool latestTarget;
    bool workerRunning;

    public Overlay()
    {
        IsVisible = false;
        InputTransparent = false;

        overlayContainer = new ContentView
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };

        Content = overlayContainer;

        // Lets the overlay host something that is not a dialog — a prompt bar pinned near the top,
        // say — without every such caller re-implementing the backdrop, blur and show/hide worker.
        UpdateContentPlacement();

        // Last line: replays any styled property that was applied before the children
        // existed. Only fires for a plain Overlay - LoadingOverlay and friends mark
        // themselves, since a base constructor always runs first. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(Overlay));
    }

    OverlayHost? GetOverlayHost()
    {
        Element? current = Parent;
        while (current is not null)
        {
            if (current is OverlayHost host)
                return host;
            current = current.Parent;
        }
        return null;
    }

    void OnBlurRadiusChanged()
    {
        if (BlurRadius > 0)
        {
            if (blurBackdrop == null)
            {
                blurBackdrop = new FrostedGlassView
                {
                    BlurRadius = BlurRadius,
                    TintColor = Colors.Transparent,
                    TintOpacity = 0,
                    IsVisible = false,
                    InputTransparent = true
                };
            }
            else
            {
                blurBackdrop.BlurRadius = BlurRadius;
            }
        }
        else
        {
            blurBackdrop = null;
        }
    }

    /// <summary>
    /// Inserts the edge glow into the host, behind this overlay and in front of the backdrop.
    /// </summary>
    /// <remarks>
    /// Placed in the host rather than inside this view for the same reason the blur backdrop is: it
    /// has to span the whole page, and this view is only as big as its content. Input-transparent
    /// throughout — the glow is a signal, never a target.
    /// </remarks>
    void ShowEdgeGlowLayer(OverlayHost? overlayHost)
    {
        if (!this.ShowEdgeGlow || overlayHost == null)
            return;

        this.edgeGlow ??= new ScreenGlowView(this.GlowOptions ?? new ScreenGlowOptions())
        {
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Opacity = 0
        };

        if (!overlayHost.Children.Contains(this.edgeGlow))
        {
            var myIndex = overlayHost.Children.IndexOf(this);
            if (myIndex >= 0)
                overlayHost.Children.Insert(myIndex, this.edgeGlow);
            else
                overlayHost.Children.Add(this.edgeGlow);
        }

        this.edgeGlow.IsVisible = true;
        this.edgeGlow.Start();
        _ = this.edgeGlow.FadeToAsync(1, AnimationDuration);
    }

    void HideEdgeGlowLayer()
    {
        if (this.edgeGlow == null)
            return;

        var glow = this.edgeGlow;
        _ = HideAsync();

        async Task HideAsync()
        {
            try
            {
                await glow.FadeToAsync(0, AnimationDuration);
            }
            catch
            {
                // Detached mid-animation — snap to hidden below, same as the overlay itself does.
            }

            glow.Opacity = 0;
            glow.Stop();
            glow.IsVisible = false;

            if (GetOverlayHost() is { } host && host.Children.Contains(glow))
                host.Children.Remove(glow);
        }
    }

    void UpdateContentPlacement()
    {
        this.overlayContainer.VerticalOptions = this.ContentAlignment;
        this.overlayContainer.Margin = this.ContentMargin;
    }

    void UpdateOverlayContent()
    {
        if (OverlayContentTemplate == null)
        {
            overlayContainer.Content = null;
            return;
        }

        var content = OverlayContentTemplate.CreateContent() as View;

        // Detach whatever was there first. A template that hands back a view it already returned —
        // which is how a caller hosts one long-lived instance rather than rebuilding it per show —
        // would otherwise be refused for still having a parent.
        if (!ReferenceEquals(overlayContainer.Content, content))
            overlayContainer.Content = null;

        overlayContainer.Content = content;
    }

    async void OnIsShownChanged(bool shown)
    {
        // Always record the latest desired state. A worker loop drives the
        // actual animation, re-checking the target after each animation so a
        // rapid true→false (or false→true) flip never gets dropped — which
        // previously happened because an `if (isAnimating) return;` guard
        // ignored the second change and left the overlay stuck in the first
        // animation's end state.
        this.latestTarget = shown;
        if (this.workerRunning)
            return;

        this.workerRunning = true;
        try
        {
            bool current = !shown; // force first iteration
            while (current != this.latestTarget)
            {
                current = this.latestTarget;
                if (current)
                    await ShowAsync();
                else
                    await HideAsync();
            }
        }
        finally
        {
            this.workerRunning = false;
        }
    }

    async Task ShowAsync()
    {
        var overlayHost = GetOverlayHost();
        if (overlayHost != null)
            overlayHost.ShowBackdrop(this, AnimationDuration);

        // Show blur layer in the overlay host (behind this view)
        if (blurBackdrop != null && overlayHost != null)
        {
            if (!overlayHost.Children.Contains(blurBackdrop))
            {
                var myIndex = overlayHost.Children.IndexOf(this);
                if (myIndex >= 0)
                    overlayHost.Children.Insert(myIndex, blurBackdrop);
                else
                    overlayHost.Children.Add(blurBackdrop);
            }
            blurBackdrop.IsVisible = true;
            _ = blurBackdrop.FadeToAsync(1, AnimationDuration);
        }

        this.ShowEdgeGlowLayer(overlayHost);

        IsVisible = true;
        Opacity = 0;
        try
        {
            await this.FadeToAsync(1, AnimationDuration);
        }
        catch
        {
            // FadeToAsync can throw if the view is detached from the visual tree
            // mid-animation (e.g. the host page is navigated away during a
            // rapid IsShown toggle). Snap to the target state below regardless
            // so the visual state remains consistent when the host returns.
        }
        Opacity = 1;
    }

    async Task HideAsync()
    {
        var overlayHost = GetOverlayHost();
        if (overlayHost != null)
            overlayHost.HideBackdrop(this, AnimationDuration);

        if (blurBackdrop != null)
            _ = blurBackdrop.FadeToAsync(0, AnimationDuration);

        this.HideEdgeGlowLayer();

        try
        {
            await this.FadeToAsync(0, AnimationDuration);
        }
        catch
        {
            // See note in ShowAsync. Snap to hidden state below regardless of
            // whether the fade-out animation actually completed.
        }
        Opacity = 0;
        IsVisible = false;

        if (blurBackdrop != null)
        {
            blurBackdrop.IsVisible = false;
            var host = GetOverlayHost();
            if (host != null && host.Children.Contains(blurBackdrop))
                host.Children.Remove(blurBackdrop);
        }
    }
}
