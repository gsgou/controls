namespace Shiny.Blazor.Controls.Captchas;

/// <summary>The widget's colour scheme.</summary>
public enum CaptchaTheme
{
    /// <summary>Follow the page — resolved from <c>prefers-color-scheme</c> at render time.</summary>
    Auto,
    Light,
    Dark
}


/// <summary>
/// The widget's footprint. Not every provider honours every value — <see cref="Flexible"/> is
/// Turnstile-only and falls back to <see cref="Normal"/> elsewhere.
/// </summary>
public enum CaptchaSize
{
    Normal,
    Compact,

    /// <summary>
    /// No visible challenge; the provider scores the session in the background and the widget is
    /// triggered by <see cref="Captcha.ExecuteAsync"/>. Ignored by the local provider, which has
    /// nothing to score.
    /// </summary>
    Invisible,

    /// <summary>Fills its container's width. Turnstile only.</summary>
    Flexible
}


/// <summary>Where an invisible provider parks its badge.</summary>
public enum CaptchaBadgePosition
{
    BottomEnd,
    BottomStart,

    /// <summary>Rendered inline, so the page decides where it sits.</summary>
    Inline
}


/// <summary>What the local provider asks the user to do.</summary>
public enum LocalCaptchaMode
{
    /// <summary>Distorted characters drawn to a canvas, typed back.</summary>
    Text,

    /// <summary>A small arithmetic question — friendlier to screen readers than distorted text.</summary>
    Math
}
