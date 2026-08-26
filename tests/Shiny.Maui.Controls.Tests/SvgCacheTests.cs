using Shiny.Maui.Controls.Images.Svg;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The behaviour that makes a list of vector icons affordable: parse once, share the result, and
/// keep a bounded number of them.
/// </summary>
public class SvgCacheTests
{
    const string Markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'><rect width='10' height='10'/></svg>";

    static SvgDocument Build() => SvgDocument.Parse(Markup);


    [Fact]
    public void Get_ParsesOnce_AndSharesTheResult()
    {
        var cache = new SvgCache();
        var parses = 0;

        SvgDocument Factory()
        {
            parses++;
            return Build();
        }

        var first = cache.Get("a", Factory);
        var second = cache.Get("a", Factory);

        parses.ShouldBe(1);

        // Reference equality is the whole point - a shared document is what makes a hundred cells
        // showing the same icon cost one parse.
        second.ShouldBeSameAs(first);
    }


    [Fact]
    public void DistinctKeys_AreDistinctEntries()
    {
        var cache = new SvgCache();

        cache.Get("a", Build).ShouldNotBeSameAs(cache.Get("b", Build));
        cache.Count.ShouldBe(2);
    }


    [Fact]
    public void Eviction_TakesTheLeastRecentlyUsed()
    {
        var cache = new SvgCache(2);

        var a = cache.Get("a", Build);
        cache.Get("b", Build);

        // Touching "a" makes "b" the oldest, so adding "c" must take "b" and leave "a".
        cache.Get("a", Build).ShouldBeSameAs(a);
        cache.Get("c", Build);

        cache.Count.ShouldBe(2);
        cache.TryGet("a", out _).ShouldBeTrue();
        cache.TryGet("b", out _).ShouldBeFalse();
        cache.TryGet("c", out _).ShouldBeTrue();
    }


    [Fact]
    public void Remove_ForcesAReparse()
    {
        var cache = new SvgCache();

        var first = cache.Get("a", Build);
        cache.Remove("a");

        cache.Get("a", Build).ShouldNotBeSameAs(first);
    }


    [Fact]
    public void ZeroLimit_TurnsCachingOff()
    {
        var cache = new SvgCache(0);

        var first = cache.Get("a", Build);

        cache.Count.ShouldBe(0);
        cache.Get("a", Build).ShouldNotBeSameAs(first);
    }


    [Fact]
    public void Clear_DropsEverything()
    {
        var cache = new SvgCache();

        cache.Get("a", Build);
        cache.Get("b", Build);
        cache.Clear();

        cache.Count.ShouldBe(0);
    }
}
