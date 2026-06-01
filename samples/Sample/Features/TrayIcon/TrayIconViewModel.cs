using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using Shiny;
using Shiny.Maui.Controls.TrayIcon;

namespace Sample.Features.TrayIcon;

[ShellMap<TrayIconPage>(registerRoute: false)]
public partial class TrayIconViewModel : ObservableObject
{
    readonly ITrayIconFactory? factory;
    Shiny.Maui.Controls.TrayIcon.ITrayIcon? icon;

    [ObservableProperty] string status = "Not running";
    [ObservableProperty] string tooltip = "Shiny Controls Sample";
    [ObservableProperty] string title = string.Empty;
    [ObservableProperty] bool isTemplate;
    [ObservableProperty] bool isVisible = true;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRunning))]
    bool isRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotSupported))]
    bool isSupported;

    public bool IsNotRunning => !this.IsRunning;
    public bool IsNotSupported => !this.IsSupported;
    [ObservableProperty] string unsupportedMessage = string.Empty;
    [ObservableProperty] bool notificationsEnabled = true;
    [ObservableProperty] int counter;

    public ObservableCollection<string> EventLog { get; } = new();

    public TrayIconViewModel(IServiceProvider services)
    {
        this.factory = services.GetService(typeof(ITrayIconFactory)) as ITrayIconFactory;
        this.IsSupported = OperatingSystem.IsWindows()
            || OperatingSystem.IsMacCatalyst()
            || OperatingSystem.IsMacOS()
            || OperatingSystem.IsLinux();
        if (!this.IsSupported)
            this.UnsupportedMessage = "System tray icons are only available on Windows, macOS (AppKit / MacCatalyst), and Linux. This platform doesn't have a system tray.";
    }

    partial void OnTooltipChanged(string value) { if (this.icon != null) this.icon.Tooltip = value; }
    partial void OnTitleChanged(string value) { if (this.icon != null) this.icon.Title = value; }
    partial void OnIsTemplateChanged(bool value) { if (this.icon != null) this.icon.IsTemplateImage = value; }
    partial void OnIsVisibleChanged(bool value) { if (this.icon != null) this.icon.IsVisible = value; }

    [RelayCommand]
    void Start()
    {
        if (this.factory == null || this.icon != null) return;
        try
        {
            this.icon = this.factory.Create();
            this.icon.SetIcon(() => OpenLogoStream());
            this.icon.Tooltip = this.Tooltip;
            this.icon.Title = this.Title;
            this.icon.IsTemplateImage = this.IsTemplate;
            this.icon.IsVisible = this.IsVisible;
            this.icon.SetMenu(this.BuildMenu());
            this.icon.PrimaryClick += (_, e) => this.LogEvent($"Primary click @ {e.X},{e.Y}");
            this.icon.SecondaryClick += (_, e) => this.LogEvent($"Secondary click @ {e.X},{e.Y}");
            this.icon.DoubleClick += (_, e) => this.LogEvent($"Double click @ {e.X},{e.Y}");
            this.IsRunning = true;
            this.Status = "Running — check your system tray / menu bar";
            this.LogEvent("Tray icon created");
        }
        catch (Exception ex)
        {
            this.Status = "Failed: " + ex.Message;
            this.LogEvent("Error: " + ex.Message);
        }
    }

    [RelayCommand]
    void Stop()
    {
        if (this.icon == null) return;
        this.icon.Dispose();
        this.icon = null;
        this.IsRunning = false;
        this.Status = "Stopped";
        this.LogEvent("Tray icon disposed");
    }

    [RelayCommand]
    void Increment()
    {
        this.Counter++;
        if (this.icon != null)
        {
            this.icon.Tooltip = $"Counter: {this.Counter}";
            this.LogEvent($"Counter → {this.Counter}");
        }
    }

    [RelayCommand]
    void ToggleVisibility() => this.IsVisible = !this.IsVisible;

    [RelayCommand]
    void RebuildMenu()
    {
        if (this.icon == null) return;
        this.icon.SetMenu(this.BuildMenu());
        this.LogEvent("Menu rebuilt");
    }

    [RelayCommand]
    void ClearLog() => this.EventLog.Clear();

    TrayMenu BuildMenu() => TrayMenu.Build(b => b
        .Item("Increment counter", this.Increment)
        .Check("Notifications", this.NotificationsEnabled, v =>
        {
            this.NotificationsEnabled = v;
            this.LogEvent("Notifications " + (v ? "enabled" : "disabled"));
        })
        .Separator()
        .Submenu("Status", s => s
            .Item("Available", () => this.LogEvent("Status → Available"))
            .Item("Busy", () => this.LogEvent("Status → Busy"))
            .Item("Away", () => this.LogEvent("Status → Away")))
        .Separator()
        .Item("About", () => this.LogEvent("About clicked"))
        .Item("Quit sample tray", this.Stop));

    void LogEvent(string message)
    {
        // Tray menu callbacks fire on the platform UI thread, and
        // MAUI MainThread is not wired up on the macOS AppKit host,
        // so update the collection directly.
        this.EventLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        while (this.EventLog.Count > 30) this.EventLog.RemoveAt(this.EventLog.Count - 1);
    }

    static Stream OpenLogoStream()
        => FileSystem.OpenAppPackageFileAsync("trayicon.png").GetAwaiter().GetResult();
}
