namespace Shiny.Blazor.Controls.FileDrop;

/// <summary>
/// The DI-resolved handler for drops. Register one with <c>AddShinyFileDrop&lt;T&gt;()</c>.
/// </summary>
/// <remarks>
/// <para>
/// The alternative is <see cref="IFileDropService.Dropped"/>, and the two do the same job from
/// different places. Use the event when the handling belongs to a component. Use this when it
/// belongs to the app — an import that should work from any page, with constructor-injected
/// services rather than whatever the current component captured.
/// </para>
/// <para>
/// Scoped, like the service, so on Blazor Server it is per-circuit and may safely hold per-user
/// state. It runs before <see cref="IFileDropService.Dropped"/> and can consume the drop by setting
/// <see cref="FileDropContext.Handled"/>.
/// </para>
/// </remarks>
public interface IFileDropDelegate
{
    /// <summary>Called once per drop, before <see cref="IFileDropService.Dropped"/> is raised.</summary>
    Task OnFilesDropped(FileDropContext context);
}


/// <summary>What an <see cref="IFileDropDelegate"/> is given, and what it can say back.</summary>
public class FileDropContext(FileDropEventArgs args)
{
    /// <summary>The drop.</summary>
    public FileDropEventArgs Args { get; } = args;

    /// <summary>The accepted files — shorthand for <c>Args.Files</c>.</summary>
    public IReadOnlyList<DroppedFile> Files => this.Args.Files;

    /// <summary>
    /// Set to true to consume the drop, which stops <see cref="IFileDropService.Dropped"/> from
    /// being raised for it.
    /// </summary>
    public bool Handled { get; set; }
}
