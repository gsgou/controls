namespace Shiny.Maui.Controls;

/// <summary>
/// What a <see cref="ShinyButton"/> is currently doing.
/// </summary>
/// <remarks>
/// A button that kicks off work has four things to say and only one place to say them, so the state
/// lives on the button rather than being assembled at each call site out of an
/// <c>ActivityIndicator</c>, a swapped label and a temporary icon. Success and Error are transient
/// by default — see <see cref="ShinyButton.StateRevertDelay"/>.
/// </remarks>
public enum ButtonState
{
    /// <summary>Idle and ready.</summary>
    Normal,

    /// <summary>Working. Shows the busy indicator and, by default, stops accepting taps.</summary>
    Busy,

    /// <summary>The work succeeded.</summary>
    Success,

    /// <summary>The work failed.</summary>
    Error
}


/// <summary>
/// How much of a <see cref="ShinyButton"/> the surface paints — the Material 3 button family.
/// </summary>
/// <remarks>
/// Appearance is the <em>emphasis</em>; <see cref="ButtonType"/> is the <em>meaning</em>. Keeping the
/// two orthogonal is what lets a destructive action be loud (filled critical) or quiet (text
/// critical) without a second enum member for every combination.
/// </remarks>
public enum ButtonAppearance
{
    /// <summary>Solid fill in the type colour. The highest-emphasis action on a screen.</summary>
    Filled,

    /// <summary>The type's container tint — filled's quieter sibling.</summary>
    Tonal,

    /// <summary>Transparent with an outline. For secondary actions that still need a boundary.</summary>
    Outlined,

    /// <summary>Text only, no fill and no outline. The lowest emphasis.</summary>
    Text,

    /// <summary>A raised surface. Filled's emphasis where a fill would fight the background.</summary>
    Elevated
}


/// <summary>
/// What a <see cref="ShinyButton"/> means, mapped onto the theme's semantic colour families.
/// </summary>
public enum ButtonType
{
    /// <summary>The primary action.</summary>
    Primary,

    /// <summary>A supporting action.</summary>
    Secondary,

    /// <summary>A constructive or confirming action.</summary>
    Success,

    /// <summary>An action that needs a second thought.</summary>
    Warning,

    /// <summary>A destructive action.</summary>
    Critical,

    /// <summary>An informational action.</summary>
    Info
}


/// <summary>
/// What <see cref="ButtonState.Busy"/> does to a <see cref="ShinyButton"/>'s content.
/// </summary>
public enum ButtonBusyMode
{
    /// <summary>
    /// The busy indicator takes the left icon's place and the text stays put. The default, because
    /// the indicator and the icon are both <see cref="ShinyButton.IconSize"/> square, so the button
    /// cannot change size and a row of buttons cannot reflow.
    /// </summary>
    ReplaceLeftIcon,

    /// <summary>
    /// The content fades out and a centred indicator takes over. The content keeps its layout space
    /// while hidden, so the button holds the width it had before it went busy.
    /// </summary>
    ReplaceContent,

    /// <summary>The indicator appears after the right icon and nothing else moves.</summary>
    KeepContent
}


/// <summary>
/// Where a <see cref="ShinyButton"/>'s icons sit relative to its text.
/// </summary>
/// <remarks>
/// Named for the position of the <em>icons</em>, not the text — <see cref="Sides"/> is the ordinary
/// case and the two stacked options exist for tile-style buttons.
/// </remarks>
public enum ButtonContentLayout
{
    /// <summary>Left icon, text, right icon, in a row.</summary>
    Sides,

    /// <summary>Both icons above the text.</summary>
    Top,

    /// <summary>Both icons below the text.</summary>
    Bottom
}
