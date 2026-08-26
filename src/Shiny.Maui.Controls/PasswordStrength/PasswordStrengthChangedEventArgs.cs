namespace Shiny.Maui.Controls;

/// <summary>Raised by <see cref="PasswordStrength.StrengthChanged"/> when the verdict changes.</summary>
public class PasswordStrengthChangedEventArgs(PasswordStrengthResult result) : EventArgs
{
    /// <summary>The evaluator's full verdict.</summary>
    public PasswordStrengthResult Result { get; } = result;

    /// <summary>Shorthand for <see cref="PasswordStrengthResult.Score"/>.</summary>
    public int Score => this.Result.Score;

    /// <summary>Shorthand for <see cref="PasswordStrengthResult.Level"/>.</summary>
    public PasswordStrengthLevel Level => this.Result.Level;

    /// <summary>Shorthand for <see cref="PasswordStrengthResult.IsAcceptable"/>.</summary>
    public bool IsAcceptable => this.Result.IsAcceptable;
}
