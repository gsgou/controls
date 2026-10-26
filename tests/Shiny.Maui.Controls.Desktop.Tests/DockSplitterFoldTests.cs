using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Desktop.Docking;
using Shiny.Maui.Controls.Infrastructure;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Desktop.Tests;

/// <summary>
/// A dock splitter lines up with a fold that runs the same way, display-only. The posture source is
/// process-wide, so the class is its own serial collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class FoldPostureCollection
{
    public const string Name = "FoldPosture";
}


[Collection(FoldPostureCollection.Name)]
public sealed class DockSplitterFoldTests : IDisposable
{
    sealed class Source : IFoldPostureSource
    {
        public FoldInfo Current { get; set; } = FoldInfo.None;
        public event EventHandler? Changed;
        public void Raise() => this.Changed?.Invoke(this, EventArgs.Empty);
    }

    readonly Source source = new();

    public DockSplitterFoldTests() => FoldPosture.SetSource(this.source);

    public void Dispose() => FoldPosture.SetSource(null);


    (Grid Grid, DockSplitter Splitter, List<double> Applied) Build(DockOrientation orientation)
    {
        var grid = new Grid();
        var applied = new List<double>();
        var splitter = new DockSplitter { Orientation = orientation, Ratio = 0.3 };
        splitter.RatioChanging += (_, r) => applied.Add(r);
        grid.Add(splitter);
        ((IView)grid).Arrange(new Rect(0, 0, 805, 400));
        ((IView)splitter).Arrange(new Rect(200, 0, DockSplitter.Thickness, 400));
        return (grid, splitter, applied);
    }


    [Fact]
    public void ASideBySideSplitSnapsToABookFold()
    {
        this.source.Current = new FoldInfo(FoldPostureState.HalfOpen, FoldPostureOrientation.Vertical, new Rect(402.5, 0, 0, 400));
        var (_, splitter, applied) = this.Build(DockOrientation.Horizontal);
        this.source.Raise();

        splitter.FoldRatio.ShouldBe(0.5);
        splitter.EffectiveRatio.ShouldBe(0.5);
        splitter.Ratio.ShouldBe(0.3, "the saved ratio is the user's, not the fold's");
        applied.ShouldContain(0.5);
    }

    [Fact]
    public void AStackedSplitIgnoresABookFold()
    {
        this.source.Current = new FoldInfo(FoldPostureState.HalfOpen, FoldPostureOrientation.Vertical, new Rect(402.5, 0, 0, 400));
        var (_, splitter, _) = this.Build(DockOrientation.Vertical);
        this.source.Raise();

        splitter.FoldRatio.ShouldBeNull();
    }

    [Fact]
    public void OpeningFlatPutsTheUsersRatioBack()
    {
        this.source.Current = new FoldInfo(FoldPostureState.HalfOpen, FoldPostureOrientation.Vertical, new Rect(402.5, 0, 0, 400));
        var (_, splitter, applied) = this.Build(DockOrientation.Horizontal);
        this.source.Raise();

        this.source.Current = FoldInfo.None;
        this.source.Raise();

        splitter.FoldRatio.ShouldBeNull();
        applied[^1].ShouldBe(0.3);
    }
}
