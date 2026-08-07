using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Keyframe.Graphics;

/// <summary>
/// A node in a keyframed scene. Every property here is an ordinary get/set, which is the point:
/// tracks drive them exactly like they drive properties on a MAUI view, so the same timeline model
/// serves both the control layer and the drawing layer.
/// </summary>
public abstract class SceneLayer
{
    /// <summary>Optional identifier, useful for locating a layer after importing a scene.</summary>
    public string? Id { get; set; }

    /// <summary>Whether the layer and its children are drawn at all.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Opacity, 0 to 1. Multiplies with every ancestor's opacity.</summary>
    public float Opacity { get; set; } = 1f;

    /// <summary>Offset from the parent's origin, in scene units.</summary>
    public PointF Position { get; set; }

    /// <summary>
    /// The point that rotation and scale pivot around, expressed as a fraction of <see cref="Size"/>.
    /// (0.5, 0.5) is the centre — the default, because rotating about a corner is almost never what
    /// anyone means.
    /// </summary>
    public PointF Anchor { get; set; } = new(0.5f, 0.5f);

    /// <summary>Rotation in degrees, clockwise.</summary>
    public float Rotation { get; set; }

    /// <summary>Scale factors about <see cref="Anchor"/>.</summary>
    public SizeF Scale { get; set; } = new(1f, 1f);

    /// <summary>Skew in degrees about <see cref="Anchor"/>.</summary>
    public SizeF Skew { get; set; }

    /// <summary>The layer's untransformed extent, used to resolve <see cref="Anchor"/>.</summary>
    public SizeF Size { get; set; }

    /// <summary>Draws the layer, applying its transform and opacity first.</summary>
    public void Draw(ICanvas canvas) => Draw(canvas, 1f);

    /// <summary>Draws the layer beneath an ancestor's accumulated opacity.</summary>
    /// <param name="canvas">The surface to draw to.</param>
    /// <param name="inheritedOpacity">Combined opacity of every ancestor, 0 to 1.</param>
    /// <remarks>
    /// <see cref="ICanvas.Alpha"/> is write-only, so the effective opacity cannot be read back off
    /// the canvas and multiplied in place. It is threaded down the tree instead, which also means a
    /// fully transparent subtree can be skipped before any of its children are visited.
    /// </remarks>
    public void Draw(ICanvas canvas, float inheritedOpacity)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (!IsVisible || Opacity <= 0f || inheritedOpacity <= 0f)
            return;

        // Opacity composes multiplicatively down the tree, matching how nested groups behave in
        // every design tool.
        var effective = Math.Clamp(inheritedOpacity * Opacity, 0f, 1f);

        canvas.SaveState();

        try
        {
            canvas.Alpha = effective;
            ApplyTransform(canvas);
            OnDraw(canvas, effective);
        }
        finally
        {
            canvas.RestoreState();
        }
    }

    /// <summary>Draws the layer's own content, with the transform and opacity already applied.</summary>
    /// <param name="canvas">The surface to draw to.</param>
    /// <param name="effectiveOpacity">This layer's opacity combined with its ancestors'.
    /// Container layers must pass it on to their children.</param>
    protected abstract void OnDraw(ICanvas canvas, float effectiveOpacity);

    void ApplyTransform(ICanvas canvas)
    {
        canvas.Translate(Position.X, Position.Y);

        var hasRotation = Rotation != 0f;
        var hasScale = Scale.Width != 1f || Scale.Height != 1f;
        var hasSkew = Skew.Width != 0f || Skew.Height != 0f;

        if (!hasRotation && !hasScale && !hasSkew)
            return;

        // Move the pivot to the origin, transform, then move it back. Doing this once for all
        // three operations keeps them commuting the way authors expect.
        var pivotX = Anchor.X * Size.Width;
        var pivotY = Anchor.Y * Size.Height;

        canvas.Translate(pivotX, pivotY);

        if (hasRotation)
            canvas.Rotate(Rotation);

        if (hasSkew)
        {
            var skewX = MathF.Tan(Skew.Width * MathF.PI / 180f);
            var skewY = MathF.Tan(Skew.Height * MathF.PI / 180f);
            canvas.ConcatenateTransform(new System.Numerics.Matrix3x2(1f, skewY, skewX, 1f, 0f, 0f));
        }

        if (hasScale)
            canvas.Scale(Scale.Width, Scale.Height);

        canvas.Translate(-pivotX, -pivotY);
    }
}

/// <summary>A layer that groups children under a shared transform and opacity.</summary>
public class GroupLayer : SceneLayer
{
    readonly List<SceneLayer> children = [];

    /// <summary>The child layers, drawn in order.</summary>
    public IReadOnlyList<SceneLayer> Children => children;

    /// <summary>Adds a child and returns it, so it can be captured inline.</summary>
    public T Add<T>(T layer) where T : SceneLayer
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (ReferenceEquals(layer, this))
            throw new ArgumentException("A group cannot contain itself.", nameof(layer));

        children.Add(layer);
        return layer;
    }

    /// <summary>Removes a child.</summary>
    public bool Remove(SceneLayer layer) => children.Remove(layer);

    /// <summary>Removes every child.</summary>
    public void Clear() => children.Clear();

    /// <summary>Finds a layer by <see cref="SceneLayer.Id"/>, searching the whole subtree.</summary>
    public SceneLayer? FindById(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        foreach (var child in children)
        {
            if (child.Id == id)
                return child;

            if (child is GroupLayer group && group.FindById(id) is { } found)
                return found;
        }

        return null;
    }

    /// <summary>Enumerates this layer and every descendant, depth first.</summary>
    public IEnumerable<SceneLayer> Descendants()
    {
        foreach (var child in children)
        {
            yield return child;

            if (child is GroupLayer group)
            {
                foreach (var nested in group.Descendants())
                    yield return nested;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnDraw(ICanvas canvas, float effectiveOpacity)
    {
        foreach (var child in children)
            child.Draw(canvas, effectiveOpacity);
    }
}
