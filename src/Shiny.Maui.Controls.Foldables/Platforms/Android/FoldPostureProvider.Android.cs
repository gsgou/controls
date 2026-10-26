using Android.App;
using Android.Runtime;
using AndroidX.Core.Content;
using AndroidX.Window.Java.Layout;
using AndroidX.Window.Layout;

namespace Shiny.Maui.Controls.Foldables;

// TODO(window 1.6): WindowManager 1.6.0-alpha03 deprecates WindowInfoTrackerCallbackAdapter in favour of
// callbacks on WindowLayoutInfo. No .NET binding of 1.6 exists yet (Xamarin.AndroidX.Window is 1.5.1.3);
// revisit when Xamarin.AndroidX.Window 1.6.x ships. Everything that touches the adapter is in this file.
public sealed partial class FoldPostureProvider
{
    WindowInfoTrackerCallbackAdapter? adapter;
    LayoutConsumer? consumer;
    WeakReference<Activity>? attached;

    static bool PlatformIsSupported() => true;

    /// <summary>Start listening to <paramref name="activity"/>'s window layout. Called on resume.</summary>
    internal void Attach(Activity activity)
    {
        if (this.attached is not null && this.attached.TryGetTarget(out var current) && ReferenceEquals(current, activity))
            return;

        this.DetachCurrent();

        this.adapter = new WindowInfoTrackerCallbackAdapter(IWindowInfoTracker.GetOrCreate(activity));
        this.consumer = new LayoutConsumer(this, activity);
        this.attached = new WeakReference<Activity>(activity);

        // The main executor, so the callback - and therefore every control's reaction - is on the UI thread.
        this.adapter.AddWindowLayoutInfoListener(activity, ContextCompat.GetMainExecutor(activity)!, this.consumer);
    }

    /// <summary>Stop listening, if <paramref name="activity"/> is the one being followed. Called on pause and destroy.</summary>
    internal void Detach(Activity activity)
    {
        if (this.attached is null || !this.attached.TryGetTarget(out var current) || !ReferenceEquals(current, activity))
            return;

        this.DetachCurrent();
    }

    void DetachCurrent()
    {
        if (this.adapter is not null && this.consumer is not null)
            this.adapter.RemoveWindowLayoutInfoListener(this.consumer);

        this.consumer?.Dispose();
        this.adapter?.Dispose();
        this.consumer = null;
        this.adapter = null;
        this.attached = null;
    }

    void OnLayout(Activity activity, WindowLayoutInfo info) => this.Update(Map(activity, info));

    /// <summary>
    /// The first folding feature, in dp. Bounds are window-relative pixels already; only the unit
    /// changes. A feature that does not separate the window (a Pixel Fold laid flat) reports its state
    /// with no rectangle, so no control reacts to it.
    /// </summary>
    static FoldInfo Map(Activity activity, WindowLayoutInfo info)
    {
        IFoldingFeature? fold = null;
        foreach (var feature in info.DisplayFeatures)
        {
            // The list hands back IDisplayFeature invokers, so a plain C# type test never matches a
            // Java FoldingFeature - it has to be cast through JNI, which checks instanceof for us.
            if (AsFolding(feature) is { } folding)
            {
                fold = folding;
                break;
            }
        }

        if (fold is null)
            return FoldInfo.None;

        var state = Equals(fold.State, FoldingFeatureState.HalfOpened)
            ? FoldPostureState.HalfOpen
            : FoldPostureState.Flat;

        var orientation = Equals(fold.Orientation, FoldingFeatureOrientation.Vertical)
            ? FoldPostureOrientation.Vertical
            : FoldPostureOrientation.Horizontal;

        Rect? rect = null;
        if (fold.IsSeparating)
        {
            var density = activity.Resources?.DisplayMetrics?.Density ?? 1f;
            if (density <= 0)
                density = 1f;

            var b = fold.Bounds;
            rect = new Rect(b.Left / density, b.Top / density, b.Width() / density, b.Height() / density);
        }

        return new FoldInfo(state, orientation, rect);
    }


    static IFoldingFeature? AsFolding(IDisplayFeature feature)
    {
        if (feature is IFoldingFeature direct)
            return direct;

        try
        {
            return feature.JavaCast<IFoldingFeature>();
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }


    sealed class LayoutConsumer(FoldPostureProvider owner, Activity activity) : Java.Lang.Object, AndroidX.Core.Util.IConsumer
    {
        readonly WeakReference<Activity> activity = new(activity);

        public void Accept(Java.Lang.Object? value)
        {
            if (value is WindowLayoutInfo info && this.activity.TryGetTarget(out var target))
                owner.OnLayout(target, info);
        }
    }
}
