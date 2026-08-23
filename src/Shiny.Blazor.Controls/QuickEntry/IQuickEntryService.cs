using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.QuickEntry;

/// <summary>
/// Drives the quick entry popup — a prompt surface summoned over the page — and the screen-edge glow
/// that goes with it. Rendering is <c>&lt;QuickEntryHost /&gt;</c>'s job; place one of those once,
/// near the root of the layout.
/// </summary>
/// <remarks>
/// The glow lives here rather than on a service of its own because the two are almost always used
/// together, and splitting them meant an app wiring up an assistant had to resolve, configure and
/// keep two objects in step for one visible behaviour. Same shape as the MAUI service, minus the
/// desktop-window presentation a browser cannot offer.
/// </remarks>
public interface IQuickEntryService
{
    /// <summary>The live options object. Changes apply on the next open.</summary>
    QuickEntryOptions Options { get; }

    /// <summary>True while the popup is on screen.</summary>
    bool IsOpen { get; }

    /// <summary>True while the glow is lit.</summary>
    bool IsGlowVisible { get; }

    /// <summary>
    /// Content rendered inside the popup. Leave it null and the host renders a
    /// <see cref="PromptView"/> with <see cref="ConfigurePrompt"/> applied.
    /// </summary>
    RenderFragment? Content { get; }

    /// <summary>Open the popup.</summary>
    void Show();

    /// <summary>Open the popup with one-off content, replacing anything set through <see cref="SetContent"/>.</summary>
    void Show(RenderFragment content);

    /// <summary>Close the popup.</summary>
    void Hide();

    /// <summary>Open the popup if closed, close it if open.</summary>
    void Toggle();

    /// <summary>Set the popup's content for every subsequent open. Null restores the built-in prompt.</summary>
    void SetContent(RenderFragment? content);

    /// <summary>
    /// Configure the built-in <see cref="PromptView"/> the host renders when no <see cref="Content"/>
    /// is set — its placeholder, suggestions, and what happens on submit.
    /// </summary>
    void ConfigurePrompt(Action<PromptViewState> configure);

    /// <summary>The state the built-in prompt renders from. Mutating it re-renders the host.</summary>
    PromptViewState Prompt { get; }

    /// <summary>Fade the glow in and leave it running. Independent of the popup.</summary>
    void ShowGlow();

    /// <summary>Fade the glow out.</summary>
    void HideGlow();

    /// <summary>Light the glow for a fixed period, then put it out.</summary>
    Task PulseGlowAsync(TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>Raised whenever anything the host renders from changes.</summary>
    event EventHandler? Changed;

    /// <summary>Raised after the popup opens.</summary>
    event EventHandler? Opened;

    /// <summary>Raised after the popup closes, however it was dismissed.</summary>
    event EventHandler? Closed;
}
