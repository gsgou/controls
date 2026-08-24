namespace Shiny.Controls.Office.Editing;

/// <summary>
/// A single reversible edit.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Apply"/> returns its own inverse rather than the command exposing a separate
/// <c>Invert()</c>. Most edits cannot describe their inverse until they run — replacing a cell value
/// needs the value that was there — and computing the inverse at apply time is what keeps commands
/// immutable, and therefore safe to serialise, replay and coalesce.
/// </para>
/// <para>
/// Redo falls out of the same rule: undoing applies the inverse, which itself returns an inverse,
/// and that is the redo command.
/// </para>
/// </remarks>
public interface IEditCommand<TContext>
{
    /// <summary>Short human-readable label, used for "Undo {Name}" in menus.</summary>
    string Name { get; }

    /// <summary>Applies the edit and returns the command that reverses it.</summary>
    IEditCommand<TContext> Apply(TContext context);
}

/// <summary>
/// Implemented by commands that can absorb an immediately following command into a single undo step —
/// so a run of typed characters undoes as one action rather than one per keystroke.
/// </summary>
public interface IMergeableCommand<TContext> : IEditCommand<TContext>
{
    /// <summary>
    /// Attempts to combine this command with <paramref name="next"/>, which has not yet been applied.
    /// Returns false when the two are unrelated, which ends the coalescing run.
    /// </summary>
    bool TryMerge(IEditCommand<TContext> next, out IEditCommand<TContext> merged);
}
