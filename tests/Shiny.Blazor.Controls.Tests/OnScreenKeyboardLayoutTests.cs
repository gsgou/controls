using Shiny.Blazor.Controls.OnScreenKeyboard;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// The keyboard lays its rows out with flex, so a row whose widths do not add up to the same total
/// as its neighbours does not overflow or error — it silently draws wider keys than the row above
/// and the board looks subtly wrong. Same for a key with no face or no label: it renders as a blank
/// button rather than failing. Nothing here can be caught by the compiler, so it is pinned instead.
/// </summary>
public class OnScreenKeyboardLayoutTests
{
    const double RowUnits = 15;

    public static TheoryData<string, int> AllRows
    {
        get
        {
            var data = new TheoryData<string, int>();
            for (var i = 0; i < OnScreenKeyboardLayout.Letters.Count; i++)
                data.Add("letters", i);

            for (var i = 0; i < OnScreenKeyboardLayout.Symbols.Count; i++)
                data.Add("symbols", i);

            return data;
        }
    }

    static IReadOnlyList<OnScreenKey> Row(string layer, int index)
        => layer == "letters" ? OnScreenKeyboardLayout.Letters[index] : OnScreenKeyboardLayout.Symbols[index];

    [Theory]
    [MemberData(nameof(AllRows))]
    public void EveryRowIsTheSameTotalWidth(string layer, int index)
        => Row(layer, index).Sum(x => x.Width).ShouldBe(RowUnits);

    [Fact]
    public void TheSharedBottomRowMatchesToo()
        => OnScreenKeyboardLayout.BottomRow.Sum(x => x.Width).ShouldBe(RowUnits);

    [Fact]
    public void EveryLetterShiftsToItsOwnUppercase()
    {
        var letters = AllKeys()
            .Where(x => x.Kind == OnScreenKeyKind.Character
                        && x.Value.Length == 1
                        && char.IsLetter(x.Value[0]));

        letters.ShouldNotBeEmpty();

        foreach (var key in letters)
            key.ShiftValue.ShouldBe(key.Value.ToUpperInvariant());
    }

    [Fact]
    public void EveryKeyHasSomethingToDraw()
    {
        // Space and the layer key are the two whose face the host paints from live state.
        var drawnByHost = new[] { OnScreenKeyKind.Space, OnScreenKeyKind.Layer };

        foreach (var key in AllKeys().Where(x => !drawnByHost.Contains(x.Kind)))
        {
            var face = key.Kind == OnScreenKeyKind.Character ? key.Value : key.Glyph;
            face.ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    public void EveryGlyphKeyIsLabelledForScreenReaders()
    {
        foreach (var key in AllKeys().Where(x => x.Kind != OnScreenKeyKind.Character))
            key.AriaLabel.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// The direction goes to the browser as a bare string and its switch has no default branch — a
    /// typo here would move no caret and raise nothing.
    /// </summary>
    [Fact]
    public void ArrowDirectionsAreOnesTheBrowserSideUnderstands()
    {
        var known = new[] { "left", "right", "up", "down" };
        var arrows = AllKeys().Where(x => x.Kind == OnScreenKeyKind.Arrow).ToList();

        arrows.Count.ShouldBe(4);
        foreach (var key in arrows)
            known.ShouldContain(key.Value);
    }

    static IEnumerable<OnScreenKey> AllKeys()
        => OnScreenKeyboardLayout.Letters
            .Concat(OnScreenKeyboardLayout.Symbols)
            .SelectMany(x => x)
            .Concat(OnScreenKeyboardLayout.BottomRow);
}
