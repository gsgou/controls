namespace Shiny.Maui.Controls.TrayIcon;

abstract class TrayIconBase : ITrayIcon
{
    protected TrayMenu? Menu { get; private set; }
    protected Func<Stream>? IconFactory { get; private set; }

    string? tooltip;
    public string? Tooltip
    {
        get => this.tooltip;
        set
        {
            this.tooltip = value;
            this.OnTooltipChanged(value);
        }
    }

    string? title;
    public string? Title
    {
        get => this.title;
        set
        {
            this.title = value;
            this.OnTitleChanged(value);
        }
    }

    bool isVisible = true;
    public bool IsVisible
    {
        get => this.isVisible;
        set
        {
            this.isVisible = value;
            this.OnVisibilityChanged(value);
        }
    }

    bool isTemplate;
    public bool IsTemplateImage
    {
        get => this.isTemplate;
        set
        {
            this.isTemplate = value;
            if (this.IconFactory != null)
                this.OnIconChanged(this.IconFactory);
        }
    }

    public void SetIcon(Func<Stream> iconStreamFactory)
    {
        this.IconFactory = iconStreamFactory;
        this.OnIconChanged(iconStreamFactory);
    }

    public void SetMenu(TrayMenu menu)
    {
        if (this.Menu != null)
            this.Menu.Changed -= this.OnMenuChanged;
        this.Menu = menu;
        menu.Changed += this.OnMenuChanged;
        this.OnMenuChanged(menu, EventArgs.Empty);
    }

    public abstract void ShowMenu();

    public event EventHandler<TrayClickEventArgs>? PrimaryClick;
    public event EventHandler<TrayClickEventArgs>? SecondaryClick;
    public event EventHandler<TrayClickEventArgs>? DoubleClick;

    protected void RaisePrimary(int x, int y) => this.PrimaryClick?.Invoke(this, new TrayClickEventArgs(x, y));
    protected void RaiseSecondary(int x, int y) => this.SecondaryClick?.Invoke(this, new TrayClickEventArgs(x, y));
    protected void RaiseDouble(int x, int y) => this.DoubleClick?.Invoke(this, new TrayClickEventArgs(x, y));

    protected abstract void OnIconChanged(Func<Stream> factory);
    protected abstract void OnTooltipChanged(string? value);
    protected abstract void OnTitleChanged(string? value);
    protected abstract void OnVisibilityChanged(bool visible);
    protected abstract void OnMenuChanged(object? sender, EventArgs e);

    public virtual void Dispose()
    {
        if (this.Menu != null)
            this.Menu.Changed -= this.OnMenuChanged;
        GC.SuppressFinalize(this);
    }
}
