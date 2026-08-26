namespace Shiny.Maui.Controls;

/// <summary>Carries the state an <see cref="Expander"/> has just settled into.</summary>
public class ExpanderEventArgs(bool isExpanded) : EventArgs
{
    /// <summary>True when the expander is now open.</summary>
    public bool IsExpanded { get; } = isExpanded;
}


/// <summary>
/// Raised before an <see cref="Expander"/> changes state. Setting <see cref="Cancel"/> leaves it where
/// it was — which is how a header gets to say "not until this form is valid".
/// </summary>
public class ExpanderChangingEventArgs(bool isExpanded) : ExpanderEventArgs(isExpanded)
{
    /// <summary>Set to true to abandon the change.</summary>
    public bool Cancel { get; set; }
}


/// <summary>One <see cref="Accordion"/> item changing state.</summary>
public class AccordionItemEventArgs(Expander item, object? data, int index, bool isExpanded)
    : ExpanderEventArgs(isExpanded)
{
    /// <summary>The expander that changed.</summary>
    public Expander Item { get; } = item;

    /// <summary>
    /// The <c>ItemsSource</c> element behind <see cref="Item"/>, or null when the expander was declared
    /// in markup rather than generated.
    /// </summary>
    public object? Data { get; } = data;

    /// <summary>Position of <see cref="Item"/> among the accordion's expanders.</summary>
    public int Index { get; } = index;
}
