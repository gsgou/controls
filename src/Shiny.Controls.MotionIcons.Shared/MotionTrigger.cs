namespace Shiny.Controls.MotionIcons;

/// <summary>
/// What starts an icon animating. Combinable — an icon can loop <em>and</em> respond to a tap.
/// </summary>
[Flags]
public enum MotionTrigger
{
    /// <summary>Nothing automatic. The icon animates only when code calls Play.</summary>
    Manual = 0,

    /// <summary>Runs continuously, resting between cycles for the configured interval.</summary>
    Loop = 1,

    /// <summary>
    /// Runs while the pointer is over the icon, finishing the cycle in progress after it leaves so
    /// the icon never snaps back mid-pose.
    /// </summary>
    Hover = 2,

    /// <summary>Runs once per tap, click or press.</summary>
    Press = 4,

    /// <summary>Runs once when the icon first becomes visible.</summary>
    Appear = 8
}
