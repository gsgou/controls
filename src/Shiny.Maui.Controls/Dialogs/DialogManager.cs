using System.Collections.Concurrent;
using Microsoft.Maui.Layouts;

namespace Shiny.Maui.Controls.Dialogs;

sealed class DialogManager
{
    static readonly ConcurrentDictionary<Window, DialogManager> Instances = new();

    readonly AbsoluteLayout overlay;
    readonly Queue<(DialogConfig Config, DialogOptions Options, TaskCompletionSource<DialogOutcome> Tcs)> queue = new();
    bool isProcessingQueue;

    DialogManager(AbsoluteLayout overlay) => this.overlay = overlay;

    public static Task<DialogOutcome> ShowAsync(DialogConfig config, DialogOptions options)
    {
        var window = Application.Current?.Windows.FirstOrDefault()
            ?? throw new InvalidOperationException("No active window found. Dialogs require an active MAUI window.");

        var manager = Instances.GetOrAdd(window, CreateManager);
        return manager.EnqueueAsync(config, options);
    }

    static DialogManager CreateManager(Window window)
    {
        var overlay = new AbsoluteLayout
        {
            InputTransparent = true,
            CascadeInputTransparent = false,
            ZIndex = 10_000
        };

        var page = window.Page
            ?? throw new InvalidOperationException("Window has no Page. Dialogs require an active page.");

        var targetPage = GetLeafPage(page);

        if (targetPage.Content is View existingContent)
        {
            var grid = new Grid();
            targetPage.Content = null;
            grid.Children.Add(existingContent);
            grid.Children.Add(overlay);
            targetPage.Content = grid;
        }
        else
        {
            var grid = new Grid();
            grid.Children.Add(overlay);
            targetPage.Content = grid;
        }

        return new DialogManager(overlay);
    }

    static ContentPage GetLeafPage(Page page) => page switch
    {
        ContentPage cp => cp,
        NavigationPage np when np.CurrentPage is not null => GetLeafPage(np.CurrentPage),
        Shell shell when shell.CurrentPage is not null => GetLeafPage(shell.CurrentPage),
        TabbedPage tp when tp.CurrentPage is not null => GetLeafPage(tp.CurrentPage),
        FlyoutPage fp when fp.Detail is not null => GetLeafPage(fp.Detail),
        _ => throw new InvalidOperationException(
            $"Cannot find a ContentPage to host a Dialog. Current page type: {page.GetType().Name}")
    };

    Task<DialogOutcome> EnqueueAsync(DialogConfig config, DialogOptions options)
    {
        var tcs = new TaskCompletionSource<DialogOutcome>();
        this.queue.Enqueue((config, options, tcs));
        _ = this.ProcessQueueAsync();
        return tcs.Task;
    }

    async Task ProcessQueueAsync()
    {
        if (this.isProcessingQueue)
            return;

        this.isProcessingQueue = true;
        try
        {
            while (this.queue.Count > 0)
            {
                var (config, options, tcs) = this.queue.Dequeue();
                await this.ShowSingleAsync(config, options, tcs);
            }
        }
        finally
        {
            this.isProcessingQueue = false;
        }
    }

    async Task ShowSingleAsync(DialogConfig config, DialogOptions options, TaskCompletionSource<DialogOutcome> tcs)
    {
        var dismissed = new TaskCompletionSource();
        var view = new DialogView(config, options);
        view.SetOnDismissed(() => dismissed.TrySetResult());

        AbsoluteLayout.SetLayoutBounds(view, new Rect(0, 0, 1, 1));
        AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.All);
        this.overlay.Children.Add(view);

        await view.AnimateInAsync();
        await dismissed.Task;

        this.overlay.Children.Remove(view);
        tcs.TrySetResult(view.Outcome);
    }
}
