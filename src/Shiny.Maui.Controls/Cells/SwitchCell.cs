using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using TvTableView = Shiny.Maui.Controls.TableView;
using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Cells;

public class SwitchCell : CellBase
{
    /// <summary>Appended to the cell's <see cref="Element.AutomationId"/> to name its switch.</summary>
    public const string SwitchAutomationIdSuffix = "_Switch";

    /// <summary>
    /// Children are built by CellBase's constructor (BuildLayout -> the virtual
    /// CreateAccessoryView override above), so by the time this body runs they exist.
    /// Marking ready here replays any property an implicit Style applied before
    /// construction - see StyleGuard.
    /// </summary>
    public SwitchCell()
    {
        AutomationIds.Link(this, switchControl, SwitchAutomationIdSuffix);
        // The table re-applies this when it hosts the cell; doing it here too means a cell used on
        // its own follows the theme from the start, not only after its accent is first changed.
        this.ApplyAccentStyles();
        StyleGuard.MarkReady(this, typeof(SwitchCell));
    }

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

    protected override void ApplyAccentStyles() => this.UpdateSwitchColor();

    void UpdateSwitchColor()
        => ThemeProbe.Tint(switchControl, Switch.OnColorProperty, OnColor ?? ParentTableView?.CellAccentColor, ShinyThemeKeys.Color.Primary);


    protected override void OnTapped()
    {
        switchControl.IsToggled = !switchControl.IsToggled;
    }
}