using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using TvTableView = Shiny.Maui.Controls.TableView;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Cells;

public class CheckboxCell : CellBase
{
    /// <summary>
    /// Children are built by CellBase's constructor (BuildLayout -> the virtual
    /// CreateAccessoryView override above), so by the time this body runs they exist.
    /// Marking ready here replays any property an implicit Style applied before
    /// construction - see StyleGuard.
    /// </summary>
    public CheckboxCell() => StyleGuard.MarkReady(this, typeof(CheckboxCell));

    CheckBox checkBox = default!;

    public static readonly BindableProperty CheckedProperty = BindableProperty.Create(
        nameof(Checked), typeof(bool), typeof(CheckboxCell), false,
        BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, () =>
            {
                ((CheckboxCell)b).checkBox.IsChecked = (bool)n;
            }));

    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor), typeof(Color), typeof(CheckboxCell), null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, () =>
            {
                ((CheckboxCell)b).UpdateCheckBoxColor();
            }));

    public bool Checked
    {
        get => (bool)GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public Color? AccentColor
    {
        get => (Color?)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    protected override View? CreateAccessoryView()
    {
        checkBox = new CheckBox
        {
            VerticalOptions = LayoutOptions.Center
        };
        checkBox.CheckedChanged += (s, e) => Checked = e.Value;
        return checkBox;
    }

    void UpdateCheckBoxColor()
    {
        var color = AccentColor ?? ParentTableView?.CellAccentColor;
        if (color != null)
            checkBox.Color = color;
    }

    protected override void OnTapped()
    {
        checkBox.IsChecked = !checkBox.IsChecked;
    }
}