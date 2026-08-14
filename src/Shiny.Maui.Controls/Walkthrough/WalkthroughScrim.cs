using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls;

/// <summary>
/// The dimmed backdrop with a hole in it — the spotlight.
/// </summary>
/// <remarks>
/// Drawn on a <see cref="GraphicsView"/> rather than assembled from four boxes around the target,
/// because a cut-out has to be a single filled shape for the corners to round and for the whole thing
/// to animate as one surface as it travels from step to step. The hole is punched by appending it to
/// the same path as the full-canvas rectangle and filling with
/// <see cref="WindingMode.EvenOdd"/>, which is the only approach that survives every head: MAUI's
/// clip API subtracts rectangles only, so it cannot round a corner or draw a circle.
/// </remarks>
public class WalkthroughScrim : GraphicsView
{
    public WalkthroughScrim()
    {
        this.Drawable = new ScrimDrawable(this);
        this.BackgroundColor = Colors.Transparent;

        // Paints only — every tap goes to the shields the walkthrough lays over it, which is what lets
        // a hole pass touches through to the real control underneath.
        this.InputTransparent = true;

        this.colorProbe = new BoxView { IsVisible = false, WidthRequest = 0, HeightRequest = 0 };
        this.colorProbe.SetDynamicResource(BoxView.ColorProperty, ShinyThemeKeys.Color.Scrim);
        this.colorProbe.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == BoxView.ColorProperty.PropertyName)
                this.Invalidate();
        };

        StyleGuard.MarkReady(this, typeof(WalkthroughScrim));
    }

    // The scrim has no children of its own (a GraphicsView cannot hold any), so the probe is parented
    // by the walkthrough into its layer. Kept here because the colour it resolves belongs to the scrim.
    readonly BoxView colorProbe;

    /// <summary>The probe that resolves the default scrim colour. The owner parents it.</summary>
    internal BoxView ColorProbe => this.colorProbe;


    static void Redraw(BindableObject b, object o, object n)
        => StyleGuard.WhenReady(b, typeof(WalkthroughScrim), () => ((WalkthroughScrim)b).Invalidate());


    public static readonly BindableProperty OverlayColorProperty = BindableProperty.Create(
        nameof(OverlayColor), typeof(Color), typeof(WalkthroughScrim), null, propertyChanged: Redraw);

    public static readonly BindableProperty OverlayOpacityProperty = BindableProperty.Create(
        nameof(OverlayOpacity), typeof(double), typeof(WalkthroughScrim), 0.8d, propertyChanged: Redraw);

    public static readonly BindableProperty HoleProperty = BindableProperty.Create(
        nameof(Hole), typeof(Rect), typeof(WalkthroughScrim), Rect.Zero, propertyChanged: Redraw);

    public static readonly BindableProperty HoleShapeProperty = BindableProperty.Create(
        nameof(HoleShape), typeof(WalkthroughHighlight), typeof(WalkthroughScrim),
        WalkthroughHighlight.RoundedRectangle, propertyChanged: Redraw);

    public static readonly BindableProperty HoleCornerRadiusProperty = BindableProperty.Create(
        nameof(HoleCornerRadius), typeof(double), typeof(WalkthroughScrim), 10d, propertyChanged: Redraw);

    public static readonly BindableProperty RingColorProperty = BindableProperty.Create(
        nameof(RingColor), typeof(Color), typeof(WalkthroughScrim), null, propertyChanged: Redraw);

    public static readonly BindableProperty RingThicknessProperty = BindableProperty.Create(
        nameof(RingThickness), typeof(double), typeof(WalkthroughScrim), 0d, propertyChanged: Redraw);

    /// <summary>Leave unset to follow the theme's scrim token.</summary>
    public Color? OverlayColor
    {
        get => (Color?)this.GetValue(OverlayColorProperty);
        set => this.SetValue(OverlayColorProperty, value);
    }

    public double OverlayOpacity
    {
        get => (double)this.GetValue(OverlayOpacityProperty);
        set => this.SetValue(OverlayOpacityProperty, value);
    }

    /// <summary>The cut-out, in this view's coordinates. Zero-sized means "no hole — dim everything".</summary>
    public Rect Hole
    {
        get => (Rect)this.GetValue(HoleProperty);
        set => this.SetValue(HoleProperty, value);
    }

    public WalkthroughHighlight HoleShape
    {
        get => (WalkthroughHighlight)this.GetValue(HoleShapeProperty);
        set => this.SetValue(HoleShapeProperty, value);
    }

    public double HoleCornerRadius
    {
        get => (double)this.GetValue(HoleCornerRadiusProperty);
        set => this.SetValue(HoleCornerRadiusProperty, value);
    }

    /// <summary>An outline traced round the cut-out. Off by default.</summary>
    public Color? RingColor
    {
        get => (Color?)this.GetValue(RingColorProperty);
        set => this.SetValue(RingColorProperty, value);
    }

    public double RingThickness
    {
        get => (double)this.GetValue(RingThicknessProperty);
        set => this.SetValue(RingThicknessProperty, value);
    }


    /// <summary>Appends the hole's outline to a path, in whichever shape was asked for.</summary>
    internal static void AppendHole(PathF path, RectF hole, WalkthroughHighlight shape, float radius)
    {
        switch (shape)
        {
            case WalkthroughHighlight.Rectangle:
                path.AppendRectangle(hole);
                break;

            case WalkthroughHighlight.Circle:
                // A circle that covers the target rather than one inscribed in it, so a wide button is
                // enclosed instead of clipped at the ends.
                var r = MathF.Sqrt((hole.Width * hole.Width) + (hole.Height * hole.Height)) / 2f;
                path.AppendCircle(hole.Center.X, hole.Center.Y, r);
                break;

            case WalkthroughHighlight.Ellipse:
                path.AppendEllipse(hole);
                break;

            default:
                // Never round further than half the shorter side, or the corners cross over and the
                // hole renders as a bow-tie.
                var capped = Math.Min(radius, Math.Min(hole.Width, hole.Height) / 2f);
                path.AppendRoundedRectangle(hole, Math.Max(0, capped));
                break;
        }
    }


    sealed class ScrimDrawable(WalkthroughScrim owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
                return;

            var color = owner.OverlayColor ?? owner.ColorProbe.Color ?? Colors.Black;
            var opacity = (float)Math.Clamp(owner.OverlayOpacity, 0, 1);
            if (opacity <= 0)
                return;

            var hole = ToRectF(owner.Hole);
            var hasHole = owner.HoleShape != WalkthroughHighlight.None && hole.Width > 0 && hole.Height > 0;

            var path = new PathF();
            path.AppendRectangle(dirtyRect);

            if (hasHole)
                AppendHole(path, hole, owner.HoleShape, (float)owner.HoleCornerRadius);

            canvas.FillColor = color.WithAlpha(opacity);
            // Even-odd is what turns the second sub-path into a hole rather than a second filled shape.
            canvas.FillPath(path, WindingMode.EvenOdd);

            if (hasHole && owner.RingThickness > 0 && owner.RingColor is { } ring)
            {
                var outline = new PathF();
                AppendHole(outline, hole, owner.HoleShape, (float)owner.HoleCornerRadius);

                canvas.StrokeColor = ring;
                canvas.StrokeSize = (float)owner.RingThickness;
                canvas.DrawPath(outline);
            }
        }

        static RectF ToRectF(Rect rect)
            => new((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
    }
}
