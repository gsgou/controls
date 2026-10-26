using Shiny.Maui.Controls;
using Shiny.Maui.Controls.Dialogs;
using Shiny.Maui.Controls.Flyout;
using Shiny.Maui.Controls.Toast;

namespace Sample.Features.Foldables;

public partial class FoldablesPage : ShinyContentPage
{
    readonly SampleFoldSource source = SampleFoldSource.Instance;
    double hinge;
    string simulation = "Device";

    public FoldablesPage()
    {
        InitializeComponent();
        SampleSourceCode.Attach(this);

        this.Flyout.StateChanged += (_, _) => this.UpdateReadout();
        this.SizeChanged += (_, _) => this.Resimulate();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this.source.Changed += this.OnPostureChanged;
        this.source.DeviceChanged += this.OnPostureChanged;
        this.UpdateReadout();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.source.Changed -= this.OnPostureChanged;
        this.source.DeviceChanged -= this.OnPostureChanged;

        // Leave the rest of the gallery on the real posture.
        this.source.Simulated = null;
        this.simulation = "Device";
    }

    void OnPostureChanged(object? sender, EventArgs e)
    {
        this.Dispatcher.Dispatch(this.UpdateReadout);

        // The flyout re-lays out after the posture lands, so read its width again once it has.
        this.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(600), this.UpdateReadout);
    }


    // The fold is reported in window coordinates, so the simulation is sized to the window.
    Size WindowSize => this.Window is { Width: > 0 } window
        ? new Size(window.Width, window.Height)
        : new Size(this.Width, this.Height);

    void Resimulate()
    {
        var size = this.WindowSize;
        this.source.Simulated = this.simulation switch
        {
            "Book" => SampleFoldSource.Book(size, this.hinge),
            "Tabletop" => SampleFoldSource.Tabletop(size, this.hinge),
            "Flat" => new FoldInfo(FoldPostureState.Flat, FoldPostureOrientation.Vertical, null),
            _ => null
        };
        this.UpdateReadout();
        this.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(600), this.UpdateReadout);
    }

    void Simulate(string mode)
    {
        this.simulation = mode;
        this.Resimulate();
    }

    void OnSimulateDevice(object? sender, EventArgs e) => this.Simulate("Device");
    void OnSimulateBook(object? sender, EventArgs e) => this.Simulate("Book");
    void OnSimulateTabletop(object? sender, EventArgs e) => this.Simulate("Tabletop");
    void OnSimulateFlat(object? sender, EventArgs e) => this.Simulate("Flat");

    void OnHingeToggled(object? sender, ToggledEventArgs e)
    {
        this.hinge = e.Value ? 24 : 0;
        this.Resimulate();
    }


    void UpdateReadout()
    {
        var info = this.source.Current;
        var device = this.source.Device;

        this.SourceLabel.Text = this.source.HasDeviceSource
            ? $"Device reports {Describe(device)}" + (this.source.Simulated is null ? "" : $" · simulating {this.simulation}")
            : "No device posture source on this platform" + (this.source.Simulated is null ? "" : $" · simulating {this.simulation}");

        this.StateLabel.Text = Describe(info);
        this.RectLabel.Text = info.Fold is { } fold
            ? $"Fold at x {fold.X:0}, y {fold.Y:0} · {fold.Width:0} × {fold.Height:0} (window units)"
            : "No fold divides the window";

        var snapped = this.Flyout.GetEffectivePresentation(FlyoutSide.Start);
        this.FlyoutLabel.Text = $"List panel: {this.ListPanel.State}, {this.Flyout.GetCurrentWidth(FlyoutSide.Start):0} wide, {snapped}";
    }

    static string Describe(FoldInfo info) => info.State switch
    {
        FoldPostureState.HalfOpen when info.IsBook => "Book (half open, vertical fold)",
        FoldPostureState.HalfOpen when info.IsTabletop => "Tabletop (half open, horizontal fold)",
        FoldPostureState.HalfOpen => "Half open",
        FoldPostureState.Flat => info.IsSeparating ? "Flat, divided by a hinge" : "Flat",
        _ => "No fold"
    };


    static T Service<T>() where T : notnull
        => IPlatformApplication.Current!.Services.GetRequiredService<T>();

    async void OnDialog(object? sender, EventArgs e)
        => await Service<IDialogService>().Alert("On one side", "A dialog never sits across the fold: the larger half of a book, the top half of a tabletop.");

    async void OnActionSheet(object? sender, EventArgs e)
        => await Service<IDialogService>().ActionSheet("Share to", ["Messages", "Mail", "Notes"]);

    async void OnToast(object? sender, EventArgs e)
        => await Service<IToaster>().InfoAsync("Toasts keep to one segment too");

    async void OnLoading(object? sender, EventArgs e)
    {
        this.Loading.IsShown = true;
        await Task.Delay(2000);
        this.Loading.IsShown = false;
    }

    void OnPanel(object? sender, EventArgs e) => this.Panel.IsOpen = true;

    void OnClosePanel(object? sender, EventArgs e) => this.Panel.IsOpen = false;

    void OnToggleList(object? sender, EventArgs e) => _ = this.ListPanel.ToggleAsync();
}
