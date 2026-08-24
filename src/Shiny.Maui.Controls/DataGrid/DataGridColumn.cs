namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// A DataGrid column bound to a property by name. Set <see cref="PropertyName"/> for the default
/// (sortable/filterable/editable) cell, or use <see cref="DataGridTemplateColumn"/> for custom content.
/// </summary>
public class DataGridColumn : BindableObject
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(DataGridColumn), string.Empty);

    public static readonly BindableProperty PropertyNameProperty = BindableProperty.Create(
        nameof(PropertyName), typeof(string), typeof(DataGridColumn), null);

    public static readonly BindableProperty WidthProperty = BindableProperty.Create(
        nameof(Width), typeof(GridLength), typeof(DataGridColumn), GridLength.Star);

    public static readonly BindableProperty WidthPercentProperty = BindableProperty.Create(
        nameof(WidthPercent), typeof(double), typeof(DataGridColumn), 0d);

    public static readonly BindableProperty MinWidthProperty = BindableProperty.Create(
        nameof(MinWidth), typeof(double), typeof(DataGridColumn), 0d);

    public static readonly BindableProperty MaxWidthProperty = BindableProperty.Create(
        nameof(MaxWidth), typeof(double), typeof(DataGridColumn), 0d);

    public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(
        nameof(IsVisible), typeof(bool), typeof(DataGridColumn), true);

    public static readonly BindableProperty SortableProperty = BindableProperty.Create(
        nameof(Sortable), typeof(bool), typeof(DataGridColumn), true);

    public static readonly BindableProperty FilterableProperty = BindableProperty.Create(
        nameof(Filterable), typeof(bool), typeof(DataGridColumn), true);

    public static readonly BindableProperty GroupableProperty = BindableProperty.Create(
        nameof(Groupable), typeof(bool), typeof(DataGridColumn), true);

    public static readonly BindableProperty EditableProperty = BindableProperty.Create(
        nameof(Editable), typeof(bool), typeof(DataGridColumn), true);

    public static readonly BindableProperty ResizableProperty = BindableProperty.Create(
        nameof(Resizable), typeof(bool), typeof(DataGridColumn), true);

    public static readonly BindableProperty StringFormatProperty = BindableProperty.Create(
        nameof(StringFormat), typeof(string), typeof(DataGridColumn), null);

    public static readonly BindableProperty FrozenProperty = BindableProperty.Create(
        nameof(Frozen), typeof(DataGridFrozen), typeof(DataGridColumn), DataGridFrozen.None);

    public string Title
    {
        get => (string)this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

    public string? PropertyName
    {
        get => (string?)this.GetValue(PropertyNameProperty);
        set => this.SetValue(PropertyNameProperty, value);
    }

    public GridLength Width
    {
        get => (GridLength)this.GetValue(WidthProperty);
        set => this.SetValue(WidthProperty, value);
    }

    /// <summary>
    /// Width as a percentage of the grid (1-100). Wins over <see cref="Width"/> when set; <c>0</c>
    /// (the default) means unset.
    /// </summary>
    /// <remarks>
    /// Outside <see cref="DataGrid.HorizontalScroll"/> this resolves to a star of the same factor,
    /// because a star factor <i>is</i> a percentage: MAUI's Grid divides the available width in
    /// exactly the ratio of the factors, so columns whose percentages sum to 100 each get theirs.
    /// Percentages that sum to less than 100 leave the remainder unclaimed (the columns simply share
    /// the whole width in that ratio); mixed with absolute columns they split whatever is left over.
    /// Under <c>HorizontalScroll</c> - where the columns are meant to be wider than the viewport, so
    /// "share what is available" has no meaning - it resolves against the scroller's own width
    /// instead, and percentages summing past 100 are what make the grid scroll.
    /// </remarks>
    public double WidthPercent
    {
        get => (double)this.GetValue(WidthPercentProperty);
        set => this.SetValue(WidthPercentProperty, value);
    }

    /// <summary>
    /// Smallest width (device-independent units) this column may take. <c>0</c> falls back to
    /// <see cref="DataGrid.MinColumnWidth"/>. Applies to an absolute <see cref="Width"/>, to a star
    /// width resolved under <see cref="DataGrid.HorizontalScroll"/>, and to interactive resizing.
    /// </summary>
    public double MinWidth
    {
        get => (double)this.GetValue(MinWidthProperty);
        set => this.SetValue(MinWidthProperty, value);
    }

    /// <summary>
    /// Largest width (device-independent units) this column may take. <c>0</c> falls back to
    /// <see cref="DataGrid.MaxColumnWidth"/>, which is itself unbounded by default. Applies to an
    /// absolute <see cref="Width"/>, to a star width resolved under
    /// <see cref="DataGrid.HorizontalScroll"/>, and to interactive resizing.
    /// </summary>
    public double MaxWidth
    {
        get => (double)this.GetValue(MaxWidthProperty);
        set => this.SetValue(MaxWidthProperty, value);
    }

    public bool IsVisible
    {
        get => (bool)this.GetValue(IsVisibleProperty);
        set => this.SetValue(IsVisibleProperty, value);
    }

    public bool Sortable
    {
        get => (bool)this.GetValue(SortableProperty);
        set => this.SetValue(SortableProperty, value);
    }

    public bool Filterable
    {
        get => (bool)this.GetValue(FilterableProperty);
        set => this.SetValue(FilterableProperty, value);
    }

    public bool Groupable
    {
        get => (bool)this.GetValue(GroupableProperty);
        set => this.SetValue(GroupableProperty, value);
    }

    public bool Editable
    {
        get => (bool)this.GetValue(EditableProperty);
        set => this.SetValue(EditableProperty, value);
    }

    /// <summary>
    /// Whether a resize handle is offered for this column. Only has an effect when the grid sets
    /// <see cref="DataGrid.AllowColumnResize"/>.
    /// </summary>
    public bool Resizable
    {
        get => (bool)this.GetValue(ResizableProperty);
        set => this.SetValue(ResizableProperty, value);
    }

    public string? StringFormat
    {
        get => (string?)this.GetValue(StringFormatProperty);
        set => this.SetValue(StringFormatProperty, value);
    }

    /// <summary>
    /// Freezes (pins) this column to the leading or trailing edge so it stays put while the grid
    /// scrolls horizontally. Only a contiguous run at each edge can be frozen - see
    /// <see cref="DataGrid.FrozenColumns"/> / <see cref="DataGrid.FrozenEndColumns"/> for the
    /// count-based form. Requires <see cref="DataGrid.HorizontalScroll"/>.
    /// </summary>
    public DataGridFrozen Frozen
    {
        get => (DataGridFrozen)this.GetValue(FrozenProperty);
        set => this.SetValue(FrozenProperty, value);
    }

    /// <summary>Custom cell content. When null, a default <see cref="Label"/> bound to <see cref="PropertyName"/> is used.</summary>
    public DataTemplate? CellTemplate { get; set; }

    /// <summary>Custom header content. When null, <see cref="Title"/> text is shown.</summary>
    public DataTemplate? HeaderTemplate { get; set; }

    /// <summary>Custom editor content for inline editing. When null, a default <see cref="Entry"/> is used.</summary>
    public DataTemplate? EditTemplate { get; set; }

    /// <summary>Custom footer content. When null, the <see cref="Aggregate"/> (if any) is shown.</summary>
    public DataTemplate? FooterTemplate { get; set; }

    /// <summary>Footer/group aggregate for this column.</summary>
    public DataGridAggregateDefinition? Aggregate { get; set; }

    /// <summary>Optional reflection-free value accessor (set for full-trim/AOT scenarios).</summary>
    public Func<object, object?>? ValueGetter { get; set; }

    /// <summary>Optional reflection-free value setter (inline editing).</summary>
    public Action<object, object?>? ValueSetter { get; set; }

    /// <summary>Optional custom value comparer for sorting.</summary>
    public IComparer<object?>? Comparer { get; set; }

    /// <summary>Stable identity used in sort/filter/group state.</summary>
    internal string Id => this.PropertyName ?? this.Title ?? this.GetHashCode().ToString();

    /// <summary>True when the column has a value (sortable/filterable/editable); false for template-only columns.</summary>
    internal virtual bool HasValue => true;

    /// <remarks>
    /// Deliberately NOT named <c>GetValue</c>: an overload taking <c>object?</c> on a
    /// <see cref="BindableObject"/> hides <c>BindableObject.GetValue(BindableProperty)</c> entirely
    /// (C# stops at the most-derived type that declares the name), so every bindable property getter
    /// on this class would call back into here, read <see cref="PropertyName"/>, and recurse until
    /// the stack blew up.
    /// </remarks>
    internal virtual object? GetCellValue(object? item)
        => this.ValueGetter is not null && item is not null
            ? this.ValueGetter(item)
            : DataGridReflection.GetValue(item, this.PropertyName);

    internal virtual void SetCellValue(object? item, object? value)
    {
        if (item is null)
            return;

        if (this.ValueSetter is not null)
            this.ValueSetter(item, value);
        else
            DataGridReflection.SetValue(item, this.PropertyName, value);
    }

    internal virtual string? GetText(object? item)
    {
        var value = this.GetCellValue(item);
        if (value is null)
            return null;

        if (!string.IsNullOrEmpty(this.StringFormat) && value is IFormattable formattable)
            return formattable.ToString(this.StringFormat, System.Globalization.CultureInfo.CurrentCulture);

        return value.ToString();
    }

    internal Type GetDataType(Type itemType)
        => DataGridReflection.GetPropertyType(itemType, this.PropertyName);
}
