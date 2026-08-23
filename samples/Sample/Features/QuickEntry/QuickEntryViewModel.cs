using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls.QuickEntry;

namespace Sample.Features.QuickEntry;

[ShellMap<QuickEntryPage>(registerRoute: false)]
public partial class QuickEntryViewModel : ObservableObject
{
    readonly IQuickEntryService? quickEntry;
    readonly object? hotKeys;

    IDisposable? extraHotKey;

    [ObservableProperty] string status = "Popup closed";
    [ObservableProperty] string resolvedPresentation = String.Empty;
    [ObservableProperty] string hotKey = OperatingSystem.IsMacOS() ? "Cmd+Opt+Space" : "Ctrl+Alt+Space";
    [ObservableProperty] string hotKeyStatus = "Not registered";
    [ObservableProperty] bool hotKeysAvailable;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GlowNotSupported))]
    bool glowSupported;
    [ObservableProperty] double glowThickness = 110;
    [ObservableProperty] double glowIntensity = 0.9;

    public bool GlowNotSupported => !this.GlowSupported;

    public string[] Presentations { get; } = { "Auto", "InApp", "Desktop" };
    public string[] Placements { get; } = { "TopCenter", "BottomCenter", "Center", "NearCursor" };
    public string[] GlowTriggers { get; } = { "None", "WhileOpen", "WhileBusy" };

    [ObservableProperty] string selectedPresentation = "Auto";
    [ObservableProperty] string selectedPlacement = "TopCenter";
    [ObservableProperty] string selectedGlowTrigger = "WhileBusy";

    public ObservableCollection<string> EventLog { get; } = new();

    /// <summary>The same control the popup hosts, shown inline so it can be inspected without opening anything.</summary>
    public PromptView Preview { get; } = new()
    {
        Placeholder = "Ask anything…",
        ShowMicrophone = true,
        Suggestions = new List<PromptSuggestion>
        {
            new("Summarise my clipboard", "Reads whatever you last copied", "📋"),
            new("Explain this error", "Paste a stack trace and get a plain-English read", "🐞"),
            new("Draft a reply", "Turns a few bullet points into a message", "✉️")
        }
    };

    public QuickEntryViewModel(IServiceProvider services)
    {
        this.quickEntry = services.GetService(typeof(IQuickEntryService)) as IQuickEntryService;

        // Resolved by name so this page still compiles and runs in a head that does not reference the
        // desktop add-on — global hotkeys only exist there.
        var hotKeyType = Type.GetType("Shiny.Maui.Controls.Desktop.QuickEntry.IGlobalHotKeyService, Shiny.Maui.Controls.Desktop");
        this.hotKeys = hotKeyType == null ? null : services.GetService(hotKeyType);
        this.HotKeysAvailable = this.hotKeys is not null && HotKeySupported(this.hotKeys);

        if (this.quickEntry != null)
        {
            this.GlowSupported = this.quickEntry.IsGlowSupported;
            this.ResolvedPresentation = this.quickEntry.ResolvedPresentation.ToString();
            this.quickEntry.Opened += (_, _) =>
            {
                this.ResolvedPresentation = this.quickEntry.ResolvedPresentation.ToString();
                this.Log("Popup opened");
            };
            this.quickEntry.Closed += (_, _) => this.Log("Popup closed");
            this.quickEntry.Options.HotKey = this.HotKey;

            _ = this.PreloadAndWireAsync();
        }

        Wire(this.Preview, this.Log);
        this.HotKeyStatus = this.HotKeysAvailable
            ? "Available — press Register"
            : "Global hotkeys need the desktop add-on on Windows, macOS or Linux";
    }

    async Task PreloadAndWireAsync()
    {
        if (this.quickEntry == null)
            return;

        try
        {
            // Content is null until the popup has been built, so preload first and wire the popup's
            // own prompt once it exists — otherwise Submitted is never handled and the popup looks
            // like it does nothing.
            await this.quickEntry.PreloadAsync();
            if (this.quickEntry.Content is PromptView prompt)
            {
                prompt.Suggestions = new List<PromptSuggestion>
                {
                    new("Summarise my clipboard", "Reads whatever you last copied", "📋"),
                    new("Explain this error", "Paste a stack trace and get a plain-English read", "🐞"),
                    new("Draft a reply", "Turns a few bullet points into a message", "✉️")
                };
                Wire(prompt, this.Log);
            }
            this.ResolvedPresentation = this.quickEntry.ResolvedPresentation.ToString();
            this.GlowSupported = this.quickEntry.IsGlowSupported;
        }
        catch (Exception ex)
        {
            this.Log($"Preload failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Stands in for a real assistant: flips the busy flag, waits, then drops a response in. This is
    /// the whole integration surface — <c>Submitted</c> in, <c>ResponseContent</c> out.
    /// </summary>
    static void Wire(PromptView prompt, Action<string> log)
    {
        prompt.Submitted += async (_, e) =>
        {
            log($"Submitted: \"{e.Text}\"");
            prompt.ResponseContent = null;
            prompt.IsBusy = true;

            await Task.Delay(1800);

            prompt.IsBusy = false;
            prompt.ResponseContent = new Label
            {
                Text = $"You asked \"{e.Text}\". Wire Submitted to your own IChatClient and put the streamed answer here — a Label, a MarkdownView, a ChatView, whatever fits.",
                FontSize = 14,
                LineHeight = 1.35
            };
        };

        prompt.Cancelled += (_, _) =>
        {
            prompt.IsBusy = false;
            log("Cancelled");
        };
    }

    [RelayCommand]
    void ShowPopup() => this.quickEntry?.Show();

    [RelayCommand]
    void HidePopup() => this.quickEntry?.Hide();

    [RelayCommand]
    void TogglePopup() => this.quickEntry?.Toggle();

    [RelayCommand]
    void RegisterHotKey()
    {
        this.extraHotKey?.Dispose();
        this.extraHotKey = this.hotKeys == null
            ? null
            : RegisterHotKey(this.hotKeys, this.HotKey, () =>
            {
                this.Log($"Hotkey {this.HotKey} pressed");
                this.quickEntry?.Toggle();
            });

        this.HotKeyStatus = this.extraHotKey != null
            ? $"{this.HotKey} is registered — try it from another app"
            : $"{this.HotKey} could not be claimed (another app may already own it)";
        this.Log(this.HotKeyStatus);
    }

    [RelayCommand]
    void ShowGlow() => this.quickEntry?.ShowGlow();

    [RelayCommand]
    void HideGlow() => this.quickEntry?.HideGlow();

    [RelayCommand]
    Task PulseGlow() => this.quickEntry?.PulseGlowAsync(TimeSpan.FromSeconds(3)) ?? Task.CompletedTask;

    partial void OnSelectedPresentationChanged(string value)
    {
        if (this.quickEntry != null && Enum.TryParse<QuickEntryPresentation>(value, out var presentation))
        {
            this.quickEntry.Options.Presentation = presentation;
            this.ResolvedPresentation = this.quickEntry.ResolvedPresentation.ToString();
            this.GlowSupported = this.quickEntry.IsGlowSupported;
            this.Log($"Presentation = {value} (resolves to {this.ResolvedPresentation})");
        }
    }

    partial void OnSelectedPlacementChanged(string value)
    {
        if (this.quickEntry != null && Enum.TryParse<QuickEntryPlacement>(value, out var placement))
            this.quickEntry.Options.Placement = placement;
    }

    partial void OnSelectedGlowTriggerChanged(string value)
    {
        if (this.quickEntry != null && Enum.TryParse<ScreenGlowTrigger>(value, out var trigger))
            this.quickEntry.Options.ScreenGlow = trigger;
    }

    partial void OnGlowThicknessChanged(double value)
    {
        if (this.quickEntry != null)
            this.quickEntry.Options.Glow.Thickness = value;
    }

    partial void OnGlowIntensityChanged(double value)
    {
        if (this.quickEntry != null)
            this.quickEntry.Options.Glow.Intensity = value;
    }

    static bool HotKeySupported(object service)
        => service.GetType().GetProperty("IsSupported")?.GetValue(service) is true;

    static IDisposable? RegisterHotKey(object service, string accelerator, Action pressed)
        => service.GetType().GetMethod("Register")?.Invoke(service, new object[] { accelerator, pressed }) as IDisposable;

    void Log(string message)
    {
        this.Status = message;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            this.EventLog.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
            while (this.EventLog.Count > 20)
                this.EventLog.RemoveAt(this.EventLog.Count - 1);
        });
    }
}
