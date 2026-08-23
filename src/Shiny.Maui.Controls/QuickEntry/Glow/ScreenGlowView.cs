namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// Draws the screen-edge glow. Fills the overlay window; everything but the border band is clipped
/// away, so the middle of the screen stays untouched.
/// </summary>
/// <remarks>
/// <para>
/// The inward falloff comes from stacking <see cref="ScreenGlowOptions.Layers"/> passes, each
/// clipped a little further in than the last. They accumulate towards the screen edge, which is
/// both cheaper and softer than trying to express a two-dimensional gradient — colour along the
/// edge, opacity across it — with a single paint.
/// </para>
/// <para>
/// Three motions run at once, and they do different jobs. The colour cycle is the one you notice —
/// the whole edge working through the palette in place. The pulse breathes the brightness, which is
/// what makes it read as alive rather than as a static coloured border. The travel is a slow drift
/// underneath both; on its own it reads as a chase light, which is not the effect.
/// </para>
/// </remarks>
class ScreenGlowView : GraphicsView
{
    readonly ScreenGlowOptions options;
    readonly GlowDrawable drawable;
    IDispatcherTimer? timer;

    public ScreenGlowView(ScreenGlowOptions options)
    {
        this.options = options;
        this.drawable = new GlowDrawable(options);
        this.Drawable = this.drawable;
        this.BackgroundColor = Colors.Transparent;
        this.InputTransparent = true;

        this.Loaded += (_, _) => this.Start();
        this.Unloaded += (_, _) => this.Stop();
    }

    public void Start()
    {
        if (this.timer != null)
            return;

        var dispatcher = this.Dispatcher;
        if (dispatcher == null)
            return;

        var fps = Math.Clamp(this.options.FrameRate, 5, 120);
        this.timer = dispatcher.CreateTimer();
        this.timer.Interval = TimeSpan.FromMilliseconds(1000d / fps);
        this.timer.IsRepeating = true;
        this.timer.Tick += (_, _) =>
        {
            this.drawable.Advance(1d / fps);
            this.Invalidate();
        };
        this.timer.Start();
    }

    public void Stop()
    {
        this.timer?.Stop();
        this.timer = null;
    }

    sealed class GlowDrawable : IDrawable
    {
        readonly ScreenGlowOptions options;
        double phase;
        double colorPhase;
        double pulsePhase;

        public GlowDrawable(ScreenGlowOptions options) => this.options = options;

        /// <summary><paramref name="seconds"/> of elapsed time, so each motion runs at its own configured rate.</summary>
        public void Advance(double seconds)
        {
            this.phase += this.options.Speed * seconds;

            if (this.options.ColorCycleSeconds > 0)
                this.colorPhase += seconds / this.options.ColorCycleSeconds;

            if (this.options.PulseSeconds > 0)
                this.pulsePhase += seconds / this.options.PulseSeconds;
        }

        public void Draw(ICanvas canvas, RectF rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            var layers = Math.Clamp(this.options.Layers, 1, 6);
            var thickness = (float)Math.Max(1d, this.options.Thickness);
            var blobs = ScreenGlowGeometry.Compute(rect.Width, rect.Height, thickness, this.phase, this.colorPhase, this.options);

            // The breath. Depth 0 (or PulseSeconds 0) holds it steady at full Intensity.
            var depth = Math.Clamp(this.options.PulseDepth, 0d, 1d);
            var breath = this.options.PulseSeconds <= 0 || depth <= 0
                ? 1d
                : 1d - depth * (0.5d - 0.5d * Math.Cos(this.pulsePhase * Math.PI * 2));

            var alpha = (float)(Math.Clamp(this.options.Intensity, 0d, 1d) * breath) * 0.45f;

            canvas.Antialias = true;
            for (var layer = 0; layer < layers; layer++)
            {
                var inset = thickness * (layer + 1) / layers;
                var innerWidth = rect.Width - inset * 2;
                var innerHeight = rect.Height - inset * 2;
                if (innerWidth <= 0 || innerHeight <= 0)
                    continue;

                canvas.SaveState();
                canvas.SubtractFromClip(inset, inset, innerWidth, innerHeight);

                foreach (var blob in blobs)
                {
                    var bounds = new RectF(blob.X - blob.Radius, blob.Y - blob.Radius, blob.Radius * 2, blob.Radius * 2);
                    var colour = blob.Color.WithAlpha(alpha);
                    var paint = new RadialGradientPaint(
                        new PaintGradientStop[]
                        {
                            new(0f, colour),
                            new(0.55f, colour.WithAlpha(alpha * 0.45f)),
                            new(1f, colour.WithAlpha(0f))
                        })
                    {
                        Center = new Point(0.5, 0.5),
                        Radius = 0.5
                    };

                    canvas.SetFillPaint(paint, bounds);
                    canvas.FillRectangle(bounds);
                }

                canvas.RestoreState();
            }
        }
    }
}
