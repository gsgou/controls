using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Cells;
using Shouldly;
using Xunit;
using TvTableSection = Shiny.Maui.Controls.Sections.TableSection;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Section separators are drawn between sections, and "between" has to mean between the ones that
/// actually draw something. A hidden section renders as a zero-height placeholder, so counting it
/// put a rule on both sides of nothing — two rules together where a section was hidden between two
/// visible ones, and a rule under the last visible section when the hidden ones were trailing.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class TableViewSectionSeparatorTests
{
    static TableView Build(params bool[] sectionVisibility)
    {
        new Application();

        var table = new TableView();
        foreach (var visible in sectionVisibility)
        {
            var section = new TvTableSection("Section") { IsVisible = visible };
            section.Cells.Add(new LabelCell { Title = "Row" });
            table.Root.Sections.Add(section);
        }
        return table;
    }

    static int SeparatorCount(TableView table) =>
        ((VerticalStackLayout)table.ScrollContent).Children.OfType<BoxView>().Count();

    static int SectionViewCount(TableView table) =>
        ((VerticalStackLayout)table.ScrollContent).Children.Count(x => x is not BoxView);

    [Fact]
    public void AllVisible_DrawsOneSeparatorBetweenEachPair()
    {
        var table = Build(true, true, true);

        SectionViewCount(table).ShouldBe(3);
        SeparatorCount(table).ShouldBe(2);
    }

    [Fact]
    public void HiddenMiddleSection_DoesNotLeaveTwoSeparatorsTogether()
    {
        var table = Build(true, false, true);

        SectionViewCount(table).ShouldBe(2);
        SeparatorCount(table).ShouldBe(1);
    }

    /// <summary>
    /// The worse half of the same bug: a rule under the final section with nothing below it reads as
    /// a rendering fault rather than as a section being hidden.
    /// </summary>
    [Fact]
    public void HiddenTrailingSections_LeaveNoRuleUnderTheLastVisibleOne()
    {
        var table = Build(true, true, false, false);

        SectionViewCount(table).ShouldBe(2);
        SeparatorCount(table).ShouldBe(1);
    }

    [Fact]
    public void HiddenLeadingSections_LeaveNoRuleAboveTheFirstVisibleOne()
    {
        var table = Build(false, true, true);

        SectionViewCount(table).ShouldBe(2);
        SeparatorCount(table).ShouldBe(1);
    }

    [Fact]
    public void OnlyOneVisibleSection_DrawsNoSeparatorAtAll()
    {
        var table = Build(false, true, false);

        SectionViewCount(table).ShouldBe(1);
        SeparatorCount(table).ShouldBe(0);
    }

    [Fact]
    public void EverySectionHidden_DrawsNothing()
    {
        var table = Build(false, false);

        SectionViewCount(table).ShouldBe(0);
        SeparatorCount(table).ShouldBe(0);
    }

    /// <summary>
    /// IsVisible raises SectionChanged, which the root forwards and the table re-renders on — so the
    /// separators have to be recounted, not just the sections.
    /// </summary>
    [Fact]
    public void HidingASectionAtRuntimeRecountsTheSeparators()
    {
        var table = Build(true, true, true);
        SeparatorCount(table).ShouldBe(2);

        table.Root.Sections[1].IsVisible = false;

        SectionViewCount(table).ShouldBe(2);
        SeparatorCount(table).ShouldBe(1);
    }

    [Fact]
    public void ShowingASectionAgainRestoresItsSeparator()
    {
        var table = Build(true, false, true);
        SeparatorCount(table).ShouldBe(1);

        table.Root.Sections[1].IsVisible = true;

        SectionViewCount(table).ShouldBe(3);
        SeparatorCount(table).ShouldBe(2);
    }

    [Fact]
    public void SeparatorsOff_DrawsNoneRegardlessOfVisibility()
    {
        var table = Build(true, true, true);
        table.ShowSectionSeparator = false;

        SeparatorCount(table).ShouldBe(0);
    }
}
