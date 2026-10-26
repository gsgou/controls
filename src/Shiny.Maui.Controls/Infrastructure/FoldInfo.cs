namespace Shiny.Maui.Controls;

/// <summary>Whether a fold or hinge is dividing the window, and how far the device is open.</summary>
/// <remarks>
/// Named <c>FoldPosture…</c> rather than <c>FoldState</c> on purpose: Shiny.Core has a
/// <c>Shiny.FoldState</c>, and an app that imports both namespaces to bridge the two would otherwise
/// get an ambiguous reference on the one line it writes.
/// </remarks>
public enum FoldPostureState
{
    /// <summary>No fold, the device is closed (outer screen), or nothing is reporting posture.</summary>
    None,

    /// <summary>Open flat. A fold can still divide the window on devices with a physical hinge.</summary>
    Flat,

    /// <summary>Partly open — book or tabletop.</summary>
    HalfOpen
}


/// <summary>Which way the fold runs across the window.</summary>
public enum FoldPostureOrientation
{
    None,

    /// <summary>A fold running top to bottom — book posture. Panes sit left and right of it.</summary>
    Vertical,

    /// <summary>A fold running side to side — tabletop posture. Panes sit above and below it.</summary>
    Horizontal
}


/// <summary>
/// What the controls need to know about a fold: whether one divides the window and where it is.
/// </summary>
/// <param name="State">How far the device is open.</param>
/// <param name="Orientation">Which way the fold runs.</param>
/// <param name="Fold">
/// The fold or hinge in <b>window</b> coordinates, device-independent units (dp / pt). Null unless
/// the fold actually divides the window — a Pixel Fold laid flat, or an iPhone Duo fully open, report
/// a state but no rectangle, and every control then behaves exactly as it does on a phone. A fold
/// with no physical width (most Android foldables) is a zero-width rectangle.
/// </param>
/// <param name="HingeMargin">
/// Extra space the platform asks content to keep clear of either side of the fold (the iPhone Duo's
/// division region carries margins). Added to <paramref name="Fold"/> to give <see cref="Avoid"/>.
/// Zero when the platform reports none, in which case a zero-width fold gets no gutter at all.
/// </param>
public sealed record FoldInfo(
    FoldPostureState State,
    FoldPostureOrientation Orientation,
    Rect? Fold,
    Thickness HingeMargin = default)
{
    /// <summary>No fold. What every control sees when no <see cref="IFoldPostureSource"/> is registered.</summary>
    public static FoldInfo None { get; } = new(FoldPostureState.None, FoldPostureOrientation.None, null);

    /// <summary>A fold divides the window.</summary>
    public bool IsSeparating => this.Fold is not null;

    /// <summary>Partly open with a fold running top to bottom.</summary>
    public bool IsBook => this.State == FoldPostureState.HalfOpen && this.Orientation == FoldPostureOrientation.Vertical && this.IsSeparating;

    /// <summary>Partly open with a fold running side to side.</summary>
    public bool IsTabletop => this.State == FoldPostureState.HalfOpen && this.Orientation == FoldPostureOrientation.Horizontal && this.IsSeparating;

    /// <summary>The region content should keep out of: <see cref="Fold"/> grown by <see cref="HingeMargin"/>.</summary>
    public Rect? Avoid => this.Fold is { } fold
        ? new Rect(
            fold.X - this.HingeMargin.Left,
            fold.Y - this.HingeMargin.Top,
            fold.Width + this.HingeMargin.HorizontalThickness,
            fold.Height + this.HingeMargin.VerticalThickness)
        : null;
}


/// <summary>
/// Where the controls get the device posture from. Register one with
/// <c>ShinyControlConfiguration.UseFoldPosture</c>, or add the <c>Shiny.Maui.Controls.Foldables</c>
/// package and call <c>UseShinyFoldables()</c>, which registers the built-in Android and iOS sources.
/// </summary>
/// <remarks>
/// <para>With nothing registered the controls see <see cref="FoldInfo.None"/> and behave exactly as
/// they always have — registering a source is the opt-in.</para>
/// <para><see cref="Changed"/> may be raised on any thread; the controls marshal to the UI thread
/// themselves.</para>
/// </remarks>
public interface IFoldPostureSource
{
    /// <summary>The posture right now.</summary>
    FoldInfo Current { get; }

    /// <summary>Raised whenever <see cref="Current"/> changes.</summary>
    event EventHandler? Changed;
}


/// <summary>What a two-pane control does when a fold divides it.</summary>
public enum FoldBehavior
{
    /// <summary>Ignore the fold. The panes keep their sizes and one may sit across the hinge.</summary>
    None,

    /// <summary>
    /// Line the split up with the fold so neither pane draws under the hinge. Display-only: the
    /// control's own width or ratio property keeps the user's value and comes back when the device is
    /// opened flat. The default.
    /// </summary>
    SnapToFold
}
