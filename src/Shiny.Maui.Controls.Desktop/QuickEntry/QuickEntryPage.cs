using Shiny.Maui.Controls.QuickEntry;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// The page hosted inside the popup window. Deliberately transparent so the native window's
/// transparency reaches all the way through and the content's rounded corners are the only thing
/// the user sees — a page with the default background would paint an opaque rectangle behind them.
/// </summary>
/// <remarks>
/// The root deliberately just fills the window. An earlier attempt gave it a fixed, generous height
/// so the content would have room to arrange at its true size — which works on paper and puts the
/// content off-screen on AppKit, where the extra slack sits above the visible area rather than
/// below it. Sizing is settled by asking the content instead: see <see cref="IQuickEntryAutoSize"/>.
/// </remarks>
sealed class QuickEntryPage : ContentPage
{
    readonly VerticalStackLayout root;
    readonly Action<double> onContentHeightChanged;
    View? hosted;
    bool autoSize;
    double lastReportedHeight = -1d;
    bool measurePending;

    public QuickEntryPage(View hosted, Action<double> onContentHeightChanged)
    {
        this.onContentHeightChanged = onContentHeightChanged;
        this.root = new VerticalStackLayout { BackgroundColor = Colors.Transparent };

        this.BackgroundColor = Colors.Transparent;
        this.Padding = 0;
        this.Content = this.root;
        this.SetHosted(hosted);
    }

    public View? Hosted => this.hosted;

    /// <summary>
    /// Swaps the hosted content, keeping the window and page alive. Rebuilding the whole window per
    /// open is what makes a spotlight popup feel sluggish, so <c>RecreateContentOnShow</c> replaces
    /// only this.
    /// </summary>
    public void SetHosted(View view)
    {
        if (this.hosted != null)
        {
            this.Unwatch(this.hosted);
            this.root.Children.Remove(this.hosted);
        }

        this.hosted = view;
        this.lastReportedHeight = -1d;

        view.VerticalOptions = LayoutOptions.Start;
        view.HorizontalOptions = LayoutOptions.Fill;
        this.root.Children.Add(view);

        if (this.autoSize)
            this.Watch(view);
    }

    /// <summary>
    /// Releases the hosted content so it can be parented somewhere else. Switching presentation mid-run
    /// hands the same view to another presenter, and MAUI refuses to add a view that still has a
    /// parent — it warns and leaves the popup empty rather than throwing, which is worse.
    /// </summary>
    public void ClearHosted()
    {
        if (this.hosted == null)
            return;

        this.Unwatch(this.hosted);
        this.root.Children.Remove(this.hosted);
        this.lastReportedHeight = -1d;
    }

    /// <summary>Starts (or stops) following the content's height so the window can track it.</summary>
    public void SetAutoSize(bool enabled)
    {
        this.autoSize = enabled;
        if (this.hosted == null)
            return;

        this.Unwatch(this.hosted);
        if (enabled)
            this.Watch(this.hosted);
    }

    /// <summary>
    /// <see cref="VisualElement.MeasureInvalidated"/> alone is not enough. The first invalidation
    /// arrives before the content's platform views exist, when an unrealised <c>Entry</c> reports a
    /// desired size nothing like its real one — so the window was sized from that first bad number
    /// and, with nothing invalidating measure again, stayed there. <c>SizeChanged</c> and
    /// <c>Loaded</c> are the signals that the real sizes have landed.
    /// </summary>
    void Watch(View view)
    {
        view.MeasureInvalidated += this.OnLayoutChanged;
        view.SizeChanged += this.OnLayoutChanged;
        view.Loaded += this.OnLayoutChanged;
        if (view is IQuickEntryAutoSize sizer)
            sizer.DesiredHeightChanged += this.OnLayoutChanged;
    }

    void Unwatch(View view)
    {
        view.MeasureInvalidated -= this.OnLayoutChanged;
        view.SizeChanged -= this.OnLayoutChanged;
        view.Loaded -= this.OnLayoutChanged;
        if (view is IQuickEntryAutoSize sizer)
            sizer.DesiredHeightChanged -= this.OnLayoutChanged;
    }

    void OnLayoutChanged(object? sender, EventArgs e)
    {
        if (this.measurePending)
            return;

        this.measurePending = true;
        this.Dispatcher.Dispatch(() =>
        {
            this.measurePending = false;
            this.ReportHeight();
        });
    }

    void ReportHeight()
    {
        if (this.hosted == null)
            return;

        // Content that knows its own height is asked; everything else is measured, with all the
        // caveats on IQuickEntryAutoSize.
        var height = this.hosted is IQuickEntryAutoSize sizer
            ? sizer.GetDesiredHeight(this.ContentWidth())
            : this.MeasuredHeight();

        if (height <= 0 || Math.Abs(height - this.lastReportedHeight) < 0.5)
            return;

        this.lastReportedHeight = height;
        this.onContentHeightChanged(height);
    }

    double ContentWidth()
        => this.hosted == null
            ? 0
            : this.hosted.WidthRequest > 0
                ? this.hosted.WidthRequest
                : (this.Width > 0 ? this.Width : 0);

    double MeasuredHeight()
    {
        var width = this.ContentWidth();
        return width <= 0 || this.hosted == null ? 0 : ((IView)this.hosted).Measure(width, double.PositiveInfinity).Height;
    }
}
