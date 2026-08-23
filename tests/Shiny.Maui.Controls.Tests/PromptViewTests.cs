using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.QuickEntry;

using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The keyboard behaviour of <see cref="PromptView"/>. The popup host reads keys off the native
/// window and hands them here, so this is the only place the interaction can be pinned down without
/// a real window on a real desktop.
/// </summary>
[Collection(PromptApplicationCollection.Name)]
public class PromptViewTests
{
    public PromptViewTests()
    {
        TestDispatcherProvider.Install();

        // A fresh Application per test rather than `Application.Current ?? new`: Application.Current
        // is process-wide, so anything one test puts in its resources leaks into the rest.
        _ = new Application();
    }

    static PromptView Build(params string[] suggestions)
    {
        var view = new PromptView();
        if (suggestions.Length > 0)
            view.Suggestions = suggestions.Select(s => new PromptSuggestion(s)).ToList();

        return view;
    }

    [Fact]
    public void Constructs_with_an_implicit_style_present()
    {
        // MAUI applies an implicit Style from StyleableElement's own constructor, before this type's
        // fields exist — the classic way a control like this crashes on first use.
        Application.Current!.Resources.Add(new Style(typeof(PromptView))
        {
            Setters = { new Setter { Property = PromptView.CornerRadiusProperty, Value = 4d } }
        });

        var view = new PromptView();
        view.CornerRadius.ShouldBe(4d);
    }

    [Fact]
    public void Arrow_keys_are_declined_when_there_is_nothing_to_move_through()
    {
        var view = Build();
        view.HandleKey(QuickEntryKey.ArrowDown).ShouldBeFalse();
        view.HighlightedIndex.ShouldBe(-1);
    }

    [Fact]
    public void Arrow_down_walks_the_suggestions()
    {
        var view = Build("one", "two");

        view.HandleKey(QuickEntryKey.ArrowDown).ShouldBeTrue();
        view.HighlightedIndex.ShouldBe(0);

        view.HandleKey(QuickEntryKey.ArrowDown);
        view.HighlightedIndex.ShouldBe(1);
    }

    [Fact]
    public void Arrow_down_past_the_end_returns_to_the_prompt()
    {
        var view = Build("one", "two");
        view.HandleKey(QuickEntryKey.ArrowDown);
        view.HandleKey(QuickEntryKey.ArrowDown);
        view.HandleKey(QuickEntryKey.ArrowDown);

        view.HighlightedIndex.ShouldBe(-1);
    }

    [Fact]
    public void Arrow_up_from_the_prompt_wraps_to_the_last_suggestion()
    {
        var view = Build("one", "two", "three");
        view.HandleKey(QuickEntryKey.ArrowUp).ShouldBeTrue();
        view.HighlightedIndex.ShouldBe(2);
    }

    [Fact]
    public void Enter_on_a_highlighted_suggestion_submits_it()
    {
        var view = Build("summarise", "translate");
        PromptSubmittedEventArgs? captured = null;
        view.Submitted += (_, e) => captured = e;

        view.HandleKey(QuickEntryKey.ArrowDown);
        view.HandleKey(QuickEntryKey.ArrowDown);
        view.HandleKey(QuickEntryKey.Enter);

        captured.ShouldNotBeNull();
        captured!.Text.ShouldBe("translate");
        ((PromptSuggestion)captured.Suggestion!).Text.ShouldBe("translate");
    }

    [Fact]
    public void Enter_with_no_highlight_submits_the_typed_text()
    {
        var view = Build("summarise");
        PromptSubmittedEventArgs? captured = null;
        view.Submitted += (_, e) => captured = e;

        view.Text = "what is the weather";
        view.HandleKey(QuickEntryKey.Enter);

        captured.ShouldNotBeNull();
        captured!.Text.ShouldBe("what is the weather");
        captured.Suggestion.ShouldBeNull();
    }

    [Fact]
    public void An_empty_prompt_submits_nothing()
    {
        var view = Build();
        var fired = false;
        view.Submitted += (_, _) => fired = true;

        view.HandleKey(QuickEntryKey.Enter);
        fired.ShouldBeFalse();
    }

    [Fact]
    public void Submitting_clears_the_prompt_by_default()
    {
        var view = Build();
        view.Text = "hello";
        view.Submit();
        view.Text.ShouldBe(String.Empty);
    }

    [Fact]
    public void ClearOnSubmit_off_keeps_the_prompt()
    {
        var view = Build();
        view.ClearOnSubmit = false;
        view.Text = "hello";
        view.Submit();
        view.Text.ShouldBe("hello");
    }

    [Fact]
    public void Submitting_while_busy_cancels_instead()
    {
        var view = Build();
        var submitted = false;
        var cancelled = false;
        view.Submitted += (_, _) => submitted = true;
        view.Cancelled += (_, _) => cancelled = true;

        view.Text = "hello";
        view.IsBusy = true;
        view.Submit();

        cancelled.ShouldBeTrue();
        submitted.ShouldBeFalse();
    }

    /// <summary>
    /// Escape peels one layer of state at a time and only declines — letting the host close the
    /// popup — once there is nothing left to back out of.
    /// </summary>
    [Fact]
    public void Escape_unwinds_one_state_at_a_time_before_reaching_the_host()
    {
        var view = Build("one");
        view.IsBusy = true;
        view.Text = "hello";
        view.ResponseContent = new Label();
        view.HandleKey(QuickEntryKey.ArrowDown);

        view.HandleKey(QuickEntryKey.Escape).ShouldBeTrue();   // cancels the request
        view.IsBusy = false;

        view.HandleKey(QuickEntryKey.Escape).ShouldBeTrue();   // drops the highlight
        view.HighlightedIndex.ShouldBe(-1);

        view.HandleKey(QuickEntryKey.Escape).ShouldBeTrue();   // clears the response
        view.ResponseContent.ShouldBeNull();

        view.HandleKey(QuickEntryKey.Escape).ShouldBeTrue();   // clears the prompt
        view.Text.ShouldBe(String.Empty);

        view.HandleKey(QuickEntryKey.Escape).ShouldBeFalse();  // nothing left — the host closes
    }

    [Fact]
    public void Typing_drops_a_stale_highlight()
    {
        var view = Build("one", "two");
        view.HandleKey(QuickEntryKey.ArrowDown);
        view.HighlightedIndex.ShouldBe(0);

        view.Text = "some";
        view.HighlightedIndex.ShouldBe(-1);
    }

    [Fact]
    public void Suggestions_beyond_the_visible_limit_are_not_reachable()
    {
        var view = new PromptView { MaxVisibleSuggestions = 2 };
        view.Suggestions = new List<PromptSuggestion>
        {
            new("one"), new("two"), new("three"), new("four")
        };

        view.HandleKey(QuickEntryKey.ArrowUp);
        view.HighlightedIndex.ShouldBe(1);
    }

    [Fact]
    public void The_leading_slot_prefers_a_custom_view_over_an_image_over_the_orb()
    {
        var view = new PromptView();
        LeadingContent(view).ShouldBeOfType<PromptOrbView>();

        view.Icon = ImageSource.FromFile("thing.png");
        LeadingContent(view).ShouldBeOfType<Image>();

        var custom = new Label { Text = "AI" };
        view.IconContent = custom;
        LeadingContent(view).ShouldBeSameAs(custom);
    }

    [Fact]
    public void ShowIcon_off_empties_the_leading_slot()
    {
        var view = new PromptView { ShowIcon = false };
        LeadingContent(view).ShouldBeNull();
    }

    [Fact]
    public void The_dropdown_sizes_to_its_content_until_a_height_is_set()
    {
        var view = new PromptView();
        var container = DropdownContainer(view);

        // -1 is MAUI's "no request": the container must not be left holding a height, or the
        // window could never shrink back.
        container.HeightRequest.ShouldBe(-1d);
        container.Content.ShouldBeOfType<VerticalStackLayout>();

        view.DropdownHeight = 240;
        container.HeightRequest.ShouldBe(240d);
        container.Content.ShouldBeOfType<ScrollView>();

        view.DropdownHeight = -1;
        container.HeightRequest.ShouldBe(-1d);
        container.Content.ShouldBeOfType<VerticalStackLayout>();
    }

    [Fact]
    public void Dropdown_content_is_hosted_alongside_the_suggestions()
    {
        var view = Build("one");
        var marker = new Label { Text = "custom" };
        view.DropdownContent = marker;

        Descendants(view).ShouldContain(marker);
    }

    [Fact]
    public void The_reported_height_is_the_card_rather_than_this_stretched_view()
    {
        // The host asks the content how tall it is precisely because a ContentView stretches to
        // whatever it is offered. Reporting zero for a zero width is the only contract the test can
        // pin down headlessly; the value itself needs a real layout pass.
        var view = new PromptView();
        ((IQuickEntryAutoSize)view).GetDesiredHeight(0).ShouldBe(0d);
    }

    // The leading slot is a stack — the orb's host, then any leading tools — so the walk goes one
    // level deeper than the input Grid.
    static View? LeadingContent(PromptView view)
        => Descendants(view)
            .OfType<Grid>()
            .SelectMany(g => g.Children.OfType<Microsoft.Maui.Controls.Layout>())
            .SelectMany(slot => slot.Children.OfType<ContentView>())
            .FirstOrDefault()?.Content;

    static ContentView DropdownContainer(PromptView view)
        => Descendants(view)
            .OfType<ContentView>()
            .First(c => c.Content is ScrollView || c.Content is VerticalStackLayout stack && stack.Children.Count == 3);

    static IEnumerable<View> Descendants(View root)
    {
        var pending = new Stack<View>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            yield return current;

            switch (current)
            {
                case Microsoft.Maui.Controls.Layout layout:
                    foreach (var child in layout.Children.OfType<View>())
                        pending.Push(child);
                    break;
                case ContentView content when content.Content is View inner:
                    pending.Push(inner);
                    break;
                case Border border when border.Content is View inner:
                    pending.Push(inner);
                    break;
            }
        }
    }

    [Fact]
    public void IsBusy_reports_through_the_busy_state_contract_the_glow_listens_on()
    {
        var view = new PromptView();
        var changes = 0;
        ((IQuickEntryBusyState)view).BusyChanged += (_, _) => changes++;

        view.IsBusy = true;
        view.IsBusy = false;

        changes.ShouldBe(2);
        ((IQuickEntryBusyState)view).IsBusy.ShouldBeFalse();
    }
}

/// <summary>
/// These tests write to <see cref="Application.Current"/>, which is process-wide, so they must not
/// run alongside anything else that touches it.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class PromptApplicationCollection
{
    public const string Name = "PromptApplicationResources";
}

/// <summary>
/// Prompt tools: the docked glyphs a package like SpeechAddins adds to a <see cref="PromptView"/>,
/// and the attach/detach contract they hang their subscriptions off.
/// </summary>
[Collection(PromptApplicationCollection.Name)]
public class PromptToolTests
{
    public PromptToolTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
    }

    sealed class ProbeTool : PromptTool, IPromptAwareTool
    {
        public int Attached { get; private set; }
        public int Detached { get; private set; }
        public int Responses { get; private set; }

        void IPromptAwareTool.Attach(PromptView prompt)
        {
            this.Attached++;
            prompt.ResponseChanged += this.OnResponseChanged;
        }

        void IPromptAwareTool.Detach()
        {
            this.Detached++;
            if (this.ParentPrompt is not null)
                this.ParentPrompt.ResponseChanged -= this.OnResponseChanged;
        }

        void OnResponseChanged(object? sender, EventArgs e) => this.Responses++;
    }

    [Fact]
    public void Both_tool_collections_exist_without_being_assigned()
    {
        // A consumer adds to them in XAML without first creating one, the way TextEntry works.
        var view = new PromptView();
        view.LeadingTools.ShouldNotBeNull();
        view.TrailingTools.ShouldNotBeNull();
    }

    [Fact]
    public void A_tool_added_later_is_attached_and_knows_its_prompt()
    {
        var view = new PromptView();
        var tool = new ProbeTool();

        view.TrailingTools!.Add(tool);

        tool.Attached.ShouldBe(1);
        tool.ParentPrompt.ShouldBeSameAs(view);
    }

    [Fact]
    public void Removing_a_tool_detaches_it_and_drops_the_prompt()
    {
        var view = new PromptView();
        var tool = new ProbeTool();

        view.TrailingTools!.Add(tool);
        view.TrailingTools!.Remove(tool);

        tool.Detached.ShouldBe(1);
        tool.ParentPrompt.ShouldBeNull();
    }

    [Fact]
    public void Replacing_the_collection_detaches_what_was_in_the_old_one()
    {
        var view = new PromptView();
        var tool = new ProbeTool();
        view.LeadingTools!.Add(tool);

        view.LeadingTools = new List<PromptTool>();

        tool.Detached.ShouldBe(1);
    }

    [Fact]
    public void A_tool_hears_the_answer_arrive()
    {
        var view = new PromptView();
        var tool = new ProbeTool();
        view.TrailingTools!.Add(tool);

        view.Response = "42";

        tool.Responses.ShouldBe(1, "this is what a read-aloud tool reacts to");
    }
}

/// <summary>
/// The plain-text half of the response area. It exists so the answer is something a tool can read —
/// a <see cref="View"/> handed to ResponseContent has no text to speak.
/// </summary>
[Collection(PromptApplicationCollection.Name)]
public class PromptResponseTests
{
    public PromptResponseTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
    }

    [Fact]
    public void Text_and_content_are_both_empty_to_start()
    {
        var view = new PromptView();
        view.Response.ShouldBeNull();
        view.ResponseContent.ShouldBeNull();
    }

    [Fact]
    public void Clearing_the_text_raises_the_change_as_well()
    {
        var view = new PromptView();
        var raised = 0;
        view.ResponseChanged += (_, _) => raised++;

        view.Response = "answer";
        view.Response = null;

        raised.ShouldBe(2);
    }

    [Fact]
    public void Escape_clears_a_plain_text_answer_before_it_reaches_the_host()
    {
        var view = new PromptView { Response = "answer" };

        view.HandleKey(QuickEntryKey.Escape).ShouldBeTrue("the key is consumed while there is something to back out of");

        view.Response.ShouldBeNull();
    }

    [Fact]
    public void Reset_clears_both_halves()
    {
        var view = new PromptView { Response = "answer", ResponseContent = new Label() };

        view.Reset();

        view.Response.ShouldBeNull();
        view.ResponseContent.ShouldBeNull();
    }
}

/// <summary>
/// The overlay capabilities quick entry's in-app presentation is built on. They are on
/// <see cref="Overlay"/> rather than private to quick entry precisely so anything else that needs a
/// non-centred overlay, or one that glows while it is up, gets them too.
/// </summary>
[Collection(PromptApplicationCollection.Name)]
public class OverlayPlacementTests
{
    public OverlayPlacementTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
    }

    [Fact]
    public void Content_is_centred_by_default()
    {
        var overlay = new Overlay();
        overlay.ContentAlignment.ShouldBe(LayoutOptions.Center);
        overlay.ContentMargin.ShouldBe(new Thickness(0));
    }

    [Fact]
    public void Alignment_and_margin_reach_the_content_container()
    {
        var overlay = new Overlay
        {
            ContentAlignment = LayoutOptions.Start,
            ContentMargin = new Thickness(0, 120, 0, 0)
        };

        var container = (ContentView)overlay.Content!;
        container.VerticalOptions.ShouldBe(LayoutOptions.Start);
        container.Margin.ShouldBe(new Thickness(0, 120, 0, 0));
    }

    [Fact]
    public void A_template_may_hand_back_the_same_instance_every_time()
    {
        // How a caller hosts one long-lived view rather than rebuilding it per show. Re-adding a view
        // that still has a parent is refused, so the container has to let go of it first.
        var hosted = new Label { Text = "prompt" };
        var overlay = new Overlay { OverlayContentTemplate = new DataTemplate(() => hosted) };

        overlay.OverlayContentTemplate = new DataTemplate(() => hosted);

        var container = (ContentView)overlay.Content!;
        container.Content.ShouldBeSameAs(hosted);
    }

    [Fact]
    public void The_edge_glow_is_off_unless_asked_for()
    {
        var overlay = new Overlay();
        overlay.ShowEdgeGlow.ShouldBeFalse();
        overlay.GlowOptions.ShouldBeNull("null means the defaults, so an app opting in configures nothing");
    }
}
