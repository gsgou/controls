using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// A column bound to a property of <typeparamref name="TItem"/> via an expression, e.g.
/// <c>&lt;PropertyColumn Property="x =&gt; x.FirstName" /&gt;</c>. Drives sorting, filtering, grouping,
/// aggregation, and inline editing automatically.
/// </summary>
public sealed class PropertyColumn<TItem, TProperty> : ColumnBase<TItem>
{
    Func<TItem, TProperty>? getter;
    PropertyInfo? propertyInfo;
    string? memberName;

    [Parameter] public Expression<Func<TItem, TProperty>> Property { get; set; } = default!;

    /// <summary>
    /// Standard .NET format string applied to the value (e.g. "N0", "C2", "d"). Wins over
    /// <see cref="DisplayAs"/> when both are set.
    /// </summary>
    [Parameter] public string? StringFormat { get; set; }

    /// <summary>Former name of <see cref="StringFormat"/>, kept working so existing markup does not break.</summary>
    [Obsolete("Renamed to StringFormat for parity with the MAUI DataGrid. Both still work; Format is honoured when StringFormat is unset.")]
    [Parameter] public string? Format { get; set; }

    /// <summary>
    /// A display preset - <c>Currency</c>, <c>Percent</c>, <c>Date</c>, <c>FileSize</c>, <c>Boolean</c>,
    /// <c>Enum</c> and friends - so the common cases need neither a format string nor a cell template.
    /// </summary>
    [Parameter] public DataGridColumnFormat DisplayAs { get; set; }

    /// <summary>
    /// Decimal places for the <c>Number</c>/<c>Currency</c>/<c>Percent</c>/<c>FileSize</c> presets.
    /// <c>null</c> uses the culture default (and, for <c>FileSize</c>, 0 places for bytes and 1 above that).
    /// </summary>
    [Parameter] public int? Decimals { get; set; }

    /// <summary>Text shown when the value is null or an empty string, e.g. <c>"&#8212;"</c>. Prefix/suffix are not applied to it.</summary>
    [Parameter] public string? NullText { get; set; }

    /// <summary>Text placed before the formatted value. Skipped when the value is null (see <see cref="NullText"/>).</summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>Text placed after the formatted value, e.g. <c>" kg"</c>. Skipped when the value is null.</summary>
    [Parameter] public string? Suffix { get; set; }

    /// <summary>Text for <c>true</c> under <see cref="DataGridColumnFormat.Boolean"/>. Defaults to a check glyph.</summary>
    [Parameter] public string? TrueText { get; set; }

    /// <summary>Text for <c>false</c> under <see cref="DataGridColumnFormat.Boolean"/>. Defaults to a cross glyph.</summary>
    [Parameter] public string? FalseText { get; set; }

    /// <summary>Culture used for formatting. <c>null</c> uses <see cref="CultureInfo.CurrentCulture"/>.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Full control over the cell text without a template: takes the raw value, returns the string to
    /// show. Runs instead of the preset/format string, but <see cref="Prefix"/>/<see cref="Suffix"/> and
    /// the <see cref="NullText"/> placeholder still apply around it.
    /// </summary>
    [Parameter] public Func<TProperty, string?>? TextFormatter { get; set; }

    protected override void OnParametersSet()
    {
        this.getter = this.Property.Compile();
        this.memberName = (this.Property.Body as MemberExpression)?.Member.Name;
        this.propertyInfo = (this.Property.Body as MemberExpression)?.Member as PropertyInfo;
        base.OnParametersSet();
    }

    protected override string ComputeId() => this.memberName ?? this.Title ?? Guid.NewGuid().ToString("N");

    internal override object? GetValue(TItem item)
        => item is null || this.getter is null ? null : this.getter(item);

    internal override DataGridColumnFormat DisplayFormat => this.DisplayAs;

    /// <summary>
    /// The single place a raw value becomes display text. The cell, the quick-filter search index,
    /// group headers and aggregates all come through here so none of them can drift apart.
    /// </summary>
    internal override string? GetText(TItem item) => this.FormatValue(this.GetValue(item));

    internal override string? FormatValue(object? value)
    {
        if (this.TextFormatter is not null)
        {
            if (value is null)
                return this.NullText;

            var custom = this.TextFormatter((TProperty)value);
            if (string.IsNullOrEmpty(custom))
                return this.NullText;

            return (this.Prefix ?? string.Empty) + custom + (this.Suffix ?? string.Empty);
        }
        return DataGridValueFormatter.Format(value, this.FormatSpec);
    }

    DataGridFormatSpec FormatSpec => new()
    {
        DisplayAs = this.DisplayAs,
        StringFormat = this.EffectiveStringFormat,
        Decimals = this.Decimals,
        NullText = this.NullText,
        Prefix = this.Prefix,
        Suffix = this.Suffix,
        TrueText = this.TrueText,
        FalseText = this.FalseText,
        Culture = this.Culture
    };

#pragma warning disable CS0618 // Format is the obsolete alias; reading it here is the whole point of keeping it.
    string? EffectiveStringFormat => string.IsNullOrEmpty(this.StringFormat) ? this.Format : this.StringFormat;
#pragma warning restore CS0618

    internal override void SetValue(TItem item, object? value)
    {
        if (item is null || this.propertyInfo is null || !this.propertyInfo.CanWrite)
            return;

        var target = this.propertyInfo.PropertyType;
        var converted = ConvertValue(value, target);
        this.propertyInfo.SetValue(item, converted);
    }

    internal override Type GetDataType() => Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);

    /// <summary>The column header text — explicit Title, else the humanized property name.</summary>
    internal override string HeaderText => this.Title ?? Humanize(this.memberName) ?? string.Empty;

    static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null)
            return null;
        if (targetType.IsInstanceOfType(value))
            return value;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        try
        {
            if (underlying.IsEnum)
                return value is string s ? Enum.Parse(underlying, s, true) : Enum.ToObject(underlying, value);
            return Convert.ChangeType(value, underlying, System.Globalization.CultureInfo.CurrentCulture);
        }
        catch
        {
            return value;
        }
    }

    static string? Humanize(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
