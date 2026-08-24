namespace Shiny.Controls.Office.Editing;

/// <summary>
/// Groups several commands into one undo step. Applied in order; the inverse runs in reverse order,
/// which is what makes nested groups compose correctly.
/// </summary>
public sealed class CompositeCommand<TContext> : IEditCommand<TContext>
{
    readonly IReadOnlyList<IEditCommand<TContext>> commands;

    public CompositeCommand(string name, IReadOnlyList<IEditCommand<TContext>> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        this.Name = name;
        this.commands = commands;
    }

    public string Name { get; }

    public int Count => this.commands.Count;

    public IEditCommand<TContext> Apply(TContext context)
    {
        var inverses = new IEditCommand<TContext>[this.commands.Count];

        // Fill back-to-front so the inverse list is already in reverse order.
        for (var i = 0; i < this.commands.Count; i++)
            inverses[this.commands.Count - 1 - i] = this.commands[i].Apply(context);

        return new CompositeCommand<TContext>(this.Name, inverses);
    }
}
