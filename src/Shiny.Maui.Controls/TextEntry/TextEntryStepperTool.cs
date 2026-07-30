using Shiny.Maui.Controls.Infrastructure;
namespace Shiny.Maui.Controls;

/// <summary>
/// A TextEntry tool that increments or decrements the numeric value in the entry.
/// If Text is not set, displays the step value with +/- prefix (e.g. "+1" or "-5").
/// </summary>
public class TextEntryStepperTool : TextEntryTool, ITextEntryAwareTool
{
    TextEntry? textEntry;
    bool textWasSetByUser;

    public TextEntryStepperTool()
    {
        Clicked += OnClicked;

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(TextEntryStepperTool));
    }

    // Step
    public static readonly BindableProperty StepProperty = BindableProperty.Create(
        nameof(Step), typeof(double), typeof(TextEntryStepperTool), 1.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(TextEntryStepperTool), () =>
            {
                ((TextEntryStepperTool)b).UpdateDisplayText();
            }));
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(Text))
            textWasSetByUser = true;
    }

    public void Attach(TextEntry entry)
    {
        textEntry = entry;
        UpdateDisplayText();
    }

    public void Detach()
    {
        textEntry = null;
    }

    void UpdateDisplayText()
    {
        if (!textWasSetByUser || string.IsNullOrEmpty(Text))
        {
            textWasSetByUser = false;
            Text = Step >= 0 ? $"+{Step:G}" : $"{Step:G}";
        }
    }

    void OnClicked(object? sender, EventArgs e)
    {
        if (textEntry is null) return;

        double currentValue = 0;
        if (!string.IsNullOrEmpty(textEntry.Text) && double.TryParse(textEntry.Text, out var parsed))
            currentValue = parsed;

        var newValue = currentValue + Step;
        textEntry.Text = newValue.ToString("G");
    }
}
