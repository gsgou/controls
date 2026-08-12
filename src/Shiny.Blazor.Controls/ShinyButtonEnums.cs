namespace Shiny.Blazor.Controls;

/// <summary>
/// What a <see cref="ShinyButton"/> is currently doing.
/// </summary>
/// <remarks>
/// Mirrors the MAUI <c>ButtonState</c> exactly. The two are declared separately rather than shared,
/// for the same reason <see cref="PillType"/> is: a host-neutral package for five enums would put a
/// second assembly into every WASM payload to save nothing.
/// </remarks>
public enum ButtonState
{
    /// <summary>Idle and ready.</summary>
    Normal,

    /// <summary>Working. Shows the busy indicator and, by default, stops accepting clicks.</summary>
    Busy,

    /// <summary>The work succeeded.</summary>
    Success,

    /// <summary>The work failed.</summary>
    Error
}


/// <summary>How much of a <see cref="ShinyButton"/> the surface paints — the Material 3 button family.</summary>
public enum ButtonAppearance
{
    /// <summary>Solid fill in the type colour. The highest-emphasis action on a screen.</summary>
    Filled,

    /// <summary>The type's container tint — filled's quieter sibling.</summary>
    Tonal,

    /// <summary>Transparent with an outline.</summary>
    Outlined,

    /// <summary>Text only, no fill and no outline.</summary>
    Text,

    /// <summary>A raised surface.</summary>
    Elevated
}


/// <summary>What a <see cref="ShinyButton"/> means, mapped onto the theme's semantic colour families.</summary>
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


/// <summary>What <see cref="ButtonState.Busy"/> does to a <see cref="ShinyButton"/>'s content.</summary>
public enum ButtonBusyMode
{
    /// <summary>
    /// The busy indicator takes the left icon's place and the text stays put. The default, because
    /// both are the same size, so the button cannot change width while it works.
    /// </summary>
    ReplaceLeftIcon,

    /// <summary>
    /// The content fades out and a centred indicator takes over. The content keeps its layout space,
    /// so the button holds the width it had.
    /// </summary>
    ReplaceContent,

    /// <summary>The indicator appears after the right icon and nothing else moves.</summary>
    KeepContent
}


/// <summary>Where a <see cref="ShinyButton"/>'s icons sit relative to its text.</summary>
public enum ButtonContentLayout
{
    /// <summary>Left icon, text, right icon, in a row.</summary>
    Sides,

    /// <summary>Both icons above the text.</summary>
    Top,

    /// <summary>Both icons below the text.</summary>
    Bottom
}
