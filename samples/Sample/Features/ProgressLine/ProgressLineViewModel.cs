using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls;

namespace Sample.Features.ProgressLine;

[ShellMap<ProgressLinePage>(registerRoute: false)]
public partial class ProgressLineViewModel(IProgressLineService progressLine) : ObservableObject
{
    [ObservableProperty] double boundValue = 40;
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string status = "Idle";

    [RelayCommand]
    void Increment() => this.BoundValue = Math.Min(100, this.BoundValue + 15);

    [RelayCommand]
    void Decrement() => this.BoundValue = Math.Max(0, this.BoundValue - 15);


    /// <summary>The common case: wrap the work, report nothing, let the trickle carry it.</summary>
    [RelayCommand]
    async Task RunTrickle()
    {
        using var run = progressLine.Start();
        this.Status = "Trickling…";
        await Task.Delay(3000);
        this.Status = "Done";
    }


    /// <summary>Real percentages from a loop that knows how much is left.</summary>
    [RelayCommand]
    async Task RunReported()
    {
        using var run = progressLine.Start(c =>
        {
            c.BarColor = Colors.MediumSeaGreen;
            c.LineHeight = 4;
        });

        for (var i = 1; i <= 10; i++)
        {
            run.SetProgress(i / 10d);
            this.Status = $"Uploading {i * 10}%";
            await Task.Delay(250);
        }
        this.Status = "Uploaded";
    }


    /// <summary>Bottom edge, which is the one that has to clear a tab bar.</summary>
    [RelayCommand]
    async Task RunBottom()
    {
        using var run = progressLine.Start(c =>
        {
            c.Position = ProgressLinePosition.Bottom;
            c.UseGradient = true;
            c.PulseEnabled = true;
            c.LineHeight = 4;
        });

        this.Status = "Working along the bottom…";
        await Task.Delay(3000);
        this.Status = "Done";
    }


    /// <summary>
    /// Two overlapping runs — the line has to stay up until the slower one lands, not vanish when
    /// the quick one does.
    /// </summary>
    [RelayCommand]
    async Task RunOverlapping()
    {
        this.Status = "Two operations…";

        var quick = Task.Run(async () =>
        {
            using var run = progressLine.Start(c => c.BarColor = Colors.Orange);
            await Task.Delay(800);
        });

        var slow = Task.Run(async () =>
        {
            using var run = progressLine.Start(c => c.BarColor = Colors.Orange);
            await Task.Delay(4000);
        });

        await Task.WhenAll(quick, slow);
        this.Status = "Both finished";
    }


    [RelayCommand]
    async Task RunIndeterminate()
    {
        using var run = progressLine.Start(c =>
        {
            c.Indeterminate = true;
            c.BarColor = Colors.MediumPurple;
        });

        this.Status = "No idea how long this takes…";
        await Task.Delay(3000);
        this.Status = "Done";
    }
}
