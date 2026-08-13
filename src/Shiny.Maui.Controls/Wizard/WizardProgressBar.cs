using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Themes;
using Shiny.Maui.Controls.Infrastructure;
using GraphicsFont = Microsoft.Maui.Graphics.Font;

namespace Shiny.Maui.Controls;

/// <summary>One step as the progress indicator sees it.</summary>
public record WizardProgressItem(string Title, bool IsCompleted, bool IsCurrent, bool IsEnabled);

/// <summary>
/// The default <see cref="Wizard"/> progress indicator: pointed breadcrumb segments (or markers, or a
/// plain bar) drawn onto a canvas.
/// </summary>
/// <remarks>
/// Drawn rather than composed from <c>Border</c>s because a chevron is a notched polygon, and because a
/// <see cref="GraphicsView"/> renders identically on every head including AppKit and GTK4. Colours are
/// still bindable properties on this view, so they resolve from the theme through
/// <c>SetDynamicResource</c> and re-render on a theme swap; the drawable only reads them.
/// </remarks>
public class WizardProgressBar : GraphicsView
{
    IReadOnlyList<WizardProgressItem> items = Array.Empty<WizardProgressItem>();

    public WizardProgressBar()
    {
        this.Drawable = new WizardProgressDrawable(this);

        this.SetDynamicResource(CompletedColorProperty, ShinyThemeKeys.Color.PrimaryContainer);
        this.SetDynamicResource(CompletedTextColorProperty, ShinyThemeKeys.Color.OnPrimaryContainer);
        this.SetDynamicResource(CurrentColorProperty, ShinyThemeKeys.Color.Primary);
        this.SetDynamicResource(CurrentTextColorProperty, ShinyThemeKeys.Color.OnPrimary);
        this.SetDynamicResource(UpcomingColorProperty, ShinyThemeKeys.Color.SurfaceContainerHighest);
        this.SetDynamicResource(UpcomingTextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
        this.SetDynamicResource(FontSizeProperty, ShinyThemeKeys.Type.LabelMediumSize);
        this.SetDynamicResource(MarkerStrokeWidthProperty, ShinyThemeKeys.Border.Medium);

        this.StartInteraction += this.OnStartInteraction;

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(WizardProgressBar));
    }


    public static readonly BindableProperty StyleKindProperty = BindableProperty.Create(
        nameof(StyleKind), typeof(WizardProgressStyle), typeof(WizardProgressBar), WizardProgressStyle.Chevron,
        propertyChanged: Redraw);

    public static readonly BindableProperty ShowTitlesProperty = BindableProperty.Create(
        nameof(ShowTitles), typeof(bool), typeof(WizardProgressBar), true, propertyChanged: Redraw);

    public static readonly BindableProperty IsInteractiveProperty = BindableProperty.Create(
        nameof(IsInteractive), typeof(bool), typeof(WizardProgressBar), false);

    public static readonly BindableProperty CompletedColorProperty = BindableProperty.Create(
        nameof(CompletedColor), typeof(Color), typeof(WizardProgressBar), null, propertyChanged: Redraw);

    public static readonly BindableProperty CompletedTextColorProperty = BindableProperty.Create(
        nameof(CompletedTextColor), typeof(Color), typeof(WizardProgressBar), null, propertyChanged: Redraw);

    public static readonly BindableProperty CurrentColorProperty = BindableProperty.Create(
        nameof(CurrentColor), typeof(Color), typeof(WizardProgressBar), null, propertyChanged: Redraw);

    public static readonly BindableProperty CurrentTextColorProperty = BindableProperty.Create(
        nameof(CurrentTextColor), typeof(Color), typeof(WizardProgressBar), null, propertyChanged: Redraw);

    public static readonly BindableProperty UpcomingColorProperty = BindableProperty.Create(
        nameof(UpcomingColor), typeof(Color), typeof(WizardProgressBar), null, propertyChanged: Redraw);

    public static readonly BindableProperty UpcomingTextColorProperty = BindableProperty.Create(
        nameof(UpcomingTextColor), typeof(Color), typeof(WizardProgressBar), null, propertyChanged: Redraw);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(WizardProgressBar), 0d, propertyChanged: Redraw);

    /// <summary>
    /// Width of the ring drawn around the current dot. Bound to the theme's medium stroke token, so a
    /// pack with heavier borders draws a heavier marker.
    /// </summary>
    public static readonly BindableProperty MarkerStrokeWidthProperty = BindableProperty.Create(
        nameof(MarkerStrokeWidth), typeof(double), typeof(WizardProgressBar), 0d, propertyChanged: Redraw);

    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(
        nameof(Caption), typeof(string), typeof(WizardProgressBar), null, propertyChanged: Redraw);

    public static readonly BindableProperty FractionProperty = BindableProperty.Create(
        nameof(Fraction), typeof(double), typeof(WizardProgressBar), 0d, propertyChanged: Redraw);


    public WizardProgressStyle StyleKind
    {
        get => (WizardProgressStyle)this.GetValue(StyleKindProperty);
        set => this.SetValue(StyleKindProperty, value);
    }

    public bool ShowTitles
    {
        get => (bool)this.GetValue(ShowTitlesProperty);
        set => this.SetValue(ShowTitlesProperty, value);
    }

    /// <summary>Whether a tap should raise <see cref="StepSelected"/>.</summary>
    public bool IsInteractive
    {
        get => (bool)this.GetValue(IsInteractiveProperty);
        set => this.SetValue(IsInteractiveProperty, value);
    }

    public Color? CompletedColor
    {
        get => (Color?)this.GetValue(CompletedColorProperty);
        set => this.SetValue(CompletedColorProperty, value);
    }

    public Color? CompletedTextColor
    {
        get => (Color?)this.GetValue(CompletedTextColorProperty);
        set => this.SetValue(CompletedTextColorProperty, value);
    }

    public Color? CurrentColor
    {
        get => (Color?)this.GetValue(CurrentColorProperty);
        set => this.SetValue(CurrentColorProperty, value);
    }

    public Color? CurrentTextColor
    {
        get => (Color?)this.GetValue(CurrentTextColorProperty);
        set => this.SetValue(CurrentTextColorProperty, value);
    }

    public Color? UpcomingColor
    {
        get => (Color?)this.GetValue(UpcomingColorProperty);
        set => this.SetValue(UpcomingColorProperty, value);
    }

    public Color? UpcomingTextColor
    {
        get => (Color?)this.GetValue(UpcomingTextColorProperty);
        set => this.SetValue(UpcomingTextColorProperty, value);
    }

    public double FontSize
    {
        get => (double)this.GetValue(FontSizeProperty);
        set => this.SetValue(FontSizeProperty, value);
    }

    public double MarkerStrokeWidth
    {
        get => (double)this.GetValue(MarkerStrokeWidthProperty);
        set => this.SetValue(MarkerStrokeWidthProperty, value);
    }

    /// <summary>Caption for <see cref="WizardProgressStyle.Bar"/> — "Step 2 of 5 — Delivery".</summary>
    public string? Caption
    {
        get => (string?)this.GetValue(CaptionProperty);
        set => this.SetValue(CaptionProperty, value);
    }

    /// <summary>0..1 completion, used by <see cref="WizardProgressStyle.Bar"/>.</summary>
    public double Fraction
    {
        get => (double)this.GetValue(FractionProperty);
        set => this.SetValue(FractionProperty, value);
    }

    public IReadOnlyList<WizardProgressItem> Items
    {
        get => this.items;
        set
        {
            this.items = value ?? Array.Empty<WizardProgressItem>();
            this.Invalidate();
        }
    }

    /// <summary>Raised with the index of the segment that was tapped, when <see cref="IsInteractive"/>.</summary>
    public event EventHandler<int>? StepSelected;


    void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        if (!this.IsInteractive || this.items.Count == 0 || e.Touches.Length == 0 || this.Width <= 0)
            return;

        var x = e.Touches[0].X;
        var index = (int)(x / (this.Width / this.items.Count));
        if (index >= 0 && index < this.items.Count)
            this.StepSelected?.Invoke(this, index);
    }

    static void Redraw(BindableObject bindable, object oldValue, object newValue)
        => StyleGuard.WhenReady(bindable, typeof(WizardProgressBar), () => ((WizardProgressBar)bindable).Invalidate());
}


sealed class WizardProgressDrawable(WizardProgressBar owner) : IDrawable
{
    const float ArrowDepth = 12f;
    const float SegmentGap = 3f;
    const float DefaultMarkerStroke = 2f;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var items = owner.Items;
        if (items.Count == 0 || rect.Width <= 0 || rect.Height <= 0)
            return;

        switch (owner.StyleKind)
        {
            case WizardProgressStyle.Chevron:
                this.DrawChevrons(canvas, rect, items);
                break;

            case WizardProgressStyle.Dots:
                this.DrawMarkers(canvas, rect, items);
                break;

            case WizardProgressStyle.Bar:
                this.DrawBar(canvas, rect);
                break;
        }
    }


    void DrawChevrons(ICanvas canvas, RectF rect, IReadOnlyList<WizardProgressItem> items)
    {
        var depth = Math.Min(ArrowDepth, rect.Height / 3f);
        var segment = (rect.Width - depth) / items.Count;
        var fontSize = this.FontSize();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var x0 = i * segment;
            var x1 = x0 + segment - SegmentGap;
            var isLast = i == items.Count - 1;
            var isFirst = i == 0;

            var path = new PathF();
            path.MoveTo(x0, 0);
            path.LineTo(x1, 0);
            if (!isLast)
                path.LineTo(x1 + depth, rect.Height / 2f);
            else
                path.LineTo(x1 + depth, 0);
            path.LineTo(x1 + depth, rect.Height);
            path.LineTo(x1, rect.Height);
            path.LineTo(x0, rect.Height);
            if (!isFirst)
                path.LineTo(x0 + depth, rect.Height / 2f);
            path.Close();

            canvas.FillColor = this.Fill(item);
            canvas.FillPath(path);

            if (!owner.ShowTitles || string.IsNullOrEmpty(item.Title))
                continue;

            var textLeft = x0 + (isFirst ? 4f : depth + 2f);
            var textWidth = x1 - textLeft - 2f;
            if (textWidth <= 0)
                continue;

            canvas.FontColor = this.Text(item);
            canvas.FontSize = fontSize;
            canvas.Font = item.IsCurrent ? GraphicsFont.DefaultBold : GraphicsFont.Default;
            canvas.Alpha = item.IsEnabled ? 1f : 0.5f;
            canvas.DrawString(
                item.Title,
                textLeft,
                0,
                textWidth,
                rect.Height,
                HorizontalAlignment.Center,
                VerticalAlignment.Center,
                TextFlow.ClipBounds
            );
            canvas.Alpha = 1f;
        }
    }


    void DrawMarkers(ICanvas canvas, RectF rect, IReadOnlyList<WizardProgressItem> items)
    {
        var fontSize = this.FontSize();
        var titleRow = owner.ShowTitles ? fontSize + 6f : 0f;
        var diameter = Math.Min(28f, Math.Max(12f, rect.Height - titleRow));
        var radius = diameter / 2f;
        var centerY = radius + 1f;
        var slot = rect.Width / items.Count;

        // Connector first, so the markers sit on top of it.
        canvas.StrokeSize = this.MarkerStroke();
        for (var i = 0; i < items.Count - 1; i++)
        {
            var from = (i + 0.5f) * slot + radius;
            var to = (i + 1.5f) * slot - radius;
            canvas.StrokeColor = items[i].IsCompleted ? owner.CompletedColor : owner.UpcomingColor;
            canvas.DrawLine(from, centerY, to, centerY);
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var cx = (i + 0.5f) * slot;

            canvas.Alpha = item.IsEnabled ? 1f : 0.5f;
            canvas.FillColor = this.Fill(item);
            canvas.FillCircle(cx, centerY, radius);

            canvas.FontColor = this.Text(item);
            canvas.FontSize = fontSize;
            canvas.Font = item.IsCurrent ? GraphicsFont.DefaultBold : GraphicsFont.Default;
            canvas.DrawString(
                item.IsCompleted ? "✓" : (i + 1).ToString(),
                cx - radius,
                centerY - radius,
                diameter,
                diameter,
                HorizontalAlignment.Center,
                VerticalAlignment.Center
            );

            if (owner.ShowTitles && !string.IsNullOrEmpty(item.Title))
            {
                canvas.FontColor = item.IsCurrent ? owner.CurrentColor : owner.UpcomingTextColor;
                canvas.Font = item.IsCurrent ? GraphicsFont.DefaultBold : GraphicsFont.Default;
                canvas.DrawString(
                    item.Title,
                    cx - slot / 2f + 2f,
                    diameter + 2f,
                    slot - 4f,
                    titleRow,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center,
                    TextFlow.ClipBounds
                );
            }

            canvas.Alpha = 1f;
        }
    }


    void DrawBar(ICanvas canvas, RectF rect)
    {
        var fontSize = this.FontSize();
        var caption = owner.Caption;
        var captionRow = string.IsNullOrEmpty(caption) ? 0f : fontSize + 6f;
        var track = Math.Min(8f, Math.Max(4f, rect.Height - captionRow - 4f));
        var radius = track / 2f;
        var top = captionRow;

        canvas.FillColor = owner.UpcomingColor;
        canvas.FillRoundedRectangle(0, top, rect.Width, track, radius);

        var fraction = Math.Clamp(owner.Fraction, 0d, 1d);
        if (fraction > 0)
        {
            canvas.FillColor = owner.CurrentColor;
            canvas.FillRoundedRectangle(0, top, (float)(rect.Width * fraction), track, radius);
        }

        if (captionRow > 0)
        {
            canvas.FontColor = owner.UpcomingTextColor;
            canvas.FontSize = fontSize;
            canvas.Font = GraphicsFont.Default;
            canvas.DrawString(caption, 0, 0, rect.Width, captionRow, HorizontalAlignment.Left, VerticalAlignment.Center, TextFlow.ClipBounds);
        }
    }


    /// <summary>Zero means the token has not resolved yet - see <see cref="FontSize"/>.</summary>
    float MarkerStroke()
    {
        var width = owner.MarkerStrokeWidth;
        return width > 0 ? (float)width : DefaultMarkerStroke;
    }

    float FontSize()
    {
        // Zero means the theme token has not resolved yet (no application resources in a bare host);
        // fall back to something legible rather than drawing invisible text.
        var size = owner.FontSize;
        return size > 0 ? (float)size : 13f;
    }

    Color? Fill(WizardProgressItem item)
        => item.IsCurrent ? owner.CurrentColor
            : item.IsCompleted ? owner.CompletedColor
            : owner.UpcomingColor;

    Color? Text(WizardProgressItem item)
        => item.IsCurrent ? owner.CurrentTextColor
            : item.IsCompleted ? owner.CompletedTextColor
            : owner.UpcomingTextColor;
}
