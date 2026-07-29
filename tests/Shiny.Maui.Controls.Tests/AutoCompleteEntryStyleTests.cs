using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// These tests install an implicit Style into <see cref="Application.Current"/>, which is
/// process-wide state, so they must not run alongside anything else touching it.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class ApplicationResourcesCollection
{
    public const string Name = "ApplicationResources";
}

/// <summary>
/// Regression cover for an implicit Style crashing AutoCompleteEntry during construction.
///
/// The control builds its children in its constructor and used to call SetDynamicResource
/// partway through. SetDynamicResource resolves against Application.Current.Resources, which
/// registers the element with MergedStyle and applies any implicit Style for the type
/// synchronously - from inside the constructor. The setters ran the propertyChanged
/// callbacks, which dereferenced children not yet assigned, so any page carrying an implicit
/// Style for this control died on inflation:
///
///     System.NullReferenceException
///       at AutoCompleteEntry.&lt;&gt;c.&lt;.cctor&gt;b__113_13(...)   // CornerRadius -> dropDownShape
///       at AutoCompleteEntry..ctor()
///
/// The style has to live in Application resources to reproduce this - a style in a page's
/// resources is only reached once the control is parented, which is after the constructor has
/// finished and is therefore safe either way.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class AutoCompleteEntryStyleTests
{
    static Style BuildStyle() => new(typeof(AutoCompleteEntry))
    {
        Setters =
        {
            // The dangerous ones - these callbacks reach for dropDownShape / dropDownBorder,
            // which are assigned late in the constructor.
            new Setter { Property = AutoCompleteEntry.CornerRadiusProperty, Value = 12d },
            new Setter { Property = AutoCompleteEntry.MaxDropDownHeightProperty, Value = 180d },
            new Setter { Property = AutoCompleteEntry.DropDownBackgroundColorProperty, Value = Colors.White },
            new Setter { Property = AutoCompleteEntry.DropDownBorderColorProperty, Value = Colors.Red },
            // The rest of the styleable surface, for good measure.
            new Setter { Property = AutoCompleteEntry.SpinnerColorProperty, Value = Colors.Green },
            new Setter { Property = AutoCompleteEntry.TextColorProperty, Value = Colors.Black },
            new Setter { Property = AutoCompleteEntry.PlaceholderColorProperty, Value = Colors.Gray },
            new Setter { Property = AutoCompleteEntry.FontSizeProperty, Value = 15d }
        }
    };

    static Application NewAppWithImplicitStyle()
    {
        // Constructing an Application makes it Application.Current.
        var app = new Application();
        app.Resources.Add(BuildStyle());
        return app;
    }

    /// <summary>The original crash: implicit style reachable while the constructor runs.</summary>
    [Fact]
    public void ImplicitAppStyle_DoesNotThrowDuringConstruction()
    {
        NewAppWithImplicitStyle();

        var control = Should.NotThrow(() => new AutoCompleteEntry());

        // Applied, not merely survived - a null-guard that swallowed the value would be a
        // silent regression rather than a crash, so assert the values actually landed.
        control.CornerRadius.ShouldBe(12d);
        control.MaxDropDownHeight.ShouldBe(180d);
        control.DropDownBackgroundColor.ShouldBe(Colors.White);
        control.DropDownBorderColor.ShouldBe(Colors.Red);
        control.SpinnerColor.ShouldBe(Colors.Green);
        control.TextColor.ShouldBe(Colors.Black);
        control.FontSize.ShouldBe(15d);
    }

    /// <summary>The control must still be usable once parented under a styled app.</summary>
    [Fact]
    public void ImplicitAppStyle_SurvivesBeingParented()
    {
        NewAppWithImplicitStyle();

        var control = new AutoCompleteEntry();
        var layout = new VerticalStackLayout();

        Should.NotThrow(() => layout.Add(control));
        Should.NotThrow(() => new ContentPage { Content = layout });

        control.CornerRadius.ShouldBe(12d);
    }

    /// <summary>A style arriving after construction must still take effect.</summary>
    [Fact]
    public void ExplicitStyle_AppliedAfterConstruction_TakesEffect()
    {
        new Application();

        var control = new AutoCompleteEntry();
        control.CornerRadius.ShouldBe(4d);

        control.Style = BuildStyle();

        control.CornerRadius.ShouldBe(12d);
        control.SpinnerColor.ShouldBe(Colors.Green);
    }

    /// <summary>Unstyled construction keeps the documented defaults.</summary>
    [Fact]
    public void NoStyle_UsesDefaults()
    {
        new Application();

        var control = new AutoCompleteEntry();

        control.CornerRadius.ShouldBe(4d);
        control.MaxDropDownHeight.ShouldBe(200d);
        control.FontSize.ShouldBe(14d);
        control.DropDownBackgroundColor.ShouldBeNull();
        control.SpinnerColor.ShouldBeNull();
    }

    /// <summary>
    /// Values set in an object initialiser are applied by the constructor's own sync pass -
    /// this is what would regress if the readiness gate swallowed early values.
    /// </summary>
    [Fact]
    public void ValuesSetAtConstruction_AreRetained()
    {
        new Application();

        var control = new AutoCompleteEntry
        {
            Text = "hello",
            Placeholder = "type here",
            FontSize = 22d,
            CornerRadius = 9d,
            MaxDropDownHeight = 111d
        };

        control.Text.ShouldBe("hello");
        control.Placeholder.ShouldBe("type here");
        control.FontSize.ShouldBe(22d);
        control.CornerRadius.ShouldBe(9d);
        control.MaxDropDownHeight.ShouldBe(111d);
    }
}
