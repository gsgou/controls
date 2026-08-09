using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls;

public partial class TextEntry : IDisposable
{
    [Inject] IJSRuntime JS { get; set; } = default!;

    ElementReference inputRef;
    bool IsFocused;
    TextEntryContext? context;
    bool needsCursorUpdate;
    int pendingCursorPosition;
    bool IsPlaceholderUp => IsFocused || !string.IsNullOrEmpty(Text);

    string DisplayText => !string.IsNullOrEmpty(Mask)
        ? TextEntryMaskHelper.ApplyMask(Text, Mask)
        : Text;

    // Parameters
    [Parameter] public string Text { get; set; } = "";
    [Parameter] public EventCallback<string> TextChanged { get; set; }
    [Parameter] public string Placeholder { get; set; } = "";

    /// <summary>Classic (browser placeholder) or Floating (M3 notched outline). Defaults to Classic, matching MAUI.</summary>
    [Parameter] public TextEntryVariant Variant { get; set; } = TextEntryVariant.Classic;

    /// <summary>How docked tools are painted. Defaults to Inline.</summary>
    [Parameter] public TextEntryToolStyle ToolStyle { get; set; } = TextEntryToolStyle.Inline;

    // Colour parameters fall through to theme tokens when unset, rather than the hard-coded greys
    // they used to default to - a themed app got a text field that ignored its own palette.
    [Parameter] public string? PlaceholderColor { get; set; }
    [Parameter] public string? FocusedPlaceholderColor { get; set; }
    [Parameter] public string? BorderColor { get; set; }
    [Parameter] public string? FocusedBorderColor { get; set; }
    [Parameter] public double BorderThickness { get; set; } = 1;
    [Parameter] public double FocusedBorderThickness { get; set; } = 2;
    [Parameter] public string CornerRadius { get; set; } = "8px";
    [Parameter] public string? EntryBackgroundColor { get; set; }
    [Parameter] public double FontSize { get; set; } = 15;
    [Parameter] public string FontFamily { get; set; } = "inherit";
    [Parameter] public string? TextColor { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public bool IsPassword { get; set; }
    [Parameter] public int MaxLength { get; set; }
    [Parameter] public string? HintText { get; set; }
    [Parameter] public string? HintColor { get; set; }
    [Parameter] public bool HasError { get; set; }
    [Parameter] public string? ErrorColor { get; set; }
    [Parameter] public bool ShowCharacterCount { get; set; }
    [Parameter] public string? Mask { get; set; }
    [Parameter] public string FormattedText { get; set; } = "";
    [Parameter] public EventCallback<string> FormattedTextChanged { get; set; }
    [Parameter] public List<TextEntryTool>? LeftTools { get; set; }
    [Parameter] public List<TextEntryTool>? RightTools { get; set; }
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public EventCallback Completed { get; set; }

    /// <summary>
    /// When false the browser's autofill, autocorrect, auto-capitalisation and spell check are all
    /// switched off, and the common password managers are told to keep out. Use it for serials,
    /// coupon codes and anything else the browser has no business rewriting.
    /// </summary>
    [Parameter] public bool IsAutoCompleteEnabled { get; set; } = true;

    /// <summary>Spell check. Forced off while <see cref="IsAutoCompleteEnabled"/> is false.</summary>
    [Parameter] public bool IsSpellCheckEnabled { get; set; } = true;

    /// <summary>Autocorrect / auto-capitalisation. Forced off while <see cref="IsAutoCompleteEnabled"/> is false.</summary>
    [Parameter] public bool IsTextPredictionEnabled { get; set; } = true;

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    // ---- theme-token fallbacks -------------------------------------------------------------
    const string OutlineToken = "var(--shiny-color-outline, #CBD5E1)";
    const string PrimaryToken = "var(--shiny-color-primary, #007AFF)";
    const string ErrorToken = "var(--shiny-color-error, #DC3545)";
    const string OnSurfaceToken = "var(--shiny-color-on-surface, inherit)";
    const string OnSurfaceVariantToken = "var(--shiny-color-on-surface-variant, #6B7280)";
    const string SurfaceToken = "var(--shiny-color-surface, #FFFFFF)";

    string PlaceholderColorValue => PlaceholderColor ?? OnSurfaceVariantToken;
    string FocusedPlaceholderColorValue => FocusedPlaceholderColor ?? PrimaryToken;
    string BorderColorValue => BorderColor ?? OutlineToken;
    string FocusedBorderColorValue => FocusedBorderColor ?? PrimaryToken;
    string EntryBackgroundColorValue => EntryBackgroundColor ?? SurfaceToken;
    string TextColorValue => TextColor ?? OnSurfaceToken;
    string HintColorValue => HintColor ?? OnSurfaceVariantToken;
    string ErrorColorValue => ErrorColor ?? ErrorToken;

    string VariantClass => Variant == TextEntryVariant.Floating ? "floating" : "classic";
    string ToolStyleClass => ToolStyle == TextEntryToolStyle.Addon ? "addon" : "inline";

    string InputType => IsPassword ? "password" : "text";
    string? InputMode => !string.IsNullOrEmpty(Mask) ? "numeric" : null;
    int? InputMaxLength => !string.IsNullOrEmpty(Mask) ? Mask.Length : (MaxLength > 0 ? MaxLength : null);

    // Classic hands the placeholder to the browser; Floating draws its own label instead.
    string? ClassicPlaceholder => Variant == TextEntryVariant.Classic && !string.IsNullOrEmpty(Placeholder)
        ? Placeholder
        : null;

    /// <summary>
    /// autocomplete="off" alone is widely ignored by Chrome and by password managers, so the opt-out
    /// has to be spelled out: a non-standard token for the browsers that honour one, the autocorrect /
    /// autocapitalize pair for mobile Safari, and the manager-specific data attributes.
    /// </summary>
    Dictionary<string, object> InputAssistanceAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>
            {
                ["autocorrect"] = IsAutoCompleteEnabled && IsTextPredictionEnabled ? "on" : "off",
                ["autocapitalize"] = IsAutoCompleteEnabled && IsTextPredictionEnabled ? "sentences" : "off",
                ["spellcheck"] = IsAutoCompleteEnabled && IsSpellCheckEnabled ? "true" : "false"
            };

            if (IsAutoCompleteEnabled)
            {
                attributes["autocomplete"] = "on";
            }
            else
            {
                attributes["autocomplete"] = IsPassword ? "new-password" : "off";
                attributes["data-lpignore"] = "true";   // LastPass
                attributes["data-1p-ignore"] = "true";  // 1Password
                attributes["data-form-type"] = "other"; // Dashlane
            }

            return attributes;
        }
    }

    string HintDisplay
    {
        get
        {
            if (HasError && !string.IsNullOrEmpty(HintText))
                return HintText;
            if (!string.IsNullOrEmpty(HintText))
                return HintText;
            if (ShowCharacterCount && MaxLength > 0)
                return $"{(Text?.Length ?? 0)}/{MaxLength}";
            return "";
        }
    }

    string CurrentBorderColor => HasError ? ErrorColorValue : (IsFocused ? FocusedBorderColorValue : BorderColorValue);

    string RootStyle => "";

    string BorderStyle
    {
        get
        {
            var color = CurrentBorderColor;
            var thickness = IsFocused ? FocusedBorderThickness : BorderThickness;
            return $"border: {thickness}px solid {color}; border-radius: {CornerRadius}; background: {EntryBackgroundColorValue};" +
                   $" --shiny-te-border-color: {color}; --shiny-te-notch-bg: {EntryBackgroundColorValue};";
        }
    }

    string PlaceholderStyle
    {
        get
        {
            // Match MAUI: only a focused field accents its label. A floated-but-unfocused label on
            // every filled row of a form is noise.
            var color = HasError
                ? ErrorColorValue
                : IsFocused ? FocusedPlaceholderColorValue : PlaceholderColorValue;

            return $"color: {color};";
        }
    }

    string InputStyle => $"font-size: {FontSize}px; font-family: {FontFamily}; color: {TextColorValue};";

    string ToolStyleFor(TextEntryTool tool)
        => tool.ToolColor is null ? "" : $"color: {tool.ToolColor};";

    string HintStyle => $"color: {(HasError ? ErrorColorValue : HintColorValue)};";

    protected override void OnInitialized()
    {
        context = new TextEntryContext(() => Text, SetTextFromTool);
        AttachTools(LeftTools);
        AttachTools(RightTools);
    }

    protected override void OnParametersSet()
    {
        // Re-attach if tool lists changed
        AttachTools(LeftTools);
        AttachTools(RightTools);
    }

    async Task OnInput(ChangeEventArgs e)
    {
        var input = e.Value?.ToString() ?? "";

        if (!string.IsNullOrEmpty(Mask))
        {
            var rawText = TextEntryMaskHelper.StripMask(input, Mask);
            var maxRaw = TextEntryMaskHelper.CalculateRawMaxLength(Mask);
            if (rawText.Length > maxRaw)
                rawText = rawText[..maxRaw];

            Text = rawText;
            FormattedText = DisplayText;
            await FormattedTextChanged.InvokeAsync(FormattedText);

            // Schedule cursor position update
            pendingCursorPosition = TextEntryMaskHelper.CalculateCursorPosition(rawText.Length, Mask);
            needsCursorUpdate = true;
        }
        else
        {
            Text = input;
        }

        await TextChanged.InvokeAsync(Text);
        NotifyToolsTextChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (needsCursorUpdate)
        {
            needsCursorUpdate = false;
            try
            {
                await JS.InvokeVoidAsync("shinyControls.setCursorPosition", inputRef, pendingCursorPosition);
            }
            catch { /* element may not be available */ }
        }
    }

    void OnFocusIn()
    {
        IsFocused = true;
    }

    async Task OnFocusOut()
    {
        IsFocused = false;
        StateHasChanged();
        await Task.CompletedTask;
    }

    async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await Completed.InvokeAsync();
    }

    async Task FocusInput()
    {
        try { await inputRef.FocusAsync(); } catch { }
    }

    void OnToolClicked(TextEntryTool tool)
    {
        tool.InternalClick();
        StateHasChanged();
    }

    async void SetTextFromTool(string text)
    {
        Text = text;
        await TextChanged.InvokeAsync(text);
        NotifyToolsTextChanged();
        StateHasChanged();
    }

    void NotifyToolsTextChanged()
    {
        NotifyToolsInList(LeftTools);
        NotifyToolsInList(RightTools);
    }

    void NotifyToolsInList(List<TextEntryTool>? tools)
    {
        if (tools is null) return;
        foreach (var tool in tools)
            tool.OnTextChanged(Text);
    }

    void AttachTools(List<TextEntryTool>? tools)
    {
        if (tools is null || context is null) return;
        foreach (var tool in tools)
        {
            if (tool._context is null)
                tool.InternalAttach(context);
        }
    }

    void DetachTools(List<TextEntryTool>? tools)
    {
        if (tools is null) return;
        foreach (var tool in tools)
            tool.InternalDetach();
    }

    public void Dispose()
    {
        DetachTools(LeftTools);
        DetachTools(RightTools);
    }
}
