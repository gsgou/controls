namespace Shiny.Maui.Controls.Ribbons;

/// <summary>
/// The fades that say a tab's groups run off the edge of the bar.
/// </summary>
/// <remarks>
/// <para>
/// When a tab is wider than the ribbon the body scrolls, and a bar that scrolls looks exactly like one
/// that does not: the last group ends flush at the edge and there is nothing to say another one
/// follows. The platform scroll indicator is no help — it is hidden here deliberately, and on iOS and
/// Android it only appears once a scroll is already under way, which is after the moment the user
/// needed to be told.
/// </para>
/// <para>
/// Two overlays rather than a gradient on the scroller itself: the scroller's background belongs to the
/// body frame behind it, so fading the scroller would show whatever is behind the ribbon through its
/// own edge.
/// </para>
/// </remarks>
public partial class Ribbon
{
    /// <summary>How wide the fade at each edge is.</summary>
    const double ScrollHintWidth = 28;

    /// <summary>
    /// Below this much hidden content the fade is not drawn.
    /// </summary>
    /// <remarks>
    /// Measured content width and the scroller's own width are rounded independently, so a tab that
    /// fits exactly can report a content size a fraction wider and show a fade that never goes away.
    /// </remarks>
    const double ScrollHintTolerance = 1;

    void OnPanelScrolled(object? sender, ScrolledEventArgs e) => this.UpdateScrollHints();

    /// <summary>
    /// Repaints the edge fades for whichever tab is showing.
    /// </summary>
    /// <remarks>
    /// The brushes are rebuilt from <see cref="bodyFrame"/>'s resolved background rather than bound to
    /// a theme key. A colour token cannot reach a gradient stop — a dynamic resource set on a brush
    /// never resolves — so the only way for the fade to follow the theme is to read the colour the
    /// body actually ended up with, each time.
    /// </remarks>
    void UpdateScrollHints()
    {
        var current = this.panels.FirstOrDefault(x => ReferenceEquals(x.Tab, this.SelectedTab));

        if (current.Panel is not { } panel || panel.Width <= 0)
        {
            this.startFade.IsVisible = false;
            this.endFade.IsVisible = false;
            return;
        }

        var hidden = panel.ContentSize.Width - panel.Width;

        var atStart = panel.ScrollX > ScrollHintTolerance;
        var atEnd = panel.ScrollX < hidden - ScrollHintTolerance;

        if (atStart || atEnd)
        {
            var ground = this.bodyFrame.BackgroundColor ?? Colors.Transparent;

            this.startFade.Background = FadeFrom(ground, toRight: true);
            this.endFade.Background = FadeFrom(ground, toRight: false);
        }

        this.startFade.IsVisible = atStart;
        this.endFade.IsVisible = atEnd;
    }

    /// <summary>A hard-to-transparent ramp in the body's own colour, running away from the edge.</summary>
    static Brush FadeFrom(Color ground, bool toRight)
        => new LinearGradientBrush(
            [
                new GradientStop(ground, 0),
                new GradientStop(ground.WithAlpha(0), 1)
            ],
            toRight ? new Point(0, 0.5) : new Point(1, 0.5),
            toRight ? new Point(1, 0.5) : new Point(0, 0.5));
}
