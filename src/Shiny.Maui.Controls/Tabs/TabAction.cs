using System.Windows.Input;
using Shiny.Controls.MotionIcons;

namespace Shiny.Maui.Controls;

/// <summary>
/// One row of the centre button's menu. Deliberately shaped like a <see cref="ToolbarItem"/> —
/// text, icon, command — because that is the thing it replaces: a per-page action that belongs to
/// the page rather than to the bar.
/// </summary>
/// <example>
/// <code language="xaml">
/// &lt;ContentPage ...&gt;
///     &lt;shiny:ShinyTabs.Actions&gt;
///         &lt;shiny:TabAction Text="New note" Icon="edit" Command="{Binding NewNoteCommand}" /&gt;
///         &lt;shiny:TabAction Text="Delete" Icon="trash" IsDestructive="True" Command="{Binding DeleteCommand}" /&gt;
///     &lt;/shiny:ShinyTabs.Actions&gt;
/// &lt;/ContentPage&gt;
/// </code>
/// </example>
public class TabAction : BindableObject, ITabIcon
{
    /// <summary>Backing store for <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(TabAction), null);

    /// <summary>Backing store for <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(TabAction), null);

    /// <summary>Backing store for <see cref="IconSource"/>.</summary>
    public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(
        nameof(IconSource), typeof(MotionIconDefinition), typeof(TabAction), null);

    /// <summary>Backing store for <see cref="IconPathData"/>.</summary>
    public static readonly BindableProperty IconPathDataProperty = BindableProperty.Create(
        nameof(IconPathData), typeof(string), typeof(TabAction), null);

    /// <summary>Backing store for <see cref="IconImage"/>.</summary>
    public static readonly BindableProperty IconImageProperty = BindableProperty.Create(
        nameof(IconImage), typeof(ImageSource), typeof(TabAction), null);

    /// <summary>Backing store for <see cref="Motion"/>.</summary>
    public static readonly BindableProperty MotionProperty = BindableProperty.Create(
        nameof(Motion), typeof(MotionPreset), typeof(TabAction), MotionPreset.Default);

    /// <summary>Backing store for <see cref="Command"/>.</summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(TabAction), null);

    /// <summary>Backing store for <see cref="CommandParameter"/>.</summary>
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(TabAction), null);

    /// <summary>Backing store for <see cref="IsDestructive"/>.</summary>
    public static readonly BindableProperty IsDestructiveProperty = BindableProperty.Create(
        nameof(IsDestructive), typeof(bool), typeof(TabAction), false);

    /// <summary>Backing store for <see cref="IsEnabled"/>.</summary>
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.Create(
        nameof(IsEnabled), typeof(bool), typeof(TabAction), true);

    /// <summary>Backing store for <see cref="IsSeparator"/>.</summary>
    public static readonly BindableProperty IsSeparatorProperty = BindableProperty.Create(
        nameof(IsSeparator), typeof(bool), typeof(TabAction), false);

    /// <summary>Backing store for <see cref="Tag"/>.</summary>
    public static readonly BindableProperty TagProperty = BindableProperty.Create(
        nameof(Tag), typeof(object), typeof(TabAction), null);

    /// <summary>The row's label.</summary>
    public string? Text
    {
        get => (string?)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <inheritdoc/>
    public string? Icon
    {
        get => (string?)this.GetValue(IconProperty);
        set => this.SetValue(IconProperty, value);
    }

    /// <inheritdoc/>
    public MotionIconDefinition? IconSource
    {
        get => (MotionIconDefinition?)this.GetValue(IconSourceProperty);
        set => this.SetValue(IconSourceProperty, value);
    }

    /// <inheritdoc/>
    public string? IconPathData
    {
        get => (string?)this.GetValue(IconPathDataProperty);
        set => this.SetValue(IconPathDataProperty, value);
    }

    /// <inheritdoc/>
    public ImageSource? IconImage
    {
        get => (ImageSource?)this.GetValue(IconImageProperty);
        set => this.SetValue(IconImageProperty, value);
    }

    /// <inheritdoc/>
    public MotionPreset Motion
    {
        get => (MotionPreset)this.GetValue(MotionProperty);
        set => this.SetValue(MotionProperty, value);
    }

    /// <summary>Run when the row is tapped, before the menu closes.</summary>
    public ICommand? Command
    {
        get => (ICommand?)this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }

    /// <summary>Passed to <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => this.GetValue(CommandParameterProperty);
        set => this.SetValue(CommandParameterProperty, value);
    }

    /// <summary>Draws the row in the theme's error colour. Purely visual — nothing is confirmed for you.</summary>
    public bool IsDestructive
    {
        get => (bool)this.GetValue(IsDestructiveProperty);
        set => this.SetValue(IsDestructiveProperty, value);
    }

    /// <summary>A disabled row is dimmed and does not respond to a tap.</summary>
    public bool IsEnabled
    {
        get => (bool)this.GetValue(IsEnabledProperty);
        set => this.SetValue(IsEnabledProperty, value);
    }

    /// <summary>Draws a divider instead of a row. Everything else on the action is ignored.</summary>
    public bool IsSeparator
    {
        get => (bool)this.GetValue(IsSeparatorProperty);
        set => this.SetValue(IsSeparatorProperty, value);
    }

    /// <summary>Whatever identifies this action to your handler. <see cref="Text"/> is not unique.</summary>
    public object? Tag
    {
        get => this.GetValue(TagProperty);
        set => this.SetValue(TagProperty, value);
    }

    /// <summary>Raised when the row is tapped, alongside <see cref="Command"/>.</summary>
    public event EventHandler<TabActionEventArgs>? Clicked;

    internal void Invoke()
    {
        if (!this.IsEnabled || this.IsSeparator)
            return;

        this.Clicked?.Invoke(this, new TabActionEventArgs(this));

        var command = this.Command;
        if (command?.CanExecute(this.CommandParameter) == true)
            command.Execute(this.CommandParameter);
    }
}
