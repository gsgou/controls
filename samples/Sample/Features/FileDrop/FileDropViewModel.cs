using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls.Desktop.FileDrop;

namespace Sample.Features.FileDrop;

[ShellMap<FileDropPage>(registerRoute: false)]
public partial class FileDropViewModel : ObservableObject, IDisposable
{
    readonly IFileDropService? drop;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotSupported))]
    bool isSupported;
    [ObservableProperty] string unsupportedMessage = String.Empty;
    [ObservableProperty] bool isEnabled = true;
    [ObservableProperty] bool isDragging;
    [ObservableProperty] string dragStatus = "Drag files anywhere over this window.";
    [ObservableProperty] bool imagesOnly;
    [ObservableProperty] string lastPosition = String.Empty;

    public bool IsNotSupported => !this.IsSupported;

    public ObservableCollection<DroppedFileRow> Files { get; } = new();
    public ObservableCollection<string> EventLog { get; } = new();

    public FileDropViewModel(IServiceProvider services)
    {
        this.drop = services.GetService(typeof(IFileDropService)) as IFileDropService;
        this.IsSupported = this.drop?.IsSupported == true;

        if (!this.IsSupported)
        {
            this.UnsupportedMessage = "Window-level file drop needs a desktop window manager — Windows, macOS (AppKit or Mac Catalyst) or Linux/GTK4. This platform has no file drag at all.";
            return;
        }

        this.drop!.DragEnter += this.OnDragEnter;
        this.drop.DragOver += this.OnDragOver;
        this.drop.DragLeave += this.OnDragLeave;
        this.drop.Dropped += this.OnDropped;
    }

    void OnDragEnter(object? sender, FileDragEventArgs e)
    {
        this.IsDragging = true;
        this.DragStatus = e.HasAcceptableFiles
            ? $"Drop to accept {e.Files.Count} file(s)."
            : "Nothing in this drag is accepted.";
        this.Log($"enter — {e.Files.Count} accepted, {e.RejectedCount} filtered out");
    }

    void OnDragOver(object? sender, FileDragEventArgs e)
        => this.LastPosition = $"{e.Position.X:0}, {e.Position.Y:0}";

    void OnDragLeave(object? sender, FileDragEventArgs e)
    {
        this.IsDragging = false;
        this.DragStatus = "Drag files anywhere over this window.";
        this.Log("leave");
    }

    void OnDropped(object? sender, FileDropEventArgs e)
    {
        this.IsDragging = false;
        this.DragStatus = $"Dropped {e.Files.Count} file(s).";

        foreach (var file in e.Files)
            this.Files.Insert(0, new DroppedFileRow(file));

        this.Log($"drop — {e.Files.Count} file(s) at {e.Position.X:0},{e.Position.Y:0}");
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (this.drop != null)
            this.drop.IsEnabled = value;
    }

    /// <summary>
    /// Shows that the filters are live: the options object the service was built with can be
    /// changed at runtime and the next drag honours it.
    /// </summary>
    partial void OnImagesOnlyChanged(bool value)
    {
        if (this.drop == null)
            return;

        this.drop.Options.AllowedExtensions.Clear();
        if (value)
        {
            foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".heic" })
                this.drop.Options.AllowedExtensions.Add(extension);
        }

        this.Log(value ? "filter — images only" : "filter — all files");
    }

    [RelayCommand]
    void Clear()
    {
        this.Files.Clear();
        this.EventLog.Clear();
    }

    [RelayCommand]
    async Task Preview(DroppedFileRow? row)
    {
        if (row == null)
            return;

        try
        {
            await using var stream = await row.File.OpenReadAsync();
            var buffer = new byte[Math.Min(256, stream.Length <= 0 ? 256 : stream.Length)];
            var read = await stream.ReadAsync(buffer);
            row.Preview = Convert.ToHexString(buffer, 0, Math.Min(read, 32));
        }
        catch (Exception ex)
        {
            row.Preview = ex.Message;
        }
    }

    void Log(string message)
    {
        this.EventLog.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {message}");
        while (this.EventLog.Count > 40)
            this.EventLog.RemoveAt(this.EventLog.Count - 1);
    }

    public void Dispose()
    {
        if (this.drop == null)
            return;

        this.drop.DragEnter -= this.OnDragEnter;
        this.drop.DragOver -= this.OnDragOver;
        this.drop.DragLeave -= this.OnDragLeave;
        this.drop.Dropped -= this.OnDropped;
    }
}


public partial class DroppedFileRow(DroppedFile file) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    string preview = String.Empty;

    public DroppedFile File { get; } = file;
    public string FileName => this.File.FileName;
    public string ContentType => this.File.ContentType;
    public string Path => this.File.FullPath ?? "(no path — staged from an item provider)";
    public string Size => this.File.Length < 0 ? "unknown" : $"{this.File.Length:N0} bytes";
    public string Details => $"{this.ContentType} · {this.Size}";
    public bool HasPreview => !String.IsNullOrEmpty(this.Preview);
}
