using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Graphics;

/// <summary>How a scene's design size is mapped onto the canvas it is drawn into.</summary>
public enum SceneStretch
{
    /// <summary>Draw at scene units, ignoring the canvas size. Content may overflow.</summary>
    None,

    /// <summary>Scale uniformly so the whole scene fits, letterboxing as needed.</summary>
    Uniform,

    /// <summary>Scale uniformly so the scene covers the canvas, cropping the overflow.</summary>
    UniformToFill,

    /// <summary>Scale each axis independently to exactly fill the canvas. Distorts.</summary>
    Fill
}

/// <summary>
/// A keyframed scene: a layer tree, the animation that drives it, and an
/// <see cref="IDrawable"/> surface to render it through.
/// </summary>
/// <remarks>
/// This is the drawing counterpart to animating MAUI views. Host it in a <c>GraphicsView</c>, an
/// <c>SKCanvasView</c>, or hand it to the export pipeline — the scene neither knows nor cares which,
/// because it only ever writes to an <see cref="ICanvas"/>.
/// </remarks>
public sealed class KeyframeScene : IDrawable
{
    /// <summary>Creates a scene with the given design size.</summary>
    /// <param name="width">Design width in scene units.</param>
    /// <param name="height">Design height in scene units.</param>
    public KeyframeScene(float width, float height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0f);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0f);

        DesignSize = new SizeF(width, height);
        Root = new GroupLayer { Size = DesignSize };
    }

    /// <summary>The coordinate space the layers are authored in.</summary>
    public SizeF DesignSize { get; }

    /// <summary>The layer tree.</summary>
    public GroupLayer Root { get; }

    /// <summary>The animation driving the layers. Null renders a static scene.</summary>
    public IAnimationNode? Animation { get; set; }

    /// <summary>How the design size maps onto the canvas.</summary>
    public SceneStretch Stretch { get; set; } = SceneStretch.Uniform;

    /// <summary>Painted behind the layers. Null leaves the canvas untouched.</summary>
    public Color? Background { get; set; }

    /// <summary>Whether content outside the design bounds is clipped away.</summary>
    public bool ClipToBounds { get; set; } = true;

    /// <summary>Convenience wrapper for adding a layer to <see cref="Root"/>.</summary>
    public T Add<T>(T layer) where T : SceneLayer => Root.Add(layer);

    /// <summary>Finds a layer anywhere in the tree by its identifier.</summary>
    public SceneLayer? FindById(string id) => Root.FindById(id);

    /// <summary>
    /// Positions the animation at an absolute offset, updating every layer it drives.
    /// </summary>
    /// <param name="time">Offset from the animation's start.</param>
    /// <returns>True once the animation has run past its end.</returns>
    public bool Seek(TimeSpan time) => Animation?.Evaluate(time) ?? true;

    /// <summary>
    /// Positions the animation by normalised progress across its whole duration. This is what the
    /// export pipeline steps through, and what a scrubber binds to.
    /// </summary>
    public bool SeekProgress(double progress)
    {
        if (Animation is null)
            return true;

        var total = Animation.TotalDuration;
        if (total == TimeSpan.MaxValue)
            throw new InvalidOperationException(
                "Cannot seek by progress into an infinitely repeating animation. Give it a finite " +
                "iteration count, or seek by absolute time.");

        return Animation.Evaluate(total * Math.Clamp(progress, 0d, 1d));
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        canvas.SaveState();

        try
        {
            if (Background is not null)
            {
                canvas.FillColor = Background;
                canvas.FillRectangle(dirtyRect);
            }

            if (ClipToBounds)
                canvas.ClipRectangle(dirtyRect);

            ApplyStretch(canvas, dirtyRect);
            Root.Draw(canvas);
        }
        finally
        {
            canvas.RestoreState();
        }
    }

    void ApplyStretch(ICanvas canvas, RectF dirtyRect)
    {
        if (Stretch is SceneStretch.None)
        {
            canvas.Translate(dirtyRect.X, dirtyRect.Y);
            return;
        }

        var scaleX = dirtyRect.Width / DesignSize.Width;
        var scaleY = dirtyRect.Height / DesignSize.Height;

        switch (Stretch)
        {
            case SceneStretch.Fill:
                canvas.Translate(dirtyRect.X, dirtyRect.Y);
                canvas.Scale(scaleX, scaleY);
                return;

            case SceneStretch.Uniform:
                scaleX = scaleY = Math.Min(scaleX, scaleY);
                break;

            case SceneStretch.UniformToFill:
                scaleX = scaleY = Math.Max(scaleX, scaleY);
                break;
        }

        // Centre whatever is left over, so letterboxing and cropping are both symmetric.
        var offsetX = dirtyRect.X + (dirtyRect.Width - DesignSize.Width * scaleX) / 2f;
        var offsetY = dirtyRect.Y + (dirtyRect.Height - DesignSize.Height * scaleY) / 2f;

        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scaleX, scaleY);
    }
}
