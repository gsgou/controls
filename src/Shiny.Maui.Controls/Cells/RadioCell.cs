using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using TvTableView = Shiny.Maui.Controls.TableView;
using TvTableSection = Shiny.Maui.Controls.Sections.TableSection;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Cells;

public class RadioCell : CellBase
{
    /// <summary>
    /// Children are built by CellBase's constructor (BuildLayout -> the virtual
    /// CreateAccessoryView override above), so by the time this body runs they exist.
    /// Marking ready here replays any property an implicit Style applied before
    /// construction - see StyleGuard.
    /// </summary>
    public RadioCell() => StyleGuard.MarkReady(this, typeof(RadioCell));

    RadioButton radioButton = default!;

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(object), typeof(RadioCell), null);

    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor), typeof(Color), typeof(RadioCell), null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(RadioCell), () =>
            {
                ((RadioCell)b).UpdateAccentColor();
            }));

    // Attached property for section-level or tableview-level selected value
    public static readonly BindableProperty SelectedValueProperty = BindableProperty.CreateAttached(
        "SelectedValue", typeof(object), typeof(RadioCell), null,
        BindingMode.TwoWay,
        // Attached, so the target is the section or table the radio group lives on, never a
        // RadioCell - RadioCell's level would never be marked on it. What the handler needs
        // built is the target's cell collection, so gate on the target's own level. Anything
        // else carrying the value has no cells to update and needs no gate at all.
        propertyChanged: (b, o, n) =>
        {
            if (b is TvTableView)
                StyleGuard.WhenReady(b, typeof(TvTableView), () => OnSelectedValueChanged(b, o, n));
            else if (b is TvTableSection)
                StyleGuard.WhenReady(b, typeof(TvTableSection), () => OnSelectedValueChanged(b, o, n));
        });

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

    public static object? GetSelectedValue(BindableObject obj) => obj.GetValue(SelectedValueProperty);
    public static void SetSelectedValue(BindableObject obj, object? value) => obj.SetValue(SelectedValueProperty, value);

    protected override View? CreateAccessoryView()
    {
        radioButton = new RadioButton
        {
            VerticalOptions = LayoutOptions.Center
        };
        radioButton.CheckedChanged += OnRadioCheckedChanged;
        return radioButton;
    }

    void OnRadioCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;

        // Write to Section scope
        if (ParentSection != null)
            SetSelectedValue(ParentSection, Value);

        // Also write to TableView scope (global radio groups)
        if (ParentTableView != null)
            SetSelectedValue(ParentTableView, Value);
    }

    static void OnSelectedValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TvTableSection section)
        {
            foreach (var cell in section.GetVisibleCells())
            {
                if (cell is RadioCell radioCell)
                    radioCell.radioButton.IsChecked = Equals(radioCell.Value, newValue);
            }
        }
        else if (bindable is TvTableView tableView)
        {
            foreach (var sec in tableView.GetAllSections())
            {
                foreach (var cell in sec.GetVisibleCells())
                {
                    if (cell is RadioCell radioCell)
                        radioCell.radioButton.IsChecked = Equals(radioCell.Value, newValue);
                }
            }
        }
    }

    void UpdateAccentColor()
    {
        var color = AccentColor ?? ParentTableView?.CellAccentColor;
        if (color != null)
            radioButton.BorderColor = color;
    }

    protected override void OnTapped()
    {
        radioButton.IsChecked = true;
    }
}