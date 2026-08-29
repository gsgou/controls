using Shiny.Controls.Office.Spelling;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Spelling suggestions on the keyboard accessory bar.
/// </summary>
/// <remarks>
/// <para>
/// The red underline is the whole of what a phone user gets otherwise. The menu that acts on it hangs
/// off a right-click on the desktop and a long press on touch, and a long press is not a gesture
/// anyone discovers on a word they were not already suspicious of — so on a phone the underlines were
/// decoration: visible, and with nothing to do about them.
/// </para>
/// <para>
/// The bar above the keyboard is where every mobile OS already puts this, which is the reason to use
/// it rather than invent somewhere. It appears only while the caret is actually inside a misspelling,
/// so it costs nothing the rest of the time, and it puts the corrections one tap from the finger that
/// is already typing.
/// </para>
/// </remarks>
public partial class DocumentEditor : IKeyboardAccessoryHost
{
    /// <summary>How many corrections the bar offers before the rest are dropped.</summary>
    /// <remarks>
    /// Four fits a phone without scrolling, and a checker's confidence falls away fast — past the
    /// first few the list is noise competing with the ones worth reading.
    /// </remarks>
    const int MaxAccessorySuggestions = 4;

    KeyboardAccessoryBinder? accessoryBinder;
    KeyboardAccessoryView? spellingBar;
    HorizontalStackLayout? suggestionRow;
    SpellingError? barError;
    bool barAttached;

    KeyboardAccessoryBinder AccessoryBinder
        => this.accessoryBinder ??= new KeyboardAccessoryBinder(this, this.input, this);

    VisualElement IKeyboardAccessoryHost.NavigationElement => this;

    void IKeyboardAccessoryHost.DismissKeyboard() => this.input.Unfocus();

    /// <summary>
    /// Offer the caret's misspelling as tappable corrections above the keyboard. On by default.
    /// </summary>
    public static readonly BindableProperty ShowSpellingSuggestionsProperty = BindableProperty.Create(
        nameof(ShowSpellingSuggestions),
        typeof(bool),
        typeof(DocumentEditor),
        true,
        propertyChanged: (b, _, _) => ((DocumentEditor)b).RefreshSpellingAccessory());

    /// <inheritdoc cref="ShowSpellingSuggestionsProperty"/>
    public bool ShowSpellingSuggestions
    {
        get => (bool)this.GetValue(ShowSpellingSuggestionsProperty);
        set => this.SetValue(ShowSpellingSuggestionsProperty, value);
    }

    /// <summary>
    /// Brings the bar into line with wherever the caret now is.
    /// </summary>
    /// <remarks>
    /// Called on every controller change, so the early exits matter: the common case is a caret in
    /// ordinary text, which has to cost one dictionary lookup and nothing else.
    /// </remarks>
    void RefreshSpellingAccessory()
    {
        if (!this.ShowSpellingSuggestions ||
            this.IsReadOnly ||
            !this.focused ||
            this.controller is not { IsSpellCheckEnabled: true } controller)
        {
            this.DetachSpellingBar();
            return;
        }

        if (controller.SpellingErrorAt(controller.Selection.Focus) is not { } error)
        {
            this.DetachSpellingBar();
            return;
        }

        // Same word, same place, already showing: the caret moved within a misspelling the bar is
        // already offering corrections for, and rebuilding would flicker the row under the finger.
        if (this.barAttached && this.barError is { } showing && showing.Start == error.Start && showing.Word == error.Word)
            return;

        this.barError = error;
        _ = this.LoadSuggestionsAsync(controller.Selection.Focus, error);
    }

    async Task LoadSuggestionsAsync(DocumentPosition position, SpellingError error)
    {
        var controller = this.controller;
        if (controller is null)
            return;

        IReadOnlyList<string> suggestions;
        try
        {
            suggestions = await controller.SuggestAtAsync(position);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // The checker answers on its own thread — Android's on a binder thread — and everything below
        // builds views.
        await this.Dispatcher.DispatchAsync(() =>
        {
            // The caret may have moved on while the checker was thinking. Dropping a stale answer is
            // what stops the bar showing corrections for the previous word.
            if (this.barError is not { } current || current.Start != error.Start || current.Word != error.Word)
                return;

            if (suggestions.Count == 0)
            {
                // Nothing to offer, so nothing to show. A bar reading "no suggestions" takes a strip
                // of the screen away from the keyboard to say that it cannot help.
                this.DetachSpellingBar();
                return;
            }

            this.ShowSuggestions(position, error, suggestions);
        });
    }

    void ShowSuggestions(DocumentPosition position, SpellingError error, IReadOnlyList<string> suggestions)
    {
        this.EnsureSpellingBar();

        var row = this.suggestionRow!;
        row.Children.Clear();

        foreach (var suggestion in suggestions.Take(MaxAccessorySuggestions))
        {
            var value = suggestion;
            row.Children.Add(this.SuggestionChip(value, () =>
            {
                this.controller?.ApplySuggestion(position, value);
                this.input.Focus();
            }));
        }

        // The two escapes from a word the checker is wrong about. Last, and quieter, because they are
        // the answer far less often than one of the corrections is.
        row.Children.Add(new BoxView
        {
            WidthRequest = 1,
            Margin = new Thickness(4, 10),
            Color = Colors.Transparent
        }.Tinted());

        row.Children.Add(this.SuggestionChip("Ignore", () =>
        {
            this.controller?.IgnoreSpelling(error.Word);
            this.input.Focus();
        }, secondary: true));

        row.Children.Add(this.SuggestionChip("Add", () =>
        {
            this.controller?.LearnSpelling(error.Word);
            this.input.Focus();
        }, secondary: true));

        this.AttachSpellingBar();
    }

    Border SuggestionChip(string text, Action tapped, bool secondary = false)
    {
        var label = new Label
        {
            Text = text,
            FontSize = 15,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        label.SetDynamicResource(
            Label.TextColorProperty,
            secondary ? ShinyThemeKeys.Color.OnSurfaceVariant : ShinyThemeKeys.Color.OnSurface);

        var chip = new Border
        {
            Content = label,
            Padding = new Thickness(14, 6),
            Margin = new Thickness(2, 6),
            StrokeThickness = 0,
            Stroke = null,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }
        };

        chip.SetDynamicResource(
            VisualElement.BackgroundColorProperty,
            secondary ? ShinyThemeKeys.Color.SurfaceContainerHighest : ShinyThemeKeys.Color.SecondaryContainer);

        // Command, not Tapped: a handler on the event cannot be raised from a test, and this is the
        // only behaviour on the chip worth asserting.
        var tap = new TapGestureRecognizer { Command = new Command(tapped) };
        chip.GestureRecognizers.Add(tap);
        return chip;
    }

    void EnsureSpellingBar()
    {
        if (this.spellingBar is not null)
            return;

        this.suggestionRow = new HorizontalStackLayout
        {
            Spacing = 0,
            Padding = new Thickness(6, 0),
            VerticalOptions = LayoutOptions.Center
        };

        // Scrolled, because a long word's corrections are long too and the bar is one phone wide.
        this.spellingBar = new KeyboardAccessoryView
        {
            BarContent = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
                Content = this.suggestionRow
            }
        };
    }

    void AttachSpellingBar()
    {
        if (this.barAttached || this.spellingBar is null)
            return;

        this.barAttached = true;
        this.AccessoryBinder.SetBar(this.spellingBar);
    }

    /// <summary>
    /// Takes the bar off the keyboard.
    /// </summary>
    /// <remarks>
    /// Guarded on <see cref="barAttached"/> rather than called freely: setting the accessory reloads
    /// the responder's input views, and doing that on every keystroke through clean text makes the
    /// keyboard flicker.
    /// </remarks>
    void DetachSpellingBar()
    {
        this.barError = null;

        if (!this.barAttached)
            return;

        this.barAttached = false;
        this.accessoryBinder?.SetBar(null);
    }
}

static class SpellingChipExtensions
{
    /// <summary>A hairline between the corrections and the two ways of dismissing them.</summary>
    /// <remarks>
    /// BoxView.Color rather than Background: on macOS AppKit a solid Background renders transparent.
    /// </remarks>
    public static BoxView Tinted(this BoxView view)
    {
        view.SetDynamicResource(BoxView.ColorProperty, ShinyThemeKeys.Color.OutlineVariant);
        return view;
    }
}
