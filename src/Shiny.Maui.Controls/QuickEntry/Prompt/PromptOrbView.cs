using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// The small animated mark that sits at the head of <see cref="PromptView"/> — three tinted
/// blobs orbiting inside a soft ring. It idles with a slow drift and speeds up while
/// <see cref="IsBusy"/> is set, which is what carries "I'm working on it" without a spinner.
/// </summary>
/// <remarks>
/// Drawn with <see cref="Microsoft.Maui.Graphics"/> rather than an image so it takes its colour
/// from <see cref="AccentColor"/> and stays crisp at any DPI. The ticker only runs while the view
/// is loaded, so a hidden popup costs nothing.
/// </remarks>
public class PromptOrbView : GraphicsView
{
    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor),
        typeof(Color),
        typeof(PromptOrbView),
        null,
        propertyChanged: (b, _, _) => ((PromptOrbView)b).Redraw()
    );

    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
        nameof(IsBusy),
        typeof(bool),
        typeof(PromptOrbView),
        false,
        propertyChanged: (b, _, _) => ((PromptOrbView)b).Redraw()
    );

    readonly OrbDrawable drawable;
    IDispatcherTimer? timer;

    public PromptOrbView()
    {
        this.drawable = new OrbDrawable(this);
        this.Drawable = this.drawable;
        this.HeightRequest = 26;
        this.WidthRequest = 26;
        this.BackgroundColor = Colors.Transparent;

        // The orb's hue follows the theme's primary unless the consumer assigns one, which is what
        // keeps it in step with the rest of the prompt.
        this.SetDynamicResource(AccentColorProperty, ShinyThemeKeys.Color.Primary);

        this.Loaded += (_, _) => this.StartTicker();
        this.Unloaded += (_, _) => this.StopTicker();
    }

    /// <summary>Base hue for the orb. The other two blobs are derived from it, so one colour restyles the whole mark.</summary>
    public Color? AccentColor
    {
        get => (Color?)this.GetValue(AccentColorProperty);
        set => this.SetValue(AccentColorProperty, value);
    }

    /// <summary>Spin faster and pulse. Drive this from your own request state.</summary>
    public bool IsBusy
    {
        get => (bool)this.GetValue(IsBusyProperty);
        set => this.SetValue(IsBusyProperty, value);
    }

    void Redraw() => this.Invalidate();

    void StartTicker()
    {
        if (this.timer != null)
            return;

        var dispatcher = this.Dispatcher;
        if (dispatcher == null)
            return;

        this.timer = dispatcher.CreateTimer();
        this.timer.Interval = TimeSpan.FromMilliseconds(33);
        this.timer.IsRepeating = true;
        this.timer.Tick += (_, _) =>
        {
            this.drawable.Advance(this.IsBusy ? 0.085 : 0.014);
            this.Invalidate();
        };
        this.timer.Start();
    }

    void StopTicker()
    {
        this.timer?.Stop();
        this.timer = null;
    }

    static readonly Color FallbackAccent = Color.FromArgb("#6D4AFF");

    sealed class OrbDrawable : IDrawable
    {
        readonly PromptOrbView owner;
        double phase;

        public OrbDrawable(PromptOrbView owner) => this.owner = owner;

        public void Advance(double delta) => this.phase = (this.phase + delta) % (Math.PI * 2);

        public void Draw(ICanvas canvas, RectF rect)
        {
            var size = Math.Min(rect.Width, rect.Height);
            if (size <= 0)
                return;

            var cx = rect.Center.X;
            var cy = rect.Center.Y;
            var r = size / 2f;

            // Only before the theme dictionary has resolved; AccentColor is token-backed.
            var accent = this.owner.AccentColor ?? FallbackAccent;
            var hue = accent.GetHue();
            var sat = Math.Clamp(accent.GetSaturation(), 0.45f, 1f);
            var lum = Math.Clamp(accent.GetLuminosity(), 0.45f, 0.75f);

            // Busy makes the blobs breathe as well as spin; the two together read as activity far
            // more clearly than rotation on its own at this size.
            var pulse = this.owner.IsBusy ? 1f + 0.12f * (float)Math.Sin(this.phase * 2.2) : 1f;

            canvas.SaveState();
            canvas.Antialias = true;

            // Soft halo behind everything, so the mark still separates from a light surface.
            canvas.FillColor = Blend(hue, sat, lum, 0.18f);
            canvas.FillCircle(cx, cy, r * 0.98f);

            var blobRadius = r * 0.42f * pulse;
            var orbit = r * 0.32f;
            for (var i = 0; i < 3; i++)
            {
                var angle = this.phase + (i * Math.PI * 2 / 3);
                var bx = cx + (float)(Math.Cos(angle) * orbit);
                var by = cy + (float)(Math.Sin(angle) * orbit);
                var h = Wrap(hue + (i - 1) * 0.075f);
                canvas.FillColor = Blend(h, sat, lum, 0.62f);
                canvas.FillCircle(bx, by, blobRadius);
            }

            var strokeSize = Math.Max(1f, r * 0.09f);
            canvas.StrokeColor = Blend(hue, sat, Math.Clamp(lum + 0.1f, 0f, 1f), 0.55f);
            canvas.StrokeSize = strokeSize;
            canvas.DrawCircle(cx, cy, r - strokeSize / 2f);

            canvas.RestoreState();
        }

        static float Wrap(float hue) => hue < 0 ? hue + 1f : (hue > 1f ? hue - 1f : hue);

        static Color Blend(float h, float s, float l, float alpha) => Color.FromHsla(h, s, l, alpha);
    }
}
