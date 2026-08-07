using Shiny.Controls.Keyframe.Graphics;

namespace Shiny.Maui.Controls.Keyframe;

/// <summary>
/// Hosts a <see cref="KeyframeScene"/> in the visual tree, redrawing it each frame while its
/// animation runs.
/// </summary>
/// <remarks>
/// This is the bridge between the two rendering models. <see cref="Animate"/> drives real MAUI
/// views through their properties; this control draws a scene onto a canvas. They share the same
/// timeline model, so a storyboard can sequence both together.
/// </remarks>
public class KeyframeView : GraphicsView
{
    /// <summary>Backing store for <see cref="Scene"/>.</summary>
    public static readonly BindableProperty SceneProperty =
        BindableProperty.Create(nameof(Scene), typeof(KeyframeScene), typeof(KeyframeView),
            propertyChanged: OnSceneChanged);

    /// <summary>Backing store for <see cref="IsPlaying"/>.</summary>
    public static readonly BindableProperty IsPlayingProperty =
        BindableProperty.Create(nameof(IsPlaying), typeof(bool), typeof(KeyframeView), true,
            propertyChanged: OnIsPlayingChanged);

    /// <summary>Backing store for <see cref="Speed"/>.</summary>
    public static readonly BindableProperty SpeedProperty =
        BindableProperty.Create(nameof(Speed), typeof(double), typeof(KeyframeView), 1d,
            propertyChanged: OnSpeedChanged);

    /// <summary>Backing store for <see cref="Progress"/>.</summary>
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(KeyframeView), 0d,
            propertyChanged: OnProgressChanged);

    MauiClock? clock;
    Player? player;
    bool suppressProgressFeedback;

    /// <summary>Creates the view.</summary>
    public KeyframeView()
    {
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
    }

    /// <summary>The scene to draw.</summary>
    public KeyframeScene? Scene
    {
        get => (KeyframeScene?)GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    /// <summary>Whether the scene's animation is advancing.</summary>
    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    /// <summary>Playback rate. Negative values run backwards.</summary>
    public double Speed
    {
        get => (double)GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    /// <summary>
    /// Normalised playback position, 0 to 1. Two-way: it reports progress while playing, and
    /// seeks the scene when written — bind a Slider to it for a scrubber.
    /// </summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>The player driving the scene, or null before the view has loaded.</summary>
    public Player? Player => player;

    static void OnSceneChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (KeyframeView)bindable;
        view.Drawable = newValue as KeyframeScene;
        view.Detach();

        if (view.IsLoaded)
            view.Attach();
    }

    static void OnIsPlayingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (KeyframeView)bindable;

        if (view.player is null)
            return;

        if ((bool)newValue)
            view.player.Resume();
        else
            view.player.Pause();
    }

    static void OnSpeedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is KeyframeView { player: not null } view)
            view.player.Rate = (double)newValue;
    }

    static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (KeyframeView)bindable;

        // Ignore the write we made ourselves while reporting playback position, or a scrubber
        // bound two-way would fight the animation for control of the clock.
        if (view.suppressProgressFeedback || view.player is null)
            return;

        // An endlessly repeating animation has no end to measure progress against. A bound slider
        // should quietly do nothing rather than throw from inside a property-changed callback.
        if (view.Scene?.Animation?.TotalDuration == TimeSpan.MaxValue)
            return;

        view.player.SeekProgress((double)newValue);
        view.Invalidate();
    }

    void Attach()
    {
        var scene = Scene;
        if (scene?.Animation is null || player is not null)
            return;

        clock = MauiClock.For(this);
        clock.Tick += OnTick;

        player = new Player(scene.Animation, clock) { Rate = Speed };

        if (IsPlaying)
            player.Play();
        else
            scene.Animation.CaptureBaselines();

        Invalidate();
    }

    void Detach()
    {
        if (clock is not null)
            clock.Tick -= OnTick;

        // The clock is shared with every other animation on the window, so it is not ours to
        // dispose — dropping our subscription is what releases it.
        player?.Dispose();
        player = null;
        clock = null;
    }

    void OnTick(TimeSpan delta)
    {
        // The player has already advanced the scene by the time this runs; all that is left is to
        // ask the platform to redraw and to publish where we are.
        Invalidate();

        var total = player?.State is PlaybackState.Running ? Scene?.Animation?.TotalDuration : null;

        if (total is null || total == TimeSpan.MaxValue || total == TimeSpan.Zero || player is null)
            return;

        suppressProgressFeedback = true;

        try
        {
            Progress = Math.Clamp(player.Position / total.Value, 0d, 1d);
        }
        finally
        {
            suppressProgressFeedback = false;
        }
    }
}
