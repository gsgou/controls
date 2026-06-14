using System.Windows.Input;

namespace Shiny.Controls.Camera;

/// <summary>
/// Base class for frame analyzers. Implements <see cref="IFrameAnalyzer"/>, is a <see cref="BindableObject"/>
/// (so its <c>Command</c>s bind in XAML and inherit the <see cref="CameraView"/>'s <see cref="BindableObject.BindingContext"/>),
/// and marshals each analyzer's strongly-typed events/commands onto the UI thread. The camera pipeline injects
/// a dispatcher when the analyzer is attached to a running camera; used standalone (or in tests) events are
/// raised inline.
/// </summary>
public abstract class FrameAnalyzer : BindableObject, IFrameAnalyzer
{
    Action<Action>? dispatcher;

    /// <summary>
    /// Whether this analyzer runs at all. Default <c>true</c>. Set <c>false</c> (e.g. bind it to a switch in
    /// XAML) to turn the analyzer off without removing it from <see cref="CameraView.Analyzers"/> — its
    /// command/event bindings stay wired and its internal state is preserved, so toggling it back on resumes
    /// instantly. A disabled analyzer is skipped by the pipeline and its overlay boxes are cleared. When every
    /// analyzer is disabled the camera behaves as if it had none (e.g. on Android, video recording is allowed).
    /// </summary>
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.Create(
        nameof(IsEnabled), typeof(bool), typeof(FrameAnalyzer), true);

    /// <inheritdoc cref="IsEnabledProperty"/>
    public bool IsEnabled
    {
        get => (bool)this.GetValue(IsEnabledProperty);
        set => this.SetValue(IsEnabledProperty, value);
    }

    /// <summary>
    /// Whether this analyzer's bounding boxes are drawn. Default <c>true</c>. Set <c>false</c> (e.g. in XAML)
    /// to run the analyzer purely for its event/command without drawing any box. Distinct from
    /// <see cref="IsEnabled"/>, which stops the analyzer running entirely.
    /// </summary>
    public static readonly BindableProperty ShowBoundingBoxProperty = BindableProperty.Create(
        nameof(ShowBoundingBox), typeof(bool), typeof(FrameAnalyzer), true);

    /// <inheritdoc cref="ShowBoundingBoxProperty"/>
    public bool ShowBoundingBox
    {
        get => (bool)this.GetValue(ShowBoundingBoxProperty);
        set => this.SetValue(ShowBoundingBoxProperty, value);
    }

    /// <inheritdoc/>
    public abstract string Id { get; }

    /// <inheritdoc/>
    public abstract ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct);

    /// <summary>
    /// Set by the camera pipeline so <see cref="Raise"/>/<see cref="Emit"/> post to the UI thread. Pass
    /// <c>null</c> to detach (then they run inline on the analysis thread).
    /// </summary>
    internal void SetDispatcher(Action<Action>? post) => this.dispatcher = post;

    /// <summary>Run an action on the UI thread when attached to a camera, or inline otherwise.</summary>
    protected void Raise(Action action)
    {
        var d = this.dispatcher;
        if (d is null)
            action();
        else
            d(action);
    }

    /// <summary>
    /// Raise an analyzer's typed event and invoke its bound command (when it can execute) — both on the UI
    /// thread. <paramref name="arg"/> is passed to the command as its parameter.
    /// </summary>
    protected void Emit(Action raiseEvent, ICommand? command, object? arg)
        => this.Raise(() =>
        {
            raiseEvent();
            if (command is not null && command.CanExecute(arg))
                command.Execute(arg);
        });

    /// <summary>
    /// Resolve the boxes to draw for a detection: nothing when <see cref="ShowBoundingBox"/> is off, else the
    /// analyzer's <c>OverlayProvider</c> result when one is supplied (return <c>null</c> from it for no box),
    /// else the analyzer's default boxes.
    /// </summary>
    protected IReadOnlyList<OverlayBox>? ResolveOverlay<TArgs>(
        TArgs args,
        Func<TArgs, IReadOnlyList<OverlayBox>?>? provider,
        Func<IReadOnlyList<OverlayBox>?> defaultBoxes)
    {
        if (!this.ShowBoundingBox)
            return null;
        return provider is not null ? provider(args) : defaultBoxes();
    }
}
