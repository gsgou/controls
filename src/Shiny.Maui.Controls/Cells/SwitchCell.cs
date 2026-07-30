using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using TvTableView = Shiny.Maui.Controls.TableView;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Cells;

public class SwitchCell : CellBase
{
    /// <summary>
    /// Children are built by CellBase's constructor (BuildLayout -> the virtual
    /// CreateAccessoryView override above), so by the time this body runs they exist.
    /// Marking ready here replays any property an implicit Style applied before
    /// construction - see StyleGuard.
    /// </summary>
    public SwitchCell() => StyleGuard.MarkReady(this, typeof(SwitchCell));

    Switch switchControl = default!;

    public static readonly BindableProperty OnProperty = BindableProperty.Create(
        nameof(On), typeof(bool), typeof(SwitchCell), false,
        BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(SwitchCell), () =>
            {
                ((SwitchCell)b).switchControl.IsToggled = (bool)n;
            }));

    public static readonly BindableProperty OnColorProperty = BindableProperty.Create(
        nameof(OnColor), typeof(Color), typeof(SwitchCell), null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(SwitchCell), () =>
            {
                ((SwitchCell)b).UpdateSwitchColor();
            }));

    public bool On
    {
        get => (bool)GetValue(OnProperty);
        set => SetValue(OnProperty, value);
    }

    public Color? OnColor
    {
        get => (Color?)GetValue(OnColorProperty);
        set => SetValue(OnColorProperty, value);
    }

    protected override View? CreateAccessoryView()
    {
        switchControl = new Switch
        {
            VerticalOptions = LayoutOptions.Center
        };
        switchControl.Toggled += (s, e) => On = e.Value;
        return switchControl;
    }

    void UpdateSwitchColor()
    {
        var color = OnColor ?? ParentTableView?.CellAccentColor;
        if (color != null)
            switchControl.OnColor = color;
    }

    protected override void OnTapped()
    {
        switchControl.IsToggled = !switchControl.IsToggled;
    }
}