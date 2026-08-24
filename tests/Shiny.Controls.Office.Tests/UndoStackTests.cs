using Shiny.Controls.Office.Editing;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class UndoStackTests
{
    sealed class Box
    {
        public string Text { get; set; } = string.Empty;
    }

    sealed class SetText(string value) : IEditCommand<Box>
    {
        public string Name => "Set";

        public IEditCommand<Box> Apply(Box context)
        {
            var previous = context.Text;
            context.Text = value;
            return new SetText(previous);
        }
    }

    sealed class AppendText(string suffix) : IMergeableCommand<Box>
    {
        public string Name => "Type";

        public IEditCommand<Box> Apply(Box context)
        {
            var previous = context.Text;
            context.Text += suffix;
            return new SetText(previous);
        }

        public bool TryMerge(IEditCommand<Box> next, out IEditCommand<Box> merged)
        {
            merged = this;
            return next is AppendText;
        }
    }

    [Fact]
    public void UndoAndRedo_WalkTheHistoryBothWays()
    {
        var box = new Box();
        var stack = new UndoStack<Box>(box);

        stack.Execute(new SetText("one"));
        stack.BreakCoalescing();
        stack.Execute(new SetText("two"));

        box.Text.ShouldBe("two");

        stack.Undo();
        box.Text.ShouldBe("one");

        stack.Undo();
        box.Text.ShouldBe(string.Empty);
        stack.CanUndo.ShouldBeFalse();

        stack.Redo();
        box.Text.ShouldBe("one");
        stack.Redo();
        box.Text.ShouldBe("two");
        stack.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void ExecutingAfterUndo_DiscardsTheRedoBranch()
    {
        var box = new Box();
        var stack = new UndoStack<Box>(box);

        stack.Execute(new SetText("a"));
        stack.BreakCoalescing();
        stack.Execute(new SetText("b"));
        stack.Undo();

        stack.CanRedo.ShouldBeTrue();
        stack.Execute(new SetText("c"));

        stack.CanRedo.ShouldBeFalse("the branch redo pointed at no longer exists");
        box.Text.ShouldBe("c");
    }

    [Fact]
    public void MergeableCommands_CollapseIntoOneUndoStep()
    {
        var box = new Box();
        var stack = new UndoStack<Box>(box);

        stack.Execute(new AppendText("h"));
        stack.Execute(new AppendText("i"));
        stack.Execute(new AppendText("!"));

        box.Text.ShouldBe("hi!");

        stack.Undo();

        box.Text.ShouldBe(string.Empty, "a typing run must undo as one action");
        stack.CanUndo.ShouldBeFalse();
    }

    [Fact]
    public void BreakCoalescing_StartsAFreshUndoStep()
    {
        var box = new Box();
        var stack = new UndoStack<Box>(box);

        stack.Execute(new AppendText("ab"));
        stack.BreakCoalescing();
        stack.Execute(new AppendText("cd"));

        stack.Undo();
        box.Text.ShouldBe("ab");
    }

    [Fact]
    public void Transaction_GroupsCommandsAndUndoesInReverse()
    {
        var box = new Box();
        var stack = new UndoStack<Box>(box);

        stack.Execute(new SetText("start"));
        stack.BreakCoalescing();

        using (stack.BeginTransaction("Batch"))
        {
            stack.Execute(new SetText("one"));
            stack.Execute(new SetText("two"));
            stack.Execute(new SetText("three"));
        }

        box.Text.ShouldBe("three");
        stack.UndoName.ShouldBe("Batch");

        stack.Undo();
        box.Text.ShouldBe("start", "the whole group reverses as one step");
    }

    [Fact]
    public void EmptyTransaction_DoesNotCreateAnUndoStep()
    {
        var box = new Box();
        var stack = new UndoStack<Box>(box);

        using (stack.BeginTransaction("Nothing"))
        {
        }

        stack.CanUndo.ShouldBeFalse();
    }

    [Fact]
    public void NestedTransactions_AreRejected()
    {
        var stack = new UndoStack<Box>(new Box());
        using var outer = stack.BeginTransaction("Outer");

        Should.Throw<InvalidOperationException>(() => stack.BeginTransaction("Inner"));
    }

    [Fact]
    public void HistoryIsBounded()
    {
        var box = new Box();
        var stack = new UndoStack<Box>(box, limit: 3);

        for (var i = 0; i < 10; i++)
        {
            stack.Execute(new SetText(i.ToString()));
            stack.BreakCoalescing();
        }

        var undos = 0;
        while (stack.CanUndo)
        {
            stack.Undo();
            undos++;
        }

        undos.ShouldBe(3);
    }
}
