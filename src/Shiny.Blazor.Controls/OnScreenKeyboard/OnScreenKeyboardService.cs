namespace Shiny.Blazor.Controls.OnScreenKeyboard;

public sealed class OnScreenKeyboardService : IOnScreenKeyboardService
{
    bool isVisible;

    public OnScreenKeyboardService(OnScreenKeyboardOptions options) => this.Options = options;

    public OnScreenKeyboardOptions Options { get; }

    public bool IsVisible
    {
        get => this.isVisible;
        private set
        {
            if (this.isVisible == value)
                return;

            this.isVisible = value;
            this.VisibilityChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? VisibilityChanged;

    public void Show() => this.IsVisible = true;
    public void Hide() => this.IsVisible = false;
    public void Toggle() => this.IsVisible = !this.IsVisible;
}
