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

    public static readonly BindableProperty DisplayAsProperty = BindableProperty.Create(
        nameof(DisplayAs), typeof(DataGridColumnFormat), typeof(DataGridColumn), DataGridColumnFormat.None);

    public static readonly BindableProperty DecimalsProperty = BindableProperty.Create(
        nameof(Decimals), typeof(int?), typeof(DataGridColumn), null);

    public static readonly BindableProperty NullTextProperty = BindableProperty.Create(
        nameof(NullText), typeof(string), typeof(DataGridColumn), null);

    public static readonly BindableProperty PrefixProperty = BindableProperty.Create(
        nameof(Prefix), typeof(string), typeof(DataGridColumn), null);

    public static readonly BindableProperty SuffixProperty = BindableProperty.Create(
        nameof(Suffix), typeof(string), typeof(DataGridColumn), null);

    public static readonly BindableProperty TrueTextProperty = BindableProperty.Create(
        nameof(TrueText), typeof(string), typeof(DataGridColumn), null);

    public static readonly BindableProperty FalseTextProperty = BindableProperty.Create(
        nameof(FalseText), typeof(string), typeof(DataGridColumn), null);

    public static readonly BindableProperty AlignmentProperty = BindableProperty.Create(
        nameof(Alignment), typeof(DataGridCellAlignment), typeof(DataGridColumn), DataGridCellAlignment.Auto);

    public static readonly BindableProperty HeaderAlignmentProperty = BindableProperty.Create(
        nameof(HeaderAlignment), typeof(DataGridCellAlignment), typeof(DataGridColumn), DataGridCellAlignment.Auto);

    public static readonly BindableProperty WrapProperty = BindableProperty.Create(
        nameof(Wrap), typeof(bool), typeof(DataGridColumn), false);

    public static readonly BindableProperty MaxLinesProperty = BindableProperty.Create(
        nameof(MaxLines), typeof(int), typeof(DataGridColumn), 0);

    // Bindable, not plain CLR properties: XAML cannot {Binding} onto a CLR property on a
    // BindableObject - it fails at compile with MAUIX2002, which reads like the property does not
    // exist. Formatting is meant to be reachable from a view model, so these have to be bindable.
    public static readonly BindableProperty TextFormatterProperty = BindableProperty.Create(
        nameof(TextFormatter), typeof(Func<object?, string?>), typeof(DataGridColumn), null);

    public static readonly BindableProperty CellStyleProperty = BindableProperty.Create(
        nameof(CellStyle), typeof(Func<object, DataGridCellStyle?>), typeof(DataGridColumn), null);

    public static readonly BindableProperty HighlightProperty = BindableProperty.Create(
        nameof(Highlight), typeof(DataGridCellStyle), typeof(DataGridColumn), null);

    public static readonly BindableProperty CultureProperty = BindableProperty.Create(
        nameof(Culture), typeof(System.Globalization.CultureInfo), typeof(DataGridColumn), null);

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

    /// <summary>
    /// A display preset - <c>Currency</c>, <c>Percent</c>, <c>Date</c>, <c>FileSize</c>, <c>Boolean</c>,
    /// <c>Enum</c> and friends - so the common cases need neither a format string nor a cell template.
    /// <see cref="StringFormat"/> wins over this when both are set.
    /// </summary>
    public DataGridColumnFormat DisplayAs
    {
        get => (DataGridColumnFormat)this.GetValue(DisplayAsProperty);
        set => this.SetValue(DisplayAsProperty, value);
    }

    /// <summary>
    /// Decimal places for the <c>Number</c>/<c>Currency</c>/<c>Percent</c>/<c>FileSize</c> presets.
    /// <c>null</c> uses the culture default (and, for <c>FileSize</c>, 0 places for bytes and 1 above that).
    /// </summary>
    public int? Decimals
    {
        get => (int?)this.GetValue(DecimalsProperty);
        set => this.SetValue(DecimalsProperty, value);
    }

    /// <summary>Text shown when the value is null or an empty string, e.g. <c>"&#8212;"</c>. Prefix/suffix are not applied to it.</summary>
    public string? NullText
    {
        get => (string?)this.GetValue(NullTextProperty);
        set => this.SetValue(NullTextProperty, value);
    }

    /// <summary>Text placed before the formatted value. Skipped when the value is null (see <see cref="NullText"/>).</summary>
    public string? Prefix
    {
        get => (string?)this.GetValue(PrefixProperty);
        set => this.SetValue(PrefixProperty, value);
    }

    /// <summary>Text placed after the formatted value, e.g. <c>" kg"</c>. Skipped when the value is null.</summary>
    public string? Suffix
    {
        get => (string?)this.GetValue(SuffixProperty);
        set => this.SetValue(SuffixProperty, value);
    }

    /// <summary>Text for <c>true</c> under <see cref="DataGridColumnFormat.Boolean"/>. Defaults to a check glyph.</summary>
    public string? TrueText
    {
        get => (string?)this.GetValue(TrueTextProperty);
        set => this.SetValue(TrueTextProperty, value);
    }

    /// <summary>Text for <c>false</c> under <see cref="DataGridColumnFormat.Boolean"/>. Defaults to a cross glyph.</summary>
    public string? FalseText
    {
        get => (string?)this.GetValue(FalseTextProperty);
        set => this.SetValue(FalseTextProperty, value);
    }

    /// <summary>Horizontal alignment of this column's cells and footer. <c>Auto</c> right-aligns quantities.</summary>
    public DataGridCellAlignment Alignment
    {
        get => (DataGridCellAlignment)this.GetValue(AlignmentProperty);
        set => this.SetValue(AlignmentProperty, value);
    }

    /// <summary>Horizontal alignment of the header. <c>Auto</c> follows <see cref="Alignment"/> so the header sits over its own values.</summary>
    public DataGridCellAlignment HeaderAlignment
    {
        get => (DataGridCellAlignment)this.GetValue(HeaderAlignmentProperty);
        set => this.SetValue(HeaderAlignmentProperty, value);
    }

    /// <summary>Let cell text wrap instead of truncating on one line. Pair with <see cref="MaxLines"/> to cap the height.</summary>
    public bool Wrap
    {
        get => (bool)this.GetValue(WrapProperty);
        set => this.SetValue(WrapProperty, value);
    }

    /// <summary>Maximum wrapped lines before truncating. <c>0</c> means unlimited; only meaningful with <see cref="Wrap"/>.</summary>
    public int MaxLines
    {
        get => (int)this.GetValue(MaxLinesProperty);
        set => this.SetValue(MaxLinesProperty, value);
    }

    /// <summary>
    /// Culture used for formatting this column. <c>null</c> uses <see cref="System.Globalization.CultureInfo.CurrentCulture"/>.
    /// </summary>
    public System.Globalization.CultureInfo? Culture
    {
        get => (System.Globalization.CultureInfo?)this.GetValue(CultureProperty);
        set => this.SetValue(CultureProperty, value);
    }

    /// <summary>
    /// Full control over the cell text without a template: takes the raw value, returns the string to
    /// show. Runs instead of the preset/format string, but <see cref="Prefix"/>/<see cref="Suffix"/> and
    /// the <see cref="NullText"/> placeholder still apply around it.
    /// </summary>
    public Func<object?, string?>? TextFormatter
    {
        get => (Func<object?, string?>?)this.GetValue(TextFormatterProperty);
        set => this.SetValue(TextFormatterProperty, value);
    }

    /// <summary>
    /// Per-cell colour/weight driven by the row item, e.g. red negatives or an amber "overdue" cell.
    /// Return <c>null</c> (or a <see cref="DataGridCellStyle"/> with null members) to keep the themed default.
    /// </summary>
    public Func<object, DataGridCellStyle?>? CellStyle
    {
        get => (Func<object, DataGridCellStyle?>?)this.GetValue(CellStyleProperty);
        set => this.SetValue(CellStyleProperty, value);
    }

    /// <summary>
    /// Highlights the whole column - a fill, a stroke, or both, applied to every one of its cells.
    /// Row-scoped and cell-scoped highlights are laid over it, and the column's own
    /// <see cref="CellStyle"/> wins over all of them.
    /// </summary>
    public DataGridCellStyle? Highlight
    {
        get => (DataGridCellStyle?)this.GetValue(HighlightProperty);
        set => this.SetValue(HighlightProperty, value);
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

    internal virtual string? GetText(object? item) => this.FormatValue(this.GetCellValue(item));

    /// <summary>
    /// The single place a raw value becomes display text. The cell, the quick-filter search index,
    /// group headers and aggregates all come through here so none of them can drift apart - which is
    /// exactly what used to happen when the cell went through a binding <c>StringFormat</c> and
    /// everything else went through <c>IFormattable.ToString</c>.
    /// </summary>
    internal string? FormatValue(object? value)
    {
        if (this.TextFormatter is not null)
        {
            if (value is null)
                return this.NullText;

            var custom = this.TextFormatter(value);
            if (string.IsNullOrEmpty(custom))
                return this.NullText;

            return (this.Prefix ?? string.Empty) + custom + (this.Suffix ?? string.Empty);
        }
        return DataGridValueFormatter.Format(value, this.FormatSpec);
    }

    DataGridFormatSpec FormatSpec => new()
    {
        DisplayAs = this.DisplayAs,
        StringFormat = this.StringFormat,
        Decimals = this.Decimals,
        NullText = this.NullText,
        Prefix = this.Prefix,
        Suffix = this.Suffix,
        TrueText = this.TrueText,
        FalseText = this.FalseText,
        Culture = this.Culture
    };

    internal Type GetDataType(Type itemType)
        => DataGridReflection.GetPropertyType(itemType, this.PropertyName);
}
