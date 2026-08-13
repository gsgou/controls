using Microsoft.Maui.Controls;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The parts of <see cref="StateView"/> that are logic rather than pixels: which branch wins, that
/// only one branch is ever hosted, and that a templated branch is not built until it is reached.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class StateViewTests
{
    public StateViewTests()
    {
        TestDispatcherProvider.Install();

        // A fresh Application per test, not `Application.Current ?? new` - Application.Current is
        // process-wide, so anything one test merges would leak into the rest of the collection.
        _ = new Application();
    }

    static StateView Build(params string[] names)
    {
        var view = new StateView { Transition = StateTransition.None };
        foreach (var name in names)
            view.States.Add(new StateViewState { Name = name, Content = new Label { Text = name } });

        return view;
    }

    static string? HostedText(StateView view)
    {
        var root = (Grid)view.Content!;
        foreach (var host in root.Children.OfType<ContentView>())
        {
            if (host.Content is Label label)
                return label.Text;
        }
        return null;
    }

    static int HostedCount(StateView view)
    {
        var root = (Grid)view.Content!;
        return root.Children.OfType<ContentView>().Count(h => h.Content != null);
    }


    [Fact]
    public void MatchingStateIsHosted()
    {
        var view = Build("One", "Two", "Three");

        view.CurrentState = "Two";

        HostedText(view).ShouldBe("Two");
        view.CurrentStateView!.Name.ShouldBe("Two");
        view.CurrentStateIndex.ShouldBe(1);
    }

    [Fact]
    public void MatchIsCaseInsensitive()
    {
        var view = Build("Loading", "Loaded");

        view.CurrentState = "LOADED";

        HostedText(view).ShouldBe("Loaded");
    }

    [Fact]
    public void OnlyOneStateIsEverHosted()
    {
        var view = Build("One", "Two", "Three");

        view.CurrentState = "Two";
        view.CurrentState = "Three";
        view.CurrentState = "One";

        // Both hosts exist for the transition; the one that lost must have been emptied, or the
        // outgoing branch keeps its bindings and timers alive underneath the new one.
        HostedCount(view).ShouldBe(1);
        HostedText(view).ShouldBe("One");
    }

    [Fact]
    public void UnmatchedFallsBackToDefaultState()
    {
        var view = Build("One", "Two");
        view.DefaultState = "Two";

        view.CurrentState = "Nonsense";

        HostedText(view).ShouldBe("Two");
    }

    [Fact]
    public void UnmatchedWithNoDefaultFallsBackToTheFirstState()
    {
        var view = Build("One", "Two");

        view.CurrentState = "Nonsense";

        HostedText(view).ShouldBe("One");
    }

    [Fact]
    public void ContentTemplateIsNotBuiltUntilItsStateIsReached()
    {
        var built = 0;
        var view = new StateView { Transition = StateTransition.None };
        view.States.Add(new StateViewState { Name = "One", Content = new Label { Text = "One" } });
        view.States.Add(new StateViewState
        {
            Name = "Two",
            ContentTemplate = new DataTemplate(() =>
            {
                built++;
                return new Label { Text = "Two" };
            })
        });

        built.ShouldBe(0);

        view.CurrentState = "Two";
        built.ShouldBe(1);
        HostedText(view).ShouldBe("Two");

        // Cached by default: coming back must not rebuild, or entry text and scroll position reset.
        view.CurrentState = "One";
        view.CurrentState = "Two";
        built.ShouldBe(1);
    }

    [Fact]
    public void CacheContentOffRebuildsTheTemplateEachVisit()
    {
        var built = 0;
        var view = new StateView { Transition = StateTransition.None, CacheContent = false };
        view.States.Add(new StateViewState { Name = "One", Content = new Label { Text = "One" } });
        view.States.Add(new StateViewState
        {
            Name = "Two",
            ContentTemplate = new DataTemplate(() =>
            {
                built++;
                return new Label { Text = "Two" };
            })
        });

        view.CurrentState = "Two";
        view.CurrentState = "One";
        view.CurrentState = "Two";

        built.ShouldBe(2);
    }

    [Fact]
    public void StateChangedReportsBothEnds()
    {
        var view = Build("One", "Two");
        StateChangedEventArgs? seen = null;
        view.StateChanged += (_, e) => seen = e;

        view.CurrentState = "Two";

        seen.ShouldNotBeNull();
        seen!.PreviousState.ShouldBe("One");
        seen.CurrentState.ShouldBe("Two");
    }

    [Fact]
    public void GoToRefusesAnUnknownState()
    {
        var view = Build("One", "Two");

        view.GoTo("Nope").ShouldBeFalse();
        HostedText(view).ShouldBe("One");

        view.GoTo("Two").ShouldBeTrue();
        HostedText(view).ShouldBe("Two");
    }

    [Fact]
    public void AddingAStateLaterStillResolves()
    {
        var view = new StateView { Transition = StateTransition.None, CurrentState = "Late" };

        HostedText(view).ShouldBeNull();

        view.States.Add(new StateViewState { Name = "Late", Content = new Label { Text = "Late" } });

        HostedText(view).ShouldBe("Late");
    }

    [Fact]
    public void RemovingTheCurrentStateFallsBackRatherThanBlanking()
    {
        var view = Build("One", "Two");
        view.CurrentState = "Two";

        view.States.RemoveAt(1);

        HostedText(view).ShouldBe("One");
        HostedCount(view).ShouldBe(1);
    }
}
