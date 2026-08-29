using System.Windows.Input;

namespace Shiny.Maui.Controls.Ribbons;

/// <summary>
/// A plain command button on the ribbon.
/// </summary>
/// <example>
/// <code language="xaml">
/// &lt;shiny:RibbonButton Text="Paste" Icon="paste.png" Command="{Binding Paste}" /&gt;
/// &lt;shiny:RibbonButton Text="Cut" Icon="cut.png" Size="Small" Command="{Binding Cut}" /&gt;
/// </code>
/// </example>
public class RibbonButton : RibbonItem
{
    public static readonly BindableProperty CommandProperty = Redraw(
        nameof(Command), typeof(ICommand), typeof(RibbonButton)
    );

    public static readonly BindableProperty CommandParameterProperty = Redraw(
        nameof(CommandParameter), typeof(object), typeof(RibbonButton)
    );


    /// <summary>Raised when the button is pressed, after <see cref="Command"/> has run.</summary>
    public event EventHandler? Clicked;

    /// <summary>Raises <see cref="Clicked"/>. Derived kinds run their own work first and then call this.</summary>
    protected void RaiseClicked() => this.Clicked?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Runs on press. The ribbon does <b>not</b> consult <c>CanExecute</c> — bind
    /// <see cref="RibbonItem.IsEnabled"/> for that, so that "can this run" is one answer the author
    /// controls rather than two that can disagree on screen.
    /// </summary>
    public ICommand? Command
    {
        get => (ICommand?)this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => this.GetValue(CommandParameterProperty);
        set => this.SetValue(CommandParameterProperty, value);
    }


    /// <summary>
    /// Presses the button: runs the command and raises <see cref="Clicked"/>. Public because it is the
    /// seam a test presses through — a tap gesture cannot be raised from one.
    /// </summary>
    public virtual void Invoke()
    {
        if (!this.IsEnabled)
            return;

        var parameter = this.CommandParameter;
        if (this.Command?.CanExecute(parameter) == true)
            this.Command.Execute(parameter);

        this.RaiseClicked();
    }
}


/// <summary>
/// A button that stays pressed — bold, italic, show/hide gridlines.
/// </summary>
/// <remarks>
/// <see cref="IsChecked"/> is two-way by default, so the common case is a plain
/// <c>IsChecked="{Binding Bold}"</c> with no command at all. When a command is also set it runs after
/// the toggle, and the parameter it receives is the new state rather than
/// <see cref="RibbonButton.CommandParameter"/> when none was given — a toggle almost always wants to
/// know which way it went.
/// </remarks>
public class RibbonToggleButton : RibbonButton
{
    public static readonly BindableProperty IsCheckedProperty = BindableProperty.Create(
        nameof(IsChecked),
        typeof(bool),
        typeof(RibbonToggleButton),
        false,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) =>
        {
            var toggle = (RibbonToggleButton)b;
            toggle.RaiseChanged();
            toggle.CheckedChanged?.Invoke(toggle, new RibbonCheckedEventArgs((bool)n));
        }
    );


    /// <summary>Raised after <see cref="IsChecked"/> changes, however it changed.</summary>
    public event EventHandler<RibbonCheckedEventArgs>? CheckedChanged;

    /// <summary>Whether the format this button applies is on. Two-way by default.</summary>
    public bool IsChecked
    {
        get => (bool)this.GetValue(IsCheckedProperty);
        set => this.SetValue(IsCheckedProperty, value);
    }


    public override void Invoke()
    {
        if (!this.IsEnabled)
            return;

        this.IsChecked = !this.IsChecked;

        var parameter = this.CommandParameter ?? this.IsChecked;
        if (this.Command?.CanExecute(parameter) == true)
            this.Command.Execute(parameter);

        this.RaiseClicked();
    }
}
