namespace Shiny.Blazor.Controls;

/// <summary>
/// A column whose cells are rendered entirely by a <c>CellTemplate</c> (and optional
/// <c>EditTemplate</c>) — not bound to a single property. Not sortable/filterable by default.
/// </summary>
public sealed class TemplateColumn<TItem> : ColumnBase<TItem>
{
    internal override bool HasValue => false;

    protected override string ComputeId() => this.Title ?? Guid.NewGuid().ToString("N");

    internal override object? GetValue(TItem item) => null;

    internal override string? GetText(TItem item) => null;
}
