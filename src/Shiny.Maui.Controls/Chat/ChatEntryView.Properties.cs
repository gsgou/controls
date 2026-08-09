using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Chat;

public partial class ChatEntryView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(ChatEntryView), string.Empty,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                var view = (ChatEntryView)b;
                var text = (string?)n ?? string.Empty;
                if (view.entry.Text != text)
                    view.entry.Text = text;
            }));

    /// <summary>The current composer text. Two-way by default.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
        nameof(PlaceholderText), typeof(string), typeof(ChatEntryView), "Type a message...",
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                ((ChatEntryView)b).entry.Placeholder = (string?)n;
            }));

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public static readonly BindableProperty MaxLinesProperty = BindableProperty.Create(
        nameof(MaxLines), typeof(int), typeof(ChatEntryView), 6,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                ((ChatEntryView)b).SyncMaxHeight();
            }));

    /// <summary>
    /// How tall the entry may grow before it starts scrolling internally, in lines of text.
    /// The entry auto-sizes up to this and no further.
    /// </summary>
    public int MaxLines
    {
        get => (int)GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(ChatEntryView), 15.0,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                var view = (ChatEntryView)b;
                view.entry.FontSize = (double)n;
                view.SyncMaxHeight();
            }));

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily), typeof(string), typeof(ChatEntryView), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                ((ChatEntryView)b).entry.FontFamily = (string?)n;
            }));

    public string? FontFamily
    {
        get => (string?)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    // ------- send button -------

    public static readonly BindableProperty SendButtonTextProperty = BindableProperty.Create(
        nameof(SendButtonText), typeof(string), typeof(ChatEntryView), "Send",
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                var view = (ChatEntryView)b;
                view.defaultSendText = (string?)n ?? string.Empty;
                if (!view.IsEditing)
                    view.sendButton.Text = view.defaultSendText;
            }));

    public string SendButtonText
    {
        get => (string)GetValue(SendButtonTextProperty);
        set => SetValue(SendButtonTextProperty, value);
    }

    public static readonly BindableProperty SendButtonBackgroundColorProperty = BindableProperty.Create(
        nameof(SendButtonBackgroundColor), typeof(Color), typeof(ChatEntryView), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                // null => leave the themed value in place.
                if (n is Color color)
                    ((ChatEntryView)b).sendButton.BackgroundColor = color;
            }));

    /// <summary>Leave unset to follow the active theme.</summary>
    public Color? SendButtonBackgroundColor
    {
        get => (Color?)GetValue(SendButtonBackgroundColorProperty);
        set => SetValue(SendButtonBackgroundColorProperty, value);
    }

    public static readonly BindableProperty SendButtonTextColorProperty = BindableProperty.Create(
        nameof(SendButtonTextColor), typeof(Color), typeof(ChatEntryView), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                if (n is Color color)
                    ((ChatEntryView)b).sendButton.TextColor = color;
            }));

    /// <summary>Leave unset to follow the active theme.</summary>
    public Color? SendButtonTextColor
    {
        get => (Color?)GetValue(SendButtonTextColorProperty);
        set => SetValue(SendButtonTextColorProperty, value);
    }

    // ------- composer shape / colours -------

    public static readonly BindableProperty BarBackgroundColorProperty = BindableProperty.Create(
        nameof(BarBackgroundColor), typeof(Color), typeof(ChatEntryView), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                if (n is Color color)
                    ((ChatEntryView)b).rootGrid.BackgroundColor = color;
            }));

    /// <summary>The area behind the composer. Leave unset to follow the active theme.</summary>
    public Color? BarBackgroundColor
    {
        get => (Color?)GetValue(BarBackgroundColorProperty);
        set => SetValue(BarBackgroundColorProperty, value);
    }

    public static readonly BindableProperty ComposerBackgroundColorProperty = BindableProperty.Create(
        nameof(ComposerBackgroundColor), typeof(Color), typeof(ChatEntryView), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                if (n is Color color)
                    ((ChatEntryView)b).composerBorder.BackgroundColor = color;
            }));

    /// <summary>The fill inside the rounded composer. Leave unset to follow the active theme.</summary>
    public Color? ComposerBackgroundColor
    {
        get => (Color?)GetValue(ComposerBackgroundColorProperty);
        set => SetValue(ComposerBackgroundColorProperty, value);
    }

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(ChatEntryView), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                if (n is Color color)
                    ((ChatEntryView)b).composerBorder.Stroke = color;
            }));

    /// <summary>The rounded composer's outline. Leave unset to follow the active theme.</summary>
    public Color? BorderColor
    {
        get => (Color?)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
        nameof(BorderThickness), typeof(double), typeof(ChatEntryView), 1.0,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                ((ChatEntryView)b).composerBorder.StrokeThickness = (double)n;
            }));

    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(ChatEntryView), 24.0,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                ((ChatEntryView)b).composerBorder.StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = (double)n };
            }));

    /// <summary>Corner radius of the rounded composer. Set it to half the collapsed height for a pill.</summary>
    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    // ------- optional buttons -------

    public static readonly BindableProperty ShowAttachButtonProperty = BindableProperty.Create(
        nameof(ShowAttachButton), typeof(bool), typeof(ChatEntryView), false,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                ((ChatEntryView)b).attachButton.IsVisible = (bool)n;
            }));

    public bool ShowAttachButton
    {
        get => (bool)GetValue(ShowAttachButtonProperty);
        set => SetValue(ShowAttachButtonProperty, value);
    }

    public static readonly BindableProperty LeftToolbarProperty = BindableProperty.Create(
        nameof(LeftToolbar), typeof(IList<IView>), typeof(ChatEntryView), null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                ((ChatEntryView)b).HookToolbar(o, n, left: true);
            }));

    /// <summary>
    /// Views appended to the left of the control row beneath the entry, after the attach / overflow
    /// buttons. Drop a segmented control or a mode picker in here.
    /// </summary>
    public IList<IView>? LeftToolbar
    {
        get => (IList<IView>?)GetValue(LeftToolbarProperty);
        set => SetValue(LeftToolbarProperty, value);
    }

    public static readonly BindableProperty RightToolbarProperty = BindableProperty.Create(
        nameof(RightToolbar), typeof(IList<IView>), typeof(ChatEntryView), null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                ((ChatEntryView)b).HookToolbar(o, n, left: false);
            }));

    /// <summary>
    /// Views placed to the right of the control row, immediately before the send button. Model
    /// pickers, a mic button, and the like.
    /// </summary>
    public IList<IView>? RightToolbar
    {
        get => (IList<IView>?)GetValue(RightToolbarProperty);
        set => SetValue(RightToolbarProperty, value);
    }

    public static readonly BindableProperty ShowActionsButtonProperty = BindableProperty.Create(
        nameof(ShowActionsButton), typeof(bool), typeof(ChatEntryView), false,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ChatEntryView), () =>
            {
                ((ChatEntryView)b).actionsButton.IsVisible = (bool)n;
            }));

    public bool ShowActionsButton
    {
        get => (bool)GetValue(ShowActionsButtonProperty);
        set => SetValue(ShowActionsButtonProperty, value);
    }
}
