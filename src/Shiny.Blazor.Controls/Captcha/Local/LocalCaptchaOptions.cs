namespace Shiny.Blazor.Controls.Captchas;

/// <summary>
/// Settings for the built-in, self-hosted challenge — the one that needs no account, no site key
/// and no network call.
/// </summary>
/// <remarks>
/// This is a speed bump, not a security boundary. The challenge is generated and checked in the
/// browser, so anything with a debugger attached can read the answer straight out of memory. It
/// stops naive form-fill bots and nothing more. When the form is worth attacking, register one of
/// the hosted providers and verify the token on your server.
/// </remarks>
public class LocalCaptchaOptions
{
    /// <summary>Distorted text or an arithmetic question. Default <see cref="LocalCaptchaMode.Text"/>.</summary>
    public LocalCaptchaMode Mode { get; set; } = LocalCaptchaMode.Text;

    /// <summary>How many characters the text challenge draws. Default 5.</summary>
    public int Length { get; set; } = 5;

    /// <summary>
    /// The alphabet the text challenge draws from. Defaults to upper-case letters and digits with
    /// the look-alikes (<c>0 O 1 I L</c>) removed, because "is that a one or an ell" is not a
    /// Turing test.
    /// </summary>
    public string CharacterSet { get; set; } = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    /// <summary>Whether the typed answer has to match case. Default false.</summary>
    public bool CaseSensitive { get; set; }

    /// <summary>Canvas width in CSS pixels. Default 180.</summary>
    public int Width { get; set; } = 180;

    /// <summary>Canvas height in CSS pixels. Default 60.</summary>
    public int Height { get; set; } = 60;

    /// <summary>
    /// How long a solved challenge stays solved before <c>Expired</c> fires and the widget resets.
    /// Default 120. Zero or less disables expiry.
    /// </summary>
    public int ExpirySeconds { get; set; } = 120;

    /// <summary>
    /// Wrong answers allowed before the challenge is thrown away and redrawn. Default 3.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>The prompt above the input. Default "Type the characters you see".</summary>
    public string? Prompt { get; set; }

    /// <summary>Text shown after a wrong answer. Default "That didn't match — try again".</summary>
    public string IncorrectText { get; set; } = "That didn't match — try again";

    /// <summary>Tooltip on the redraw button. Default "New challenge".</summary>
    public string RefreshText { get; set; } = "New challenge";

    /// <summary>Placeholder in the answer box. Default "Answer".</summary>
    public string PlaceholderText { get; set; } = "Answer";
}
