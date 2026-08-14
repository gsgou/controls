namespace Shiny.Maui.Controls;

/// <summary>Which step the walkthrough just moved to.</summary>
public class WalkthroughStepEventArgs(WalkthroughStep step, int index, int count) : EventArgs
{
    public WalkthroughStep Step { get; } = step;

    /// <summary>Zero-based position among the <em>visible</em> steps.</summary>
    public int Index { get; } = index;

    /// <summary>How many visible steps there are.</summary>
    public int Count { get; } = count;

    /// <summary>One-based position, for "step 2 of 5" captions.</summary>
    public int Number => this.Index + 1;
}


/// <summary>How a walkthrough run ended.</summary>
public class WalkthroughEndedEventArgs(WalkthroughEndReason reason) : EventArgs
{
    public WalkthroughEndReason Reason { get; } = reason;

    /// <summary>Whether the user saw it through, as opposed to skipping or being interrupted.</summary>
    public bool Completed => this.Reason == WalkthroughEndReason.Completed;
}
