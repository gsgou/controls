using System.ComponentModel;
using System.Windows.Input;
using Shiny.Maui.Controls;

namespace Sample.Features.Tabs;

/// <summary>
/// Implements <see cref="ITabAware"/> rather than overriding the page's <c>OnAppearing</c>: a page
/// adopted into a tab is never the page the platform presented, so MAUI's page lifecycle does not
/// run for it. The view model gets the callbacks directly, with nothing to relay.
/// </summary>
public class TabsInboxViewModel : INotifyPropertyChanged, ITabAware
{
    int unread = 4;
    int visits;
    string log = "";

    public TabsInboxViewModel()
    {
        this.ComposeCommand = new Command(() => this.Log = "Composing a new message…");
        this.MarkReadCommand = new Command(() =>
        {
            this.Unread = 0;
            this.Log = "Everything marked read — the tab's badge went with it.";
        });
        this.EmptyCommand = new Command(() =>
        {
            this.Unread = 0;
            this.Log = "Inbox emptied.";
        });
    }

    public ICommand ComposeCommand { get; }

    public ICommand MarkReadCommand { get; }

    public ICommand EmptyCommand { get; }

    public int Unread
    {
        get => this.unread;
        set
        {
            this.unread = value;
            this.Raise(nameof(this.Unread));
            this.Raise(nameof(this.UnreadText));
        }
    }

    /// <summary>Null rather than "0", so the badge disappears instead of showing a zero.</summary>
    public string? UnreadText => this.unread > 0 ? this.unread.ToString() : null;

    public string Log
    {
        get => this.log;
        set
        {
            this.log = value;
            this.Raise(nameof(this.Log));
        }
    }

    public void OnTabAppearing()
    {
        this.visits++;
        this.Log = $"Inbox tab entered {this.visits} time(s).";
    }

    public void OnTabDisappearing() => this.Log = "Inbox tab left.";

    public event PropertyChangedEventHandler? PropertyChanged;

    void Raise(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
