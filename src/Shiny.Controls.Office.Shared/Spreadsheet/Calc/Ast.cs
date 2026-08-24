namespace Shiny.Controls.Office.Spreadsheet.Calc;

public abstract record FormulaNode;

public sealed record LiteralNode(CellValue Value) : FormulaNode;

/// <summary>A reference to a single cell, optionally on another sheet.</summary>
public sealed record ReferenceNode(string? Sheet, CellRef Cell) : FormulaNode;

/// <summary>A rectangular reference. <c>Sheet</c> applies to the whole range.</summary>
public sealed record RangeNode(string? Sheet, CellRange Range) : FormulaNode;

/// <summary>A name that resolved to nothing the engine knows about — evaluates to #NAME?.</summary>
public sealed record UnknownNameNode(string Name) : FormulaNode;

public enum UnaryOperator
{
    Negate,
    Plus,
    Percent
}

public sealed record UnaryNode(UnaryOperator Operator, FormulaNode Operand) : FormulaNode;

public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Power,
    Concat,
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual
}

public sealed record BinaryNode(BinaryOperator Operator, FormulaNode Left, FormulaNode Right) : FormulaNode;

public sealed record FunctionNode(string Name, IReadOnlyList<FormulaNode> Arguments) : FormulaNode;

/// <summary>
/// An argument position left empty, as in <c>IF(A1,,"no")</c>. Excel treats this as zero or empty
/// depending on the function, so it has to survive parsing as its own thing rather than becoming a literal.
/// </summary>
public sealed record MissingArgumentNode : FormulaNode
{
    public static readonly MissingArgumentNode Instance = new();
}
