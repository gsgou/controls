using Shiny.Controls.Office.Editing;
using Shiny.Controls.Office.Spreadsheet.Calc;

namespace Shiny.Controls.Office.Spreadsheet.Commands;

/// <summary>Adds an empty sheet at a position in the tab order.</summary>
public sealed class AddSheetCommand(string sheetName, int index) : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public int Index { get; } = index;

    public string Name => "Add Sheet";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        context.InsertSheet(this.SheetName, this.Index, worksheetXml: null, visible: true);
        return new DeleteSheetCommand(this.SheetName);
    }
}

/// <summary>Removes a sheet and everything on it.</summary>
public sealed class DeleteSheetCommand(string sheetName) : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;

    public string Name => "Delete Sheet";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var snapshot = context.RemoveSheet(this.SheetName);
        return new RestoreSheetCommand(snapshot);
    }
}

/// <summary>
/// Puts a deleted sheet back where it was, with its contents.
/// </summary>
/// <remarks>
/// Only ever produced as the inverse of <see cref="DeleteSheetCommand"/>, but public for the same
/// reason every other command is: an undo stack that can be serialised cannot hold a type its reader
/// is not allowed to name.
/// </remarks>
public sealed class RestoreSheetCommand(SheetSnapshot sheet) : IEditCommand<Workbook>
{
    public SheetSnapshot Sheet { get; } = sheet;

    public string Name => "Delete Sheet";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        context.InsertSheet(this.Sheet.Name, this.Sheet.Index, this.Sheet.Xml, this.Sheet.IsVisible);
        return new DeleteSheetCommand(this.Sheet.Name);
    }
}

/// <summary>
/// Renames a sheet, rewriting every formula and defined name that pointed at the old name.
/// </summary>
public sealed class RenameSheetCommand(string sheetName, string newName) : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public string NewName { get; } = newName;

    public string Name => "Rename Sheet";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        context.RenameSheet(this.SheetName, this.NewName);
        return new RenameSheetCommand(this.NewName, this.SheetName);
    }
}

/// <summary>Moves a sheet to a different position in the tab order.</summary>
public sealed class MoveSheetCommand(string sheetName, int index) : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public int Index { get; } = index;

    public string Name => "Move Sheet";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var previous = -1;
        for (var i = 0; i < context.Sheets.Count && previous < 0; i++)
        {
            if (string.Equals(context.Sheets[i].Name, this.SheetName, StringComparison.OrdinalIgnoreCase))
                previous = i;
        }

        if (previous < 0)
            throw new KeyNotFoundException($"No sheet named '{this.SheetName}'.");

        context.MoveSheet(this.SheetName, this.Index);
        return new MoveSheetCommand(this.SheetName, previous);
    }
}

/// <summary>Hides or shows a sheet.</summary>
public sealed class SetSheetVisibilityCommand(string sheetName, bool visible) : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public bool Visible { get; } = visible;

    public string Name => this.Visible ? "Show Sheet" : "Hide Sheet";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var previous = context[this.SheetName].IsVisible;
        context.SetSheetVisibility(this.SheetName, this.Visible);
        return new SetSheetVisibilityCommand(this.SheetName, previous);
    }
}

/// <summary>
/// Copies a sheet, contents and all, under a new name.
/// </summary>
/// <remarks>
/// References the source made to itself by name — <c>Sales!B2</c> written on <c>Sales</c> — are
/// repointed at the copy, which is what Excel does and what anyone dragging a tab expects: the copy
/// should compute from its own numbers, not go on reading the original's.
/// </remarks>
public sealed class DuplicateSheetCommand(string sheetName, string newName, int index) : IEditCommand<Workbook>
{
    public string SheetName { get; } = sheetName;
    public string NewName { get; } = newName;
    public int Index { get; } = index;

    public string Name => "Duplicate Sheet";

    public IEditCommand<Workbook> Apply(Workbook context)
    {
        var source = context[this.SheetName];
        var xml = source.Part.Worksheet?.OuterXml
            ?? throw new InvalidOperationException($"Sheet '{this.SheetName}' has no content to copy.");

        var copy = context.InsertSheet(this.NewName, this.Index, xml, source.IsVisible);
        if (copy.RewriteFormulas(text => FormulaSheetRenamer.Rename(text, source.Name, this.NewName)))
            context.Recalculate();

        return new DeleteSheetCommand(this.NewName);
    }
}
