using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.Wizard;

[ShellMap<WizardPage>(registerRoute: false)]
public partial class WizardViewModel : ObservableObject
{
    [ObservableProperty] string currentStep = "Account";
    [ObservableProperty] string status = "Fill in an email to leave step 1.";

    [ObservableProperty] string email = string.Empty;
    [ObservableProperty] bool wantsDelivery = true;
    [ObservableProperty] string address = string.Empty;

    /// <summary>Gates the Account step. The wizard ANDs it with its own boundary checks.</summary>
    public bool AccountIsValid => !string.IsNullOrWhiteSpace(this.Email) && this.Email.Contains('@');

    partial void OnEmailChanged(string value)
    {
        this.OnPropertyChanged(nameof(this.AccountIsValid));
        this.Status = this.AccountIsValid ? "Looks good — Next is enabled." : "Enter an email containing @.";
    }

    partial void OnCurrentStepChanged(string value) => this.Status = $"On step: {value}";

    [RelayCommand]
    void Submit() => this.Status = $"Submitted {this.Email}" +
                                   (this.WantsDelivery ? $" for delivery to {this.Address}" : " for collection");

    [RelayCommand]
    void Abandon() => this.Status = "Cancelled — nothing was submitted.";
}
