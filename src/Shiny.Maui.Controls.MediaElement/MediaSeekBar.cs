using Shiny.Maui.Controls.Media.Internal;

namespace Shiny.Maui.Controls.Media;

/// <summary>
/// The scrubber a <see cref="MediaElement"/> puts in its transport bar: a played track, a buffered-ahead
/// track, and a draggable thumb.
/// </summary>
/// <remarks>
/// Purpose-built rather than reusing Shiny's <c>Slider</c> because a seek bar needs two things a value
/// slider doesn't have — a second track showing how far the download has buffered, and explicit
/// <see cref="DragStarted"/>/<see cref="DragCompleted"/> signals so the owner can stop writing the
/// player's position into the thumb while a finger is on it. It's public so a hand-rolled transport bar
/// can use it too.
/// </remarks>
public class MediaSeekBar : GraphicsView
{
    readonly SeekBarDrawable drawable = new();
    double dragStartFraction;
    bool isDragging;

    public MediaSeekBar()
    {
        this.Drawable = this.drawable;
        this.HeightRequest = 32;
        this.VerticalOptions = LayoutOptions.Center;
        SemanticProperties.SetDescription(this, "Seek");

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += this.OnPanUpdated;
        this.GestureRecognizers.Add(pan);

        var tap = new TapGestureRecognizer();
        tap.Tapped += this.OnTapped;
        this.GestureRecognizers.Add(tap);
    }


    /// <summary>The playhead. Ignored while the user is dragging the thumb.</summary>
    public static readonly BindableProperty PositionProperty = BindableProperty.Create(
        nameof(Position), typeof(TimeSpan), typeof(MediaSeekBar), TimeSpan.Zero,
        propertyChanged: (b, _, _) => ((MediaSeekBar)b).Refresh());

    /// <summary>Total media length. A zero duration disables interaction (nothing to seek within).</summary>
    public static readonly BindableProperty DurationProperty = BindableProperty.Create(
        nameof(Duration), typeof(TimeSpan), typeof(MediaSeekBar), TimeSpan.Zero,
        propertyChanged: (b, _, _) => ((MediaSeekBar)b).Refresh());

    /// <summary>How much is buffered ahead, 0..1, drawn as the dimmer secondary track.</summary>
    public static readonly BindableProperty BufferedProgressProperty = BindableProperty.Create(
        nameof(BufferedProgress), typeof(double), typeof(MediaSeekBar), 0d,
        propertyChanged: (b, _, _) => ((MediaSeekBar)b).Refresh());

    /// <summary>Colour of the unplayed track.</summary>
    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(MediaSeekBar), Color.FromRgba(255, 255, 255, 70),
        propertyChanged: (b, _, _) => ((MediaSeekBar)b).Refresh());

    /// <summary>Colour of the played track and the thumb.</summary>
    public static readonly BindableProperty ProgressColorProperty = BindableProperty.Create(
        nameof(ProgressColor), typeof(Color), typeof(MediaSeekBar), Colors.White,
        propertyChanged: (b, _, _) => ((MediaSeekBar)b).Refresh());

    /// <summary>Colour of the buffered-ahead track.</summary>
    public static readonly BindableProperty BufferedColorProperty = BindableProperty.Create(
        nameof(BufferedColor), typeof(Color), typeof(MediaSeekBar), Color.FromRgba(255, 255, 255, 130),
        propertyChanged: (b, _, _) => ((MediaSeekBar)b).Refresh());

    /// <summary>Thickness of the track. Default 4.</summary>
    public static readonly BindableProperty TrackHeightProperty = BindableProperty.Create(
        nameof(TrackHeight), typeof(double), typeof(MediaSeekBar), 4d,
        propertyChanged: (b, _, _) => ((MediaSeekBar)b).Refresh());

    /// <summary>Diameter of the thumb. Default 14; it grows while dragging.</summary>
    public static readonly BindableProperty ThumbSizeProperty = BindableProperty.Create(
        nameof(ThumbSize), typeof(double), typeof(MediaSeekBar), 14d,
        propertyChanged: (b, _, _) => ((MediaSeekBar)b).Refresh());


    /// <inheritdoc cref="PositionProperty"/>
    public TimeSpan Position
    {
        get => (TimeSpan)this.GetValue(PositionProperty);
        set => this.SetValue(PositionProperty, value);
    }

    /// <inheritdoc cref="DurationProperty"/>
    public TimeSpan Duration
    {
        get => (TimeSpan)this.GetValue(DurationProperty);
        set => this.SetValue(DurationProperty, value);
    }

    /// <inheritdoc cref="BufferedProgressProperty"/>
    public double BufferedProgress
    {
        get => (double)this.GetValue(BufferedProgressProperty);
        set => this.SetValue(BufferedProgressProperty, value);
    }

    /// <inheritdoc cref="TrackColorProperty"/>
    public Color TrackColor
    {
        get => (Color)this.GetValue(TrackColorProperty);
        set => this.SetValue(TrackColorProperty, value);
    }

    /// <inheritdoc cref="ProgressColorProperty"/>
    public Color ProgressColor
    {
        get => (Color)this.GetValue(ProgressColorProperty);
        set => this.SetValue(ProgressColorProperty, value);
    }

    /// <inheritdoc cref="BufferedColorProperty"/>
    public Color BufferedColor
    {
        get => (Color)this.GetValue(BufferedColorProperty);
        set => this.SetValue(BufferedColorProperty, value);
    }

    /// <inheritdoc cref="TrackHeightProperty"/>
    public double TrackHeight
    {
        get => (double)this.GetValue(TrackHeightProperty);
        set => this.SetValue(TrackHeightProperty, value);
    }

    /// <inheritdoc cref="ThumbSizeProperty"/>
    public double ThumbSize
    {
        get => (double)this.GetValue(ThumbSizeProperty);
        set => this.SetValue(ThumbSizeProperty, value);
    }


    /// <summary>Raised when the user grabs the thumb. Stop writing <see cref="Position"/> until <see cref="DragCompleted"/>.</summary>
    public event EventHandler? DragStarted;

    /// <summary>Raised continuously while dragging, with the position under the finger.</summary>
    public event EventHandler<TimeSpan>? Seeking;

    /// <summary>Raised when the finger lifts (or the user taps the track), with the position to seek to.</summary>
    public event EventHandler<TimeSpan>? DragCompleted;


    void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!this.IsEnabled || this.Duration <= TimeSpan.Zero || this.Width <= 0)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                this.isDragging = true;
                this.dragStartFraction = this.Fraction;
                this.drawable.IsDragging = true;
                this.DragStarted?.Invoke(this, EventArgs.Empty);
                this.Invalidate();
                break;

            case GestureStatus.Running:
                if (!this.isDragging)
                    break;

                // Pan reports a cumulative delta from where the gesture began, so track the fraction we
                // started from rather than the (frozen) Position property.
                var fraction = Math.Clamp(this.dragStartFraction + e.TotalX / this.Width, 0d, 1d);
                this.ApplyFraction(fraction);
                this.Seeking?.Invoke(this, this.Position);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (!this.isDragging)
                    break;

                this.isDragging = false;
                this.drawable.IsDragging = false;
                this.Invalidate();
                this.DragCompleted?.Invoke(this, this.Position);
                break;
        }
    }

    void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!this.IsEnabled || this.Duration <= TimeSpan.Zero || this.Width <= 0)
            return;

        var point = e.GetPosition(this);
        if (point is null)
            return;

        this.ApplyFraction(Math.Clamp(point.Value.X / this.Width, 0d, 1d));
        this.DragCompleted?.Invoke(this, this.Position);
    }

    void ApplyFraction(double fraction)
        => this.Position = TimeSpan.FromTicks((long)(this.Duration.Ticks * fraction));

    double Fraction => this.Duration > TimeSpan.Zero
        ? Math.Clamp(this.Position.Ticks / (double)this.Duration.Ticks, 0d, 1d)
        : 0d;

    void Refresh()
    {
        this.drawable.Fraction = this.Fraction;
        this.drawable.Buffered = Math.Clamp(this.BufferedProgress, 0d, 1d);
        this.drawable.TrackColor = this.TrackColor;
        this.drawable.ProgressColor = this.ProgressColor;
        this.drawable.BufferedColor = this.BufferedColor;
        this.drawable.TrackHeight = (float)this.TrackHeight;
        this.drawable.ThumbSize = (float)this.ThumbSize;
        this.Invalidate();
    }
}


class SeekBarDrawable : IDrawable
{
    public double Fraction { get; set; }
    public double Buffered { get; set; }
    public Color TrackColor { get; set; } = Color.FromRgba(255, 255, 255, 70);
    public Color ProgressColor { get; set; } = Colors.White;
    public Color BufferedColor { get; set; } = Color.FromRgba(255, 255, 255, 130);
    public float TrackHeight { get; set; } = 4;
    public float ThumbSize { get; set; } = 14;
    public bool IsDragging { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var thumb = this.IsDragging ? this.ThumbSize * 1.35f : this.ThumbSize;
        var radius = thumb / 2f;

        // Inset by the thumb radius so the thumb stays fully on-canvas at both extremes.
        var left = dirtyRect.X + radius;
        var width = Math.Max(0f, dirtyRect.Width - thumb);
        var centerY = dirtyRect.Center.Y;
        var top = centerY - this.TrackHeight / 2f;
        var corner = this.TrackHeight / 2f;

        canvas.FillColor = this.TrackColor;
        canvas.FillRoundedRectangle(left, top, width, this.TrackHeight, corner);

        var buffered = (float)(width * this.Buffered);
        if (buffered > 0)
        {
            canvas.FillColor = this.BufferedColor;
            canvas.FillRoundedRectangle(left, top, buffered, this.TrackHeight, corner);
        }

        var played = (float)(width * this.Fraction);
        if (played > 0)
        {
            canvas.FillColor = this.ProgressColor;
            canvas.FillRoundedRectangle(left, top, played, this.TrackHeight, corner);
        }

        canvas.FillColor = this.ProgressColor;
        canvas.FillCircle(left + played, centerY, radius);
    }
}
