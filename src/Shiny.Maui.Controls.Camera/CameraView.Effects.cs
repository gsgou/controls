using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Shiny.Maui.Controls.Camera;

public partial class CameraView
{
    /// <summary>
    /// The ordered effects applied to the preview, to captured stills and to recorded video — colour looks,
    /// spatial (GPU) looks, compositing overlays, and post-capture transforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The collection is live: add, remove or reorder while the camera is running and the change takes effect
    /// on the next frame. <see cref="Filter"/> composes with it and is always applied <b>first</b>, so setting
    /// both is well-defined rather than dependent on assignment order.
    /// </para>
    /// <para>
    /// Coverage varies by platform — ask <see cref="GetEffectSupport"/> before offering an effect in a UI.
    /// </para>
    /// <code>
    /// // from code
    /// camera.Effects.Add(CameraEffects.Mono);
    ///
    /// // or in XAML — built-ins are static, custom effects are ordinary objects
    /// &lt;cam:CameraView&gt;
    ///     &lt;cam:CameraView.Effects&gt;
    ///         &lt;x:Static Member="cam:CameraEffects.Mono" /&gt;
    ///     &lt;/cam:CameraView.Effects&gt;
    /// &lt;/cam:CameraView&gt;
    /// </code>
    /// </remarks>
    public static readonly BindableProperty EffectsProperty = BindableProperty.Create(
        nameof(Effects), typeof(IList<ICameraEffect>), typeof(CameraView),
        defaultValueCreator: _ => new ObservableCollection<ICameraEffect>(),
        propertyChanged: OnEffectsChanged);

    /// <inheritdoc cref="EffectsProperty"/>
    public IList<ICameraEffect> Effects
    {
        get => (IList<ICameraEffect>)this.GetValue(EffectsProperty);
        set => this.SetValue(EffectsProperty, value);
    }

    /// <summary>
    /// The current effects resolved into an immutable, ordered snapshot — what the platform handlers actually
    /// render against. Rebuilt on the UI thread whenever <see cref="Filter"/> or <see cref="Effects"/> changes,
    /// so a capture or encoder thread can read one consistent set of effects per frame without locking.
    /// </summary>
    public CameraEffectChain EffectChain { get; private set; } = CameraEffectChain.Empty;

    /// <summary>
    /// The active analyzer's most recent ungated result — what it currently sees, updated every frame — or
    /// <c>null</c> when there is no analyzer or it publishes none. Handed to <see cref="IDrawEffect"/>s as
    /// <c>CameraEffectContext.AnalyzerResult</c> so they can anchor to it.
    /// </summary>
    public object? LastAnalyzerResult => (this.Analyzer as FrameAnalyzer)?.LiveResult;

    /// <summary>
    /// How much of <paramref name="effect"/> the <b>current platform</b> will actually honour.
    /// </summary>
    /// <remarks>
    /// Coverage is genuinely uneven — Windows has no live preview filter, Android needs API 33+ for spatial
    /// shaders, WebKit is unreliable with SVG filters on video. This reports that honestly so an app can grey
    /// out a control rather than offer an effect that silently does nothing.
    /// </remarks>
    public static EffectSupport GetEffectSupport(ICameraEffect effect) => CameraEffectSupport.Resolve(effect);

    static void OnEffectsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (CameraView)bindable;
        view.TrackEffects(oldValue, newValue);
        view.RebuildEffectChain();
    }

    /// <summary>
    /// Move the <c>CollectionChanged</c> subscription from one <see cref="Effects"/> collection to another, so
    /// mutating the live one rebuilds the chain.
    /// </summary>
    /// <remarks>
    /// Called from <see cref="OnEffectsChanged"/> when the collection is replaced, and — critically — from the
    /// constructor for the <i>default</i> collection. A <c>BindableProperty</c>'s <c>defaultValueCreator</c>
    /// materializes lazily on first read and does <b>not</b> raise <c>propertyChanged</c>, so the default
    /// collection would otherwise never be subscribed: <c>Effects.Add(...)</c> would leave
    /// <see cref="EffectChain"/> empty and every effect added that way would silently do nothing, while
    /// <see cref="Filter"/> — a real property change — kept working. That asymmetry is exactly what it looked
    /// like from the outside: colour filters fine, comic/sketch/posterize/pixelate dead.
    /// </remarks>
    void TrackEffects(object? oldValue, object? newValue)
    {
        if (oldValue is INotifyCollectionChanged oldObservable)
            oldObservable.CollectionChanged -= this.OnEffectsCollectionChanged;

        if (newValue is INotifyCollectionChanged newObservable)
        {
            // defensive: never end up double-subscribed if this is called twice for the same instance
            newObservable.CollectionChanged -= this.OnEffectsCollectionChanged;
            newObservable.CollectionChanged += this.OnEffectsCollectionChanged;
        }
    }

    // Materialize the default collection and subscribe to it now, rather than waiting for a property change
    // that never comes. Called from the constructor.
    private protected void InitializeEffects() => this.TrackEffects(null, this.Effects);

    void OnEffectsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => this.RebuildEffectChain();

    /// <summary>
    /// Re-snapshot the chain and push it to the handler. Called automatically when <see cref="Filter"/> or
    /// <see cref="Effects"/> changes; call it yourself after mutating state <i>inside</i> an effect (e.g.
    /// toggling its <c>IsEnabled</c>), which the collection cannot observe.
    /// </summary>
    public void RebuildEffectChain()
    {
        this.EffectChain = CameraEffectChain.Create(this.Filter, this.Effects);
        this.Handler?.UpdateValue(nameof(Effects));
    }
}
