#if SHINY_UIHINGE
using System.Runtime.Versioning;
using CoreGraphics;
using UIKit;

namespace Shiny.Maui.Controls.Foldables;

/// <summary>
/// iPhone Duo. The hinge status comes from a <see cref="UIHingeInteraction"/>; the fold itself is the
/// window's active <c>division</c> reserved region, which only exists while the device is partly open.
/// </summary>
public sealed partial class FoldPostureProvider
{
    UIHingeInteraction? interaction;
    UIView? host;
    RegionProbe? probe;
    UIHingeStatus status;   // default(UIHingeStatus) is Unknown

    [SupportedOSPlatformGuard("ios27.1")]
    [UnsupportedOSPlatformGuard("maccatalyst")]
    static bool PlatformIsSupported() => OperatingSystem.IsIOSVersionAtLeast(27, 1) && !OperatingSystem.IsMacCatalyst();

    /// <summary>Follow the key window. Called whenever the app or a scene activates.</summary>
    internal void Attach()
    {
        if (PlatformIsSupported())
            this.AttachCore();
    }

    [SupportedOSPlatform("ios27.1")]
    [UnsupportedOSPlatform("maccatalyst")]
    void AttachCore()
    {
        var window = KeyWindow();
        if (window is null)
            return;

        // The root view controller's view is the hierarchy UIKit delivers hinge updates to; the
        // window is the fallback before one exists.
        var target = window.RootViewController?.View ?? window;
        if (ReferenceEquals(target, this.host))
        {
            this.Refresh();
            return;
        }

        this.DetachCurrent();
        this.host = target;

        this.interaction = new UIHingeInteraction((_, update) =>
        {
            // Hinge is null when the hierarchy does not provide updates: Unknown, not Closed.
            this.status = update.Hinge?.Status ?? UIHingeStatus.Unknown;
            this.Refresh();
        });
        target.AddInteraction(this.interaction);

        // Reserved regions move with the layout, not only with the hinge: a hidden full-size view
        // whose LayoutSubviews runs on every resize re-queries them.
        this.probe = new RegionProbe(this)
        {
            Frame = window.Bounds,
            AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
            Hidden = true,
            UserInteractionEnabled = false
        };
        window.AddSubview(this.probe);

        this.Refresh();
    }

    [SupportedOSPlatform("ios27.1")]
    [UnsupportedOSPlatform("maccatalyst")]
    void DetachCurrent()
    {
        if (this.interaction is not null)
            this.host?.RemoveInteraction(this.interaction);

        this.probe?.RemoveFromSuperview();
        this.probe?.Dispose();
        this.interaction?.Dispose();
        this.interaction = null;
        this.probe = null;
        this.host = null;
        this.status = default;
    }

    /// <summary>Re-read the division region and hinge status.</summary>
    internal void Refresh()
    {
        if (!PlatformIsSupported() || this.host is not { } view)
            return;

        this.Update(this.Read(view));
    }

    [SupportedOSPlatform("ios27.1")]
    [UnsupportedOSPlatform("maccatalyst")]
    FoldInfo Read(UIView view)
    {
        UIViewReservedRegion? division = null;
        var regions = view.GetReservedRegions(UIViewReservedRegionKind.CreateDivision());
        foreach (var region in regions ?? [])
        {
            if (region.Active)
            {
                division = region;
                break;
            }
        }

        if (division is not null)
        {
            // The region's frame is in the queried view's space; controls want the window's.
            var frame = view.ConvertRectToView(division.Frame, null);
            var margins = division.Margins;
            var orientation = frame.Height >= frame.Width ? FoldPostureOrientation.Vertical : FoldPostureOrientation.Horizontal;

            return new FoldInfo(
                FoldPostureState.HalfOpen,
                orientation,
                new Rect(frame.X, frame.Y, frame.Width, frame.Height),
                new Thickness(margins.Left, margins.Top, margins.Right, margins.Bottom)
            );
        }

        return this.status switch
        {
            // Fully open: a state but nothing dividing the window - no control reacts.
            UIHingeStatus.FullyOpen => new FoldInfo(FoldPostureState.Flat, FoldPostureOrientation.None, null),
            _ => FoldInfo.None
        };
    }

    static UIWindow? KeyWindow()
    {
        var windows = UIApplication.SharedApplication?.ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(scene => scene.Windows)
            .ToList();

        if (windows is null || windows.Count == 0)
            return null;

        return windows.FirstOrDefault(w => w.IsKeyWindow) ?? windows[0];
    }


    sealed class RegionProbe(FoldPostureProvider owner) : UIView(CGRect.Empty)
    {
        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            owner.Refresh();
        }
    }
}
#endif
