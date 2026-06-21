namespace Shiny.Maui.Controls.Dialogs;

public enum DialogKind
{
    Alert,
    Confirm,
    Prompt,
    ActionSheet
}

public class DialogConfig
{
    public DialogKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string OkText { get; set; } = "OK";

    /// <summary>Cancel/secondary button text. Null hides the button (Alert).</summary>
    public string? CancelText { get; set; }

    /// <summary>ActionSheet options, in display order. The chosen one is returned from <c>ActionSheet</c>.</summary>
    public IReadOnlyList<string> Actions { get; set; } = [];

    /// <summary>ActionSheet option to render destructively (red). Must match one of the <see cref="Actions"/>.</summary>
    public string? DestructiveAction { get; set; }

    /// <summary>Prompt placeholder text.</summary>
    public string? Placeholder { get; set; }

    /// <summary>Prompt initial value.</summary>
    public string? InitialValue { get; set; }

    /// <summary>Keyboard for the Prompt entry.</summary>
    public Keyboard Keyboard { get; set; } = Keyboard.Default;

    /// <summary>Mask the Prompt entry (password).</summary>
    public bool IsPassword { get; set; }

    // --- Appearance ---

    public DialogAnimation Animation { get; set; } = DialogAnimation.Pop;

    /// <summary>Tapping the dimmed backdrop cancels the dialog.</summary>
    public bool DismissOnBackdrop { get; set; } = true;

    public bool UseFeedback { get; set; } = true;

    public Color? BackgroundColor { get; set; }
    public Color? TitleColor { get; set; }
    public Color? MessageColor { get; set; }
    public Color? OkButtonColor { get; set; }
    public Color? OkButtonTextColor { get; set; }
    public Color? CancelButtonColor { get; set; }
    public Color? CancelButtonTextColor { get; set; }
    public double CornerRadius { get; set; } = 16;

    /// <summary>Opacity of the dimmed backdrop (0-1).</summary>
    public double BackdropOpacity { get; set; } = 0.45;

    /// <summary>Max width of the dialog card.</summary>
    public double MaxWidth { get; set; } = 360;
}
