using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Cells;

public class SimpleCheckCell : CellBase
{
    /// <summary>
    /// Children are built by CellBase's constructor (BuildLayout -> the virtual
    /// CreateAccessoryView override above), so by the time this body runs they exist.
    /// Marking ready here replays any property an implicit Style applied before
    /// construction - see StyleGuard.
    /// </summary>
    public SimpleCheckCell() => StyleGuard.MarkReady(this, typeof(SimpleCheckCell));

    Label checkLabel = default!;

    public static readonly BindableProperty CheckedProperty = BindableProperty.Create(
        nameof(Checked), typeof(bool), typeof(SimpleCheckCell), false,
        BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, () =>
            {
                ((SimpleCheckCell)b).UpdateCheckVisibility();
            }));

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(object), typeof(SimpleCheckCell), null);

    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor), typeof(Color), typeof(SimpleCheckCell), null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, () =>
            {
                ((SimpleCheckCell)b).UpdateCheckColor();
            }));

    public bool Checked
    {
        get => (bool)GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public Color? AccentColor
    {
        get => (Color?)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    protected override View? CreateAccessoryView()
    {
        checkLabel = new Label
        {
            Text = "\u2713",
            FontSize = 20,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        return checkLabel;
    }

    void UpdateCheckVisibility()
    {
        checkLabel.IsVisible = Checked;
    }

    void UpdateCheckColor()
    {
        checkLabel.TextColor = AccentColor ?? ParentTableView?.CellAccentColor ?? Colors.Blue;
    }

    protected override void OnTapped()
    {
        Checked = !Checked;
    }
}