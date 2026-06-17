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

    /// <summary>Standard .NET format string applied to the value (e.g. "N0", "C2", "d").</summary>
    [Parameter] public string? Format { get; set; }

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

    internal override string? GetText(TItem item)
    {
        var value = this.GetValue(item);
        if (value is null)
            return null;

        if (!string.IsNullOrEmpty(this.Format) && value is IFormattable formattable)
            return formattable.ToString(this.Format, System.Globalization.CultureInfo.CurrentCulture);

        return value.ToString();
    }

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
