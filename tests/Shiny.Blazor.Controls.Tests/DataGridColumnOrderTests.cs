using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// Drag-and-drop column ordering. The rule that matters is which <i>side</i> of the target a column
/// lands on: inserting before the target unconditionally made dragging a column one place to the right
/// a no-op — removing it and re-inserting it in front of its own right-hand neighbour puts it back
/// exactly where it started, so the header simply refused to move.
/// </summary>
public class DataGridColumnOrderTests
{
    class Row
    {
        public string Name { get; set; } = "";
    }

    static readonly string[] Columns = { "A", "B", "C", "D" };

    static string Drop(string dragged, string target)
        => string.Join(",", DataGrid<Row>.Reorder(Columns, dragged, target));

    [Fact]
    public void DraggingRightLandsAfterTheTarget()
        => Drop("A", "C").ShouldBe("B,C,A,D");

    [Fact]
    public void DraggingLeftLandsBeforeTheTarget()
        => Drop("D", "B").ShouldBe("A,D,B,C");

    /// <summary>The one-step case the always-insert-before bug swallowed entirely.</summary>
    [Fact]
    public void DraggingOnePlaceRightActuallyMovesTheColumn()
        => Drop("A", "B").ShouldBe("B,A,C,D");

    [Fact]
    public void DraggingOnePlaceLeftMovesItTheOtherWay()
        => Drop("B", "A").ShouldBe("B,A,C,D");

    [Fact]
    public void DroppingAColumnOnItselfChangesNothing()
        => Drop("B", "B").ShouldBe("A,B,C,D");

    [Fact]
    public void DraggingToEitherEndWorks()
    {
        Drop("A", "D").ShouldBe("B,C,D,A");
        Drop("D", "A").ShouldBe("D,A,B,C");
    }

    /// <summary>An unknown id is a stale drag, not a reason to shuffle the columns.</summary>
    [Fact]
    public void AnUnknownColumnLeavesTheOrderAlone()
    {
        Drop("gone", "B").ShouldBe("A,B,C,D");
        Drop("A", "gone").ShouldBe("B,C,D,A");
    }

    // ---- which edge the drop marker sits on ----

    [Fact]
    public void TheMarkerSitsOnTheTrailingEdgeWhenMovingRight()
        => DataGrid<Row>.DropsAfter(Columns, "A", "C").ShouldBeTrue();

    [Fact]
    public void TheMarkerSitsOnTheLeadingEdgeWhenMovingLeft()
        => DataGrid<Row>.DropsAfter(Columns, "D", "B").ShouldBeFalse();

    // ---- opt-in ----

    [Fact]
    public void ReorderingIsOffUntilTheGridAsksForIt()
    {
        new DataGrid<Row>().CanReorder.ShouldBeFalse();
        new DataGrid<Row> { DragDropColumnReordering = true }.CanReorder.ShouldBeTrue();
    }
}
