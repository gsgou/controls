namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// Optional hook for popup content that wants to know when it is shown or hidden — focusing an
/// entry on open, cancelling an in-flight request on close.
/// </summary>
public interface IQuickEntryPresentationAware
{
    /// <summary>Called after the popup becomes visible.</summary>
    void OnQuickEntryOpened();

    /// <summary>Called after the popup is hidden.</summary>
    void OnQuickEntryClosed();
}

/// <summary>
/// Implemented by popup content that has a "working on it" state, so the host can drive the
/// screen-edge glow from it. <see cref="PromptView"/> implements this over its
/// <see cref="PromptView.IsBusy"/> property.
/// </summary>
public interface IQuickEntryBusyState
{
    /// <summary>True while the content is working.</summary>
    bool IsBusy { get; }

    /// <summary>Raised whenever <see cref="IsBusy"/> changes.</summary>
    event EventHandler? BusyChanged;
}
