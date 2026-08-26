using Shiny.Maui.Controls;

namespace Sample.Features.PasswordStrength;

public partial class PasswordStrengthPage : ContentPage
{
    public PasswordStrengthPage()
    {
        InitializeComponent();

        // Per-control override. Registering it with SetCustomPasswordStrengthEvaluator would apply
        // it to every field in the app instead, which is what a real app usually wants.
        Delayed.Evaluator = new DelayedPasswordStrengthEvaluator();
    }

    void OnStrengthChanged(object? sender, PasswordStrengthChangedEventArgs e)
        => (BindingContext as PasswordStrengthViewModel)?.OnStrengthChanged(e);

    void OnFrenchToggled(object? sender, ToggledEventArgs e)
        => Localized.Localizer = e.Value ? French : null;

    static string? French(PasswordStrengthText text) => text.Key switch
    {
        PasswordStrengthTextKey.LevelWeak => "Faible",
        PasswordStrengthTextKey.LevelFair => "Moyen",
        PasswordStrengthTextKey.LevelGood => "Bon",
        PasswordStrengthTextKey.LevelStrong => "Fort",
        PasswordStrengthTextKey.ShowPassword => "Voir",
        PasswordStrengthTextKey.HidePassword => "Cacher",
        // Argument carries the number, so the sentence can be rebuilt rather than patched
        PasswordStrengthTextKey.RuleMinimumLength => $"Au moins {text.Argument} caractères",
        PasswordStrengthTextKey.RuleNumber => "Un chiffre",
        PasswordStrengthTextKey.RuleNotCompromised => "Pas un mot de passe courant",
        _ => null // anything not translated keeps the default
    };
}
