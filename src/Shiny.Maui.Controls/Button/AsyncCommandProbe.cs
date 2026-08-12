using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows.Input;

namespace Shiny.Maui.Controls;

/// <summary>
/// Finds the <c>Task</c> an async command is running, so a <see cref="ShinyButton"/> can stay busy
/// for exactly as long as the command does.
/// </summary>
/// <remarks>
/// <para><see cref="ICommand.Execute"/> returns <c>void</c>, so a button given an async command has
/// no handle on the work it just started. Every async command implementation solves that the same
/// way — MVVM Toolkit's <c>IAsyncRelayCommand</c>, Prism's, ReactiveUI's and most hand-rolled ones
/// all expose an <c>ExecutionTask</c> or an <c>IsRunning</c>/<c>IsExecuting</c> flag — but there is
/// no shared interface to type against.</para>
/// <para>Rather than take a dependency on one MVVM framework in the core controls package (which
/// would put it in every consumer's app, whichever framework they actually use), the shape is
/// discovered once per command type and cached. A command that exposes none of it simply isn't an
/// async command as far as the button is concerned, and <see cref="ShinyButton.State"/> is left
/// entirely to the caller.</para>
/// <para>This is the same trade-off — and the same suppression — as
/// <c>DataGridReflection</c>. Consumers targeting full trimming can drive
/// <see cref="ShinyButton.State"/> from their view model instead and turn
/// <see cref="ShinyButton.AutoBusy"/> off.</para>
/// </remarks>
static class AsyncCommandProbe
{
    // Checked in order. ExecutionTask first: a Task tells us when the work actually finished, where
    // a bool only tells us it changed at some point we'd have to poll for.
    static readonly string[] TaskProperties = ["ExecutionTask", "Execution", "CurrentTask"];
    static readonly string[] FlagProperties = ["IsRunning", "IsExecuting", "IsBusy"];

    static readonly ConcurrentDictionary<Type, Shape> Cache = new();

    readonly record struct Shape(PropertyInfo? Task, PropertyInfo? Flag)
    {
        public bool IsAsync => this.Task is not null || this.Flag is not null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Probes well-known async-command members by name so no MVVM framework has to be referenced; set AutoBusy=false and drive State yourself for full-trim scenarios.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Probes well-known async-command members by name so no MVVM framework has to be referenced; set AutoBusy=false and drive State yourself for full-trim scenarios.")]
    static Shape GetShape(Type type) => Cache.GetOrAdd(type, t =>
    {
        PropertyInfo? task = null;
        PropertyInfo? flag = null;

        foreach (var name in TaskProperties)
        {
            var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (pi is not null && typeof(Task).IsAssignableFrom(pi.PropertyType))
            {
                task = pi;
                break;
            }
        }

        foreach (var name in FlagProperties)
        {
            var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (pi is not null && pi.PropertyType == typeof(bool))
            {
                flag = pi;
                break;
            }
        }

        return new Shape(task, flag);
    });

    /// <summary>Whether this command looks like an async command at all.</summary>
    public static bool IsAsync(ICommand? command)
        => command is not null && GetShape(command.GetType()).IsAsync;

    /// <summary>
    /// The task the command is currently running, or null if it isn't running one (or doesn't
    /// expose it).
    /// </summary>
    /// <remarks>
    /// Read <em>after</em> <see cref="ICommand.Execute"/>, so the property has been assigned. A
    /// command exposing only a flag returns <see cref="Task.CompletedTask"/> when idle and null
    /// while running, which the button reads as "busy, but tell me when it's over some other way" —
    /// see <see cref="IsRunning"/>.
    /// </remarks>
    public static Task? GetExecutionTask(ICommand? command)
    {
        if (command is null)
            return null;

        var shape = GetShape(command.GetType());
        return shape.Task?.GetValue(command) as Task;
    }

    /// <summary>
    /// Whether the command reports itself as running, or null if it doesn't expose a flag.
    /// </summary>
    public static bool? IsRunning(ICommand? command)
    {
        if (command is null)
            return null;

        var shape = GetShape(command.GetType());
        return shape.Flag?.GetValue(command) as bool?;
    }
}
