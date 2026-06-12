using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls;

[ContentProperty(nameof(RightTools))]
public partial class TextEntry : ContentView
{
    // Floating-label animation targets
    const double PlaceholderTranslationY = -14;
    const double PlaceholderScaledSize = 0.85;
    const uint AnimationDuration = 150;

    // Bootstrap form-control sizing
    const double ClassicMinHeight = 38;
    const double FloatingMinHeight = 58;
    const double EntryHorizontalPadding = 12;
    const double ClassicVerticalPadding = 6;
    const double FloatingVerticalPaddingTop = 18;
    const double FloatingVerticalPaddingBottom = 6;

    readonly Border outerBorder;
    readonly Microsoft.Maui.Controls.Shapes.RoundRectangle borderShape;
    readonly Grid contentGrid;
    readonly Grid entryArea;
    readonly HorizontalStackLayout leftToolsLayout;
    readonly HorizontalStackLayout rightToolsLayout;
    readonly BoxView leftSeparator;
    readonly BoxView rightSeparator;
    readonly Label placeholderLabel;
    readonly BorderlessEntry entry;
    readonly Label hintLabel;
    readonly Grid rootGrid;

    bool suppressTextChanged;
    bool isPlaceholderUp;

    // Internal event for tools (like ClearButtonTool) to observe text changes
    internal event EventHandler? InternalTextChanged;

    public TextEntry()
    {
        placeholderLabel = new Label
        {
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Start,
            InputTransparent = true,
            AnchorX = 0 // Scale from left edge
        };

        entry = new BorderlessEntry
        {
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
            BackgroundColor = Colors.Transparent
        };
        entry.SetDynamicResource(Entry.TextColorProperty, ShinyThemeKeys.Color.OnSurface);
        entry.SetDynamicResource(Entry.PlaceholderColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
        entry.TextChanged += OnEntryTextChanged;
        entry.Focused += OnEntryFocused;
        entry.Unfocused += OnEntryUnfocused;
        entry.Completed += OnEntryCompleted;

        entryArea = new Grid
        {
            Padding = new Thickness(EntryHorizontalPadding, ClassicVerticalPadding),
            Children = { placeholderLabel, entry }
        };

        // Tool "addon" surface → surface-container-high token (was #E9ECEF)
        leftToolsLayout = new HorizontalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Fill
        };
        leftToolsLayout.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.SurfaceContainerHigh);

        rightToolsLayout = new HorizontalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Fill
        };
        rightToolsLayout.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.SurfaceContainerHigh);

        leftSeparator = new BoxView
        {
            WidthRequest = 1,
            VerticalOptions = LayoutOptions.Fill,
            IsVisible = false
        };

        rightSeparator = new BoxView
        {
            WidthRequest = 1,
            VerticalOptions = LayoutOptions.Fill,
            IsVisible = false
        };

        contentGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),  // left tools
                new ColumnDefinition(GridLength.Auto),  // left separator
                new ColumnDefinition(GridLength.Star),  // entry area
                new ColumnDefinition(GridLength.Auto),  // right separator
                new ColumnDefinition(GridLength.Auto)   // right tools
            },
            ColumnSpacing = 0,
            Padding = 0
        };
        contentGrid.Add(leftToolsLayout, 0, 0);
        contentGrid.Add(leftSeparator, 1, 0);
        contentGrid.Add(entryArea, 2, 0);
        contentGrid.Add(rightSeparator, 3, 0);
        contentGrid.Add(rightToolsLayout, 4, 0);

        borderShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 };
        outerBorder = new Border
        {
            StrokeShape = borderShape,
            StrokeThickness = 1,
            Padding = 0,
            Content = contentGrid,
            MinimumHeightRequest = ClassicMinHeight
        };
        outerBorder.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Surface);

        hintLabel = new Label
        {
            FontSize = 12,
            Margin = new Thickness(2, 4, 2, 0),
            IsVisible = false
        };

        rootGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            }
        };
        rootGrid.Add(outerBorder, 0, 0);
        rootGrid.Add(hintLabel, 0, 1);

        Content = rootGrid;

        // Initialize tool collections
        LeftTools = new ObservableCollection<TextEntryTool>();
        RightTools = new ObservableCollection<TextEntryTool>();

        // Apply default variant (Classic) so the placeholder lives on the native entry.
        ApplyVariant();

        // Seed resting border/separator + placeholder colors (explicit-or-theme-token).
        ApplyBorderState();
        ApplyPlaceholderRestColor();
    }

    // ---- Theme token defaults for each logical color (used when no explicit Color is set) ----
    const string PlaceholderColorToken = ShinyThemeKeys.Color.OnSurfaceVariant;
    const string FocusedPlaceholderColorToken = ShinyThemeKeys.Color.Primary;
    const string BorderColorToken = ShinyThemeKeys.Color.Outline;
    const string FocusedBorderColorToken = ShinyThemeKeys.Color.Primary;
    const string ErrorColorToken = ShinyThemeKeys.Color.Error;
    const string HintColorToken = ShinyThemeKeys.Color.OnSurfaceVariant;

    // Applies an explicit color when set, otherwise binds the theme token, to the border stroke
    // (a Brush, so we drive a SolidColorBrush's Color) and the tool separators.
    void ApplyStroke(Color? explicitColor, string token)
    {
        if (explicitColor is Color c)
        {
            outerBorder.Stroke = c;
            leftSeparator.Color = c;
            rightSeparator.Color = c;
        }
        else
        {
            var brush = new SolidColorBrush();
            brush.SetDynamicResource(SolidColorBrush.ColorProperty, token);
            outerBorder.Stroke = brush;
            leftSeparator.SetDynamicResource(BoxView.ColorProperty, token);
            rightSeparator.SetDynamicResource(BoxView.ColorProperty, token);
        }
    }

    // Re-applies the border/separator color for the current state (error > focused > resting).
    void ApplyBorderState()
    {
        if (HasError)
            ApplyStroke(ErrorColor, ErrorColorToken);
        else if (entry.IsFocused)
            ApplyStroke(FocusedBorderColor, FocusedBorderColorToken);
        else
            ApplyStroke(BorderColor, BorderColorToken);
    }

    // Resting (down) placeholder color.
    void ApplyPlaceholderRestColor()
    {
        if (PlaceholderColor is Color c)
            placeholderLabel.TextColor = c;
        else
            placeholderLabel.SetDynamicResource(Label.TextColorProperty, PlaceholderColorToken);
    }

    // Floated (up) placeholder color — error wins, else focused accent.
    void ApplyPlaceholderFloatColor()
    {
        if (HasError)
        {
            if (ErrorColor is Color ec)
                placeholderLabel.TextColor = ec;
            else
                placeholderLabel.SetDynamicResource(Label.TextColorProperty, ErrorColorToken);
        }
        else if (FocusedPlaceholderColor is Color fc)
        {
            placeholderLabel.TextColor = fc;
        }
        else
        {
            placeholderLabel.SetDynamicResource(Label.TextColorProperty, FocusedPlaceholderColorToken);
        }
    }

    // Hint label color for the current state.
    void ApplyHintColor(bool error)
    {
        if (error)
        {
            if (ErrorColor is Color ec)
                hintLabel.TextColor = ec;
            else
                hintLabel.SetDynamicResource(Label.TextColorProperty, ErrorColorToken);
        }
        else if (HintColor is Color hc)
        {
            hintLabel.TextColor = hc;
        }
        else
        {
            hintLabel.SetDynamicResource(Label.TextColorProperty, HintColorToken);
        }
    }

    void ApplyVariant()
    {
        if (Variant == TextEntryVariant.Classic)
        {
            placeholderLabel.IsVisible = false;
            entry.Placeholder = Placeholder;
            entryArea.Padding = new Thickness(EntryHorizontalPadding, ClassicVerticalPadding);
            outerBorder.MinimumHeightRequest = ClassicMinHeight;
        }
        else
        {
            placeholderLabel.IsVisible = true;
            entry.Placeholder = string.Empty;
            entryArea.Padding = new Thickness(EntryHorizontalPadding, FloatingVerticalPaddingTop, EntryHorizontalPadding, FloatingVerticalPaddingBottom);
            outerBorder.MinimumHeightRequest = FloatingMinHeight;

            // Snap the placeholder to the correct rest position for the current text.
            isPlaceholderUp = false;
            placeholderLabel.TranslationY = 0;
            placeholderLabel.Scale = 1;
            if (!string.IsNullOrEmpty(entry.Text) || entry.IsFocused)
                AnimatePlaceholder(true);
        }
    }

    void ApplyPlaceholder(string text)
    {
        placeholderLabel.Text = text;
        if (Variant == TextEntryVariant.Classic)
            entry.Placeholder = text;
    }

    Shadow BuildFocusGlow(Color? color, string token)
    {
        SolidColorBrush brush;
        if (color is Color c)
        {
            brush = new SolidColorBrush(c);
        }
        else
        {
            brush = new SolidColorBrush();
            brush.SetDynamicResource(SolidColorBrush.ColorProperty, token);
        }
        return new Shadow
        {
            Brush = brush,
            Offset = Point.Zero,
            Radius = 8,
            Opacity = 0.35f
        };
    }

    void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (suppressTextChanged) return;

        suppressTextChanged = true;

        if (!string.IsNullOrEmpty(Mask))
        {
            var rawText = TextEntryMaskHelper.StripMask(entry.Text, Mask);
            var maxRaw = TextEntryMaskHelper.CalculateRawMaxLength(Mask);
            if (rawText.Length > maxRaw)
                rawText = rawText[..maxRaw];

            Text = rawText;
            var formatted = TextEntryMaskHelper.ApplyMask(rawText, Mask);
            FormattedText = formatted;
            entry.Text = formatted;

            // Set cursor position after formatting
            var cursorPos = TextEntryMaskHelper.CalculateCursorPosition(rawText.Length, Mask);
            Dispatcher.Dispatch(() => entry.CursorPosition = Math.Min(cursorPos, formatted.Length));
        }
        else
        {
            Text = entry.Text ?? string.Empty;
        }

        suppressTextChanged = false;

        InternalTextChanged?.Invoke(this, EventArgs.Empty);
        TextChanged?.Invoke(this, new TextChangedEventArgs(e.OldTextValue, Text));
        if (TextChangedCommand?.CanExecute(Text) == true)
            TextChangedCommand.Execute(Text);

        UpdateCharacterCount();
    }

    void OnEntryFocused(object? sender, FocusEventArgs e)
    {
        if (Variant == TextEntryVariant.Floating)
            AnimatePlaceholder(true);

        if (HasError)
            ApplyStroke(ErrorColor, ErrorColorToken);
        else
            ApplyStroke(FocusedBorderColor, FocusedBorderColorToken);

        outerBorder.StrokeThickness = FocusedBorderThickness;
        outerBorder.Shadow = HasError
            ? BuildFocusGlow(ErrorColor, ErrorColorToken)
            : BuildFocusGlow(FocusedBorderColor, FocusedBorderColorToken);
    }

    void OnEntryUnfocused(object? sender, FocusEventArgs e)
    {
        if (Variant == TextEntryVariant.Floating && string.IsNullOrEmpty(entry.Text))
            AnimatePlaceholder(false);

        if (HasError)
            ApplyStroke(ErrorColor, ErrorColorToken);
        else
            ApplyStroke(BorderColor, BorderColorToken);

        outerBorder.StrokeThickness = BorderThickness;
        outerBorder.Shadow = HasError ? BuildFocusGlow(ErrorColor, ErrorColorToken) : null!;
    }

    void OnEntryCompleted(object? sender, EventArgs e)
    {
        Completed?.Invoke(this, e);
        if (CompletedCommand?.CanExecute(Text) == true)
            CompletedCommand.Execute(Text);
    }

    async void AnimatePlaceholder(bool up)
    {
        if (Variant != TextEntryVariant.Floating) return;
        if (up == isPlaceholderUp) return;
        isPlaceholderUp = up;

        if (up)
        {
            await Task.WhenAll(
                placeholderLabel.TranslateToAsync(0, PlaceholderTranslationY, AnimationDuration, Easing.CubicOut),
                placeholderLabel.ScaleToAsync(PlaceholderScaledSize, AnimationDuration, Easing.CubicOut)
            );
            ApplyPlaceholderFloatColor();
        }
        else
        {
            await Task.WhenAll(
                placeholderLabel.TranslateToAsync(0, 0, AnimationDuration, Easing.CubicOut),
                placeholderLabel.ScaleToAsync(1, AnimationDuration, Easing.CubicOut)
            );
            ApplyPlaceholderRestColor();
        }
    }

    void UpdateCharacterCount()
    {
        if (!ShowCharacterCount || MaxLength <= 0) return;
        var count = entry.Text?.Length ?? 0;
        // Show in hint when no error
        if (!HasError)
        {
            hintLabel.Text = $"{count}/{MaxLength}";
            ApplyHintColor(count >= MaxLength);
            hintLabel.IsVisible = true;
        }
    }

    void SyncHint()
    {
        if (HasError && !string.IsNullOrEmpty(HintText))
        {
            hintLabel.Text = HintText;
            ApplyHintColor(true);
            hintLabel.IsVisible = true;
        }
        else if (!string.IsNullOrEmpty(HintText))
        {
            hintLabel.Text = HintText;
            ApplyHintColor(false);
            hintLabel.IsVisible = true;
        }
        else if (ShowCharacterCount && MaxLength > 0)
        {
            UpdateCharacterCount();
        }
        else
        {
            hintLabel.IsVisible = false;
        }

        // Border color: error > focused > resting.
        if (HasError)
            ApplyStroke(ErrorColor, ErrorColorToken);
        else if (entry.IsFocused)
            ApplyStroke(FocusedBorderColor, FocusedBorderColorToken);
        else
            ApplyStroke(BorderColor, BorderColorToken);

        if (HasError)
            outerBorder.Shadow = BuildFocusGlow(ErrorColor, ErrorColorToken);
        else if (entry.IsFocused)
            outerBorder.Shadow = BuildFocusGlow(FocusedBorderColor, FocusedBorderColorToken);
        else
            outerBorder.Shadow = null!;
    }

    // Tool collection management
    void OnToolsChanged(IList<TextEntryTool>? oldTools, IList<TextEntryTool>? newTools, HorizontalStackLayout layout)
    {
        if (oldTools is INotifyCollectionChanged oldNcc)
            oldNcc.CollectionChanged -= (_, _) => RebuildTools(newTools, layout);

        DetachTools(oldTools);
        RebuildTools(newTools, layout);
        AttachTools(newTools);

        if (newTools is INotifyCollectionChanged ncc)
            ncc.CollectionChanged += (_, _) =>
            {
                DetachTools(newTools);
                RebuildTools(newTools, layout);
                AttachTools(newTools);
            };
    }

    void RebuildTools(IList<TextEntryTool>? tools, HorizontalStackLayout layout)
    {
        layout.Children.Clear();
        if (tools is null || tools.Count == 0)
        {
            layout.IsVisible = false;
            if (layout == leftToolsLayout)
                leftSeparator.IsVisible = false;
            else
                rightSeparator.IsVisible = false;
            return;
        }

        layout.IsVisible = true;
        if (layout == leftToolsLayout)
            leftSeparator.IsVisible = true;
        else
            rightSeparator.IsVisible = true;

        foreach (var tool in tools)
        {
            tool.ParentEntry = this;
            layout.Children.Add(tool);
        }
    }

    void AttachTools(IList<TextEntryTool>? tools)
    {
        if (tools is null) return;
        foreach (var tool in tools)
        {
            if (tool is ITextEntryAwareTool aware)
                aware.Attach(this);
        }
    }

    void DetachTools(IList<TextEntryTool>? tools)
    {
        if (tools is null) return;
        foreach (var tool in tools)
        {
            if (tool is ITextEntryAwareTool aware)
                aware.Detach();
        }
    }

    void OnMaskChanged()
    {
        if (!string.IsNullOrEmpty(Mask))
        {
            entry.Keyboard = Keyboard.Numeric;
            entry.MaxLength = Mask.Length;

            // Reformat existing text
            if (!string.IsNullOrEmpty(Text))
            {
                suppressTextChanged = true;
                var formatted = TextEntryMaskHelper.ApplyMask(Text, Mask);
                FormattedText = formatted;
                entry.Text = formatted;
                suppressTextChanged = false;
            }
        }
        else
        {
            // Mask removed - restore raw text to entry
            entry.MaxLength = MaxLength;
            suppressTextChanged = true;
            entry.Text = Text;
            FormattedText = string.Empty;
            suppressTextChanged = false;
        }
    }

    void ApplyMaskToEntry()
    {
        if (string.IsNullOrEmpty(Mask)) return;

        var formatted = TextEntryMaskHelper.ApplyMask(Text, Mask);
        FormattedText = formatted;

        suppressTextChanged = true;
        entry.Text = formatted;
        suppressTextChanged = false;

        if (!string.IsNullOrEmpty(formatted) && !isPlaceholderUp)
            AnimatePlaceholder(true);
        else if (string.IsNullOrEmpty(formatted) && !entry.IsFocused && isPlaceholderUp)
            AnimatePlaceholder(false);
    }

    // Public API
    public event EventHandler<TextChangedEventArgs>? TextChanged;
    public event EventHandler? Completed;

    public new bool Focus() => entry.Focus();
    public new void Unfocus() => entry.Unfocus();
}
