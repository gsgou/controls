namespace Shiny.Maui.Controls;

/// <summary>
/// Stock accessory bars, for the cases where writing one out in XAML is noise.
/// </summary>
public enum KeyboardAccessoryPreset
{
    /// <summary>No bar.</summary>
    None,

    /// <summary>A single trailing "Done" that dismisses the keyboard.</summary>
    Done,

    /// <summary>Leading previous/next field arrows.</summary>
    Navigation,

    /// <summary>Previous/next arrows on the left, "Done" on the right.</summary>
    NavigationAndDone
}

/// <summary>
/// Which way a <see cref="KeyboardNavigationItem"/> moves focus.
/// </summary>
public enum KeyboardNavigationDirection
{
    Previous,
    Next
}
