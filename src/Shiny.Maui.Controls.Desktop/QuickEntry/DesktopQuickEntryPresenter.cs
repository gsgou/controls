using Microsoft.Extensions.Logging;
using Shiny.Maui.Controls.QuickEntry;

namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// Presents the quick entry popup as a borderless, always-on-top OS window that opens over other
/// applications — the Claude Desktop / Spotlight / Copilot-key behaviour, which an in-app overlay
/// cannot give you because it can only ever draw inside your own app.
/// </summary>
sealed class DesktopQuickEntryPresenter : IQuickEntryPresenter, IDesktopQuickEntryHost, IDisposable
{
    readonly ILogger<DesktopQuickEntryPresenter>? logger;
    readonly SemaphoreSlim gate = new(1, 1);

    Window? window;
    QuickEntryPage? page;
    object? platformWindow;

    public DesktopQuickEntryPresenter(ILogger<DesktopQuickEntryPresenter>? logger = null)
        => this.logger = logger;

    public QuickEntryPresentation Kind => QuickEntryPresentation.Desktop;

    public bool IsSupported => QuickEntryPlatform.IsSupported;

    public Action? Deactivated { get; set; }

    public Func<QuickEntryKey, bool>? KeyPressed { get; set; }

    public Action<double>? ContentHeightChanged { get; set; }

    public void NotifyDeactivated() => this.Deactivated?.Invoke();

    public bool NotifyKey(QuickEntryKey key) => this.KeyPressed?.Invoke(key) ?? false;

    public async Task PrepareAsync(QuickEntryOptions options, View content)
    {
        if (this.platformWindow != null)
        {
            this.SetContent(content);
            return;
        }

        await this.gate.WaitAsync().ConfigureAwait(true);
        try
        {
            if (this.platformWindow != null)
                return;

            var app = Application.Current
                ?? throw new InvalidOperationException("Quick entry needs a running MAUI application.");

            this.page = new QuickEntryPage(content, h => this.ContentHeightChanged?.Invoke(h));
            this.page.SetAutoSize(options.AutoSize);

            var w = new Window(this.page)
            {
                Title = options.WindowTitle,
                Width = options.Width,
                Height = options.CollapsedHeight
            };
            this.window = w;

            // The handler — and with it the native window — is created as a side effect of
            // OpenWindow, and on some heads that happens after the call returns. HandlerChanged is
            // the one signal every head raises, so it is what gates the platform styling.
            var ready = new TaskCompletionSource();
            void OnHandlerChanged(object? sender, EventArgs e)
            {
                if (w.Handler?.PlatformView is { } native)
                {
                    w.HandlerChanged -= OnHandlerChanged;
                    this.platformWindow = native;
                    ready.TrySetResult();
                }
            }
            w.HandlerChanged += OnHandlerChanged;

            app.OpenWindow(w);
            OnHandlerChanged(w, EventArgs.Empty);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await ready.Task.WaitAsync(timeout.Token).ConfigureAwait(true);

            QuickEntryPlatform.Initialize(this, this.platformWindow!, options);
            QuickEntryPlatform.Hide(this.platformWindow!);
        }
        catch (OperationCanceledException)
        {
            this.logger?.LogError("Timed out waiting for the quick entry window handler. The popup will not open on this host.");
            this.Teardown();
        }
        finally
        {
            this.gate.Release();
        }
    }

    public void SetContent(View content) => this.page?.SetHosted(content);

    public void Show(QuickEntryOptions options, double width, double height)
    {
        if (this.platformWindow != null)
            QuickEntryPlatform.Show(this.platformWindow, options, width, height);
    }

    public void Hide()
    {
        if (this.platformWindow != null)
            QuickEntryPlatform.Hide(this.platformWindow);
    }

    public void Resize(QuickEntryOptions options, double width, double height)
    {
        if (this.platformWindow != null)
            QuickEntryPlatform.Resize(this.platformWindow, options, width, height);
    }

    public void Teardown()
    {
        // Release the content first. Switching presentation hands the same view to another presenter,
        // and a view that still has a parent is refused there.
        this.page?.ClearHosted();

        var native = this.platformWindow;
        this.platformWindow = null;
        if (native != null)
            QuickEntryPlatform.Teardown(native);

        var w = this.window;
        this.window = null;
        this.page = null;

        if (w != null)
        {
            try { Application.Current?.CloseWindow(w); }
            catch (Exception ex) { this.logger?.LogDebug(ex, "Closing the quick entry window failed"); }
        }
    }

    public void Dispose()
    {
        this.Teardown();
        this.gate.Dispose();
    }
}
