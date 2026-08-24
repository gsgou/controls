namespace Shiny.Controls.Office.Editing;

/// <summary>
/// Transactional undo/redo over <see cref="IEditCommand{TContext}"/>.
/// </summary>
public sealed class UndoStack<TContext>
{
    readonly List<IEditCommand<TContext>> undo = new();
    readonly List<IEditCommand<TContext>> redo = new();

    /// <summary>Inverses captured while a transaction is open, in the order the commands ran.</summary>
    readonly List<IEditCommand<TContext>> transactionInverses = new();

    readonly TContext context;
    readonly int limit;

    bool transactionOpen;
    string transactionName = string.Empty;
    bool coalescingAllowed;

    /// <summary>The last command applied outside a transaction, kept so the next one can try to merge with it.</summary>
    IEditCommand<TContext>? lastApplied;

    /// <summary>Scratch slot for the merge out-parameter; never read outside Execute.</summary>
    IEditCommand<TContext>? merged;

    public UndoStack(TContext context, int limit = 500)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        this.context = context;
        this.limit = limit;
    }

    public bool CanUndo => this.undo.Count > 0;
    public bool CanRedo => this.redo.Count > 0;

    public string? UndoName => this.CanUndo ? this.undo[^1].Name : null;
    public string? RedoName => this.CanRedo ? this.redo[^1].Name : null;

    /// <summary>Raised after any operation that changes what undo or redo would do.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Applies a command and records its inverse. Any pending redo history is discarded, since the
    /// branch it belonged to no longer exists.
    /// </summary>
    public void Execute(IEditCommand<TContext> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (this.transactionOpen)
        {
            this.transactionInverses.Add(command.Apply(this.context));
            return;
        }

        // Fold this into the previous step where possible, so a typing run undoes as one action rather
        // than one per keystroke.
        var merging = this.coalescingAllowed &&
            this.undo.Count > 0 &&
            this.lastApplied is IMergeableCommand<TContext> mergeable &&
            mergeable.TryMerge(command, out this.merged);

        var inverse = command.Apply(this.context);

        if (merging)
        {
            // The stack holds inverses, and the one already on top only rewinds as far as the run had
            // got when it was pushed - for a typing run, that is a single character. Undoing the whole
            // run means undoing this increment and then everything before it, newest first, which is
            // exactly a composite. Keeping the old inverse alone rewinds one keystroke; replacing it
            // with the new one rewinds only the last.
            this.undo[^1] = new CompositeCommand<TContext>(
                this.undo[^1].Name,
                [inverse, this.undo[^1]]);

            this.lastApplied = this.merged;
        }
        else
        {
            this.Push(inverse);
            this.lastApplied = command;
            this.coalescingAllowed = true;
        }

        this.merged = null;
        this.redo.Clear();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Ends the current coalescing run, so the next command starts a fresh undo step even if it
    /// would otherwise merge. Call this on selection changes, focus loss, and save.
    /// </summary>
    public void BreakCoalescing()
    {
        this.coalescingAllowed = false;
        this.lastApplied = null;
    }

    /// <summary>
    /// Opens a group. Every command executed until the returned scope is disposed becomes a single
    /// undo step. Groups do not nest — opening one while another is open throws.
    /// </summary>
    public IDisposable BeginTransaction(string name)
    {
        if (this.transactionOpen)
            throw new InvalidOperationException("A transaction is already open; nested transactions are not supported.");

        this.transactionOpen = true;
        this.transactionName = name;
        this.transactionInverses.Clear();
        this.coalescingAllowed = false;
        this.lastApplied = null;
        return new TransactionScope(this);
    }

    void CommitTransaction()
    {
        this.transactionOpen = false;
        var name = this.transactionName;
        this.transactionName = string.Empty;

        if (this.transactionInverses.Count == 0)
            return;

        // Inverses were captured in apply order; a composite must undo in reverse.
        var inverses = new IEditCommand<TContext>[this.transactionInverses.Count];
        for (var i = 0; i < this.transactionInverses.Count; i++)
            inverses[this.transactionInverses.Count - 1 - i] = this.transactionInverses[i];

        this.transactionInverses.Clear();
        this.Push(new CompositeCommand<TContext>(name, inverses));
        this.redo.Clear();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!this.CanUndo)
            return;

        var command = this.undo[^1];
        this.undo.RemoveAt(this.undo.Count - 1);
        this.redo.Add(command.Apply(this.context));
        this.coalescingAllowed = false;
        this.lastApplied = null;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!this.CanRedo)
            return;

        var command = this.redo[^1];
        this.redo.RemoveAt(this.redo.Count - 1);
        this.undo.Add(command.Apply(this.context));
        this.coalescingAllowed = false;
        this.lastApplied = null;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        this.undo.Clear();
        this.redo.Clear();
        this.coalescingAllowed = false;
        this.lastApplied = null;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    void Push(IEditCommand<TContext> inverse)
    {
        this.undo.Add(inverse);
        if (this.undo.Count > this.limit)
            this.undo.RemoveAt(0);
    }

    sealed class TransactionScope(UndoStack<TContext> owner) : IDisposable
    {
        bool disposed;

        public void Dispose()
        {
            if (this.disposed)
                return;

            this.disposed = true;
            owner.CommitTransaction();
        }
    }
}
