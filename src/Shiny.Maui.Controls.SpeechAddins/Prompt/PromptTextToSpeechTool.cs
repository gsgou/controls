using System.Globalization;
using Shiny.Maui.Controls.QuickEntry;
using Shiny.Speech;

namespace Shiny.Maui.Controls.SpeechAddins.QuickEntry;

/// <summary>
/// A <see cref="PromptTool"/> that reads the prompt's answer aloud with
/// <see cref="ITextToSpeechService"/>. Drop it into <see cref="PromptView.TrailingTools"/>.
/// </summary>
/// <remarks>
/// <para>
/// It speaks <see cref="PromptView.Response"/> — the plain-text answer. A response pushed through
/// <see cref="PromptView.ResponseContent"/> alone is a <c>View</c> with no text to read, so set
/// <see cref="TextSelector"/> to pull the words out of whatever you rendered.
/// </para>
/// <para>
/// The glyph is a toggle: it becomes <see cref="SpeakingText"/> while speaking and tapping again
/// stops. With <see cref="AutoSpeak"/> the answer is read the moment it lands.
/// </para>
/// </remarks>
public class PromptTextToSpeechTool : PromptTool, IPromptAwareTool
{
    const string IdleGlyph = "\U0001F50A"; // speaker
    const string StopGlyph = "⏹";     // stop

    PromptView? prompt;
    CancellationTokenSource? cts;
    bool isSpeaking;
    int generation;

    public PromptTextToSpeechTool()
    {
        this.Text = IdleGlyph;
        this.Clicked += this.OnClicked;
    }

    // ------- Configuration -------

    public static readonly BindableProperty SpeechRateProperty = BindableProperty.Create(
        nameof(SpeechRate), typeof(float), typeof(PromptTextToSpeechTool), 1.0f);

    public float SpeechRate
    {
        get => (float)this.GetValue(SpeechRateProperty);
        set => this.SetValue(SpeechRateProperty, value);
    }

    public static readonly BindableProperty PitchProperty = BindableProperty.Create(
        nameof(Pitch), typeof(float), typeof(PromptTextToSpeechTool), 1.0f);

    public float Pitch
    {
        get => (float)this.GetValue(PitchProperty);
        set => this.SetValue(PitchProperty, value);
    }

    public static readonly BindableProperty VolumeProperty = BindableProperty.Create(
        nameof(Volume), typeof(float), typeof(PromptTextToSpeechTool), 1.0f);

    public float Volume
    {
        get => (float)this.GetValue(VolumeProperty);
        set => this.SetValue(VolumeProperty, value);
    }

    public static readonly BindableProperty VoiceNameProperty = BindableProperty.Create(
        nameof(VoiceName), typeof(string), typeof(PromptTextToSpeechTool));

    /// <summary>The name of the voice to use. Null uses the system default.</summary>
    public string? VoiceName
    {
        get => (string?)this.GetValue(VoiceNameProperty);
        set => this.SetValue(VoiceNameProperty, value);
    }

    public static readonly BindableProperty CultureProperty = BindableProperty.Create(
        nameof(Culture), typeof(string), typeof(PromptTextToSpeechTool));

    /// <summary>Culture code (e.g. "en-US") for voice selection. Null uses the system default.</summary>
    public string? Culture
    {
        get => (string?)this.GetValue(CultureProperty);
        set => this.SetValue(CultureProperty, value);
    }

    public static readonly BindableProperty AutoSpeakProperty = BindableProperty.Create(
        nameof(AutoSpeak), typeof(bool), typeof(PromptTextToSpeechTool), false);

    /// <summary>Speak the answer as soon as it arrives, rather than waiting for a tap.</summary>
    public bool AutoSpeak
    {
        get => (bool)this.GetValue(AutoSpeakProperty);
        set => this.SetValue(AutoSpeakProperty, value);
    }

    public static readonly BindableProperty HideWhenEmptyProperty = BindableProperty.Create(
        nameof(HideWhenEmpty), typeof(bool), typeof(PromptTextToSpeechTool), true,
        propertyChanged: (b, _, _) => ((PromptTextToSpeechTool)b).UpdateVisibility());

    /// <summary>Hide the tool while there is nothing to read. Default true.</summary>
    public bool HideWhenEmpty
    {
        get => (bool)this.GetValue(HideWhenEmptyProperty);
        set => this.SetValue(HideWhenEmptyProperty, value);
    }

    public static readonly BindableProperty SpeakingTextProperty = BindableProperty.Create(
        nameof(SpeakingText), typeof(string), typeof(PromptTextToSpeechTool), StopGlyph);

    /// <summary>Glyph shown while speaking. Tapping it stops.</summary>
    public string SpeakingText
    {
        get => (string)this.GetValue(SpeakingTextProperty);
        set => this.SetValue(SpeakingTextProperty, value);
    }

    public static readonly BindableProperty SpeakingColorProperty = BindableProperty.Create(
        nameof(SpeakingColor), typeof(Color), typeof(PromptTextToSpeechTool),
        Color.FromArgb("#F44336"));

    public Color SpeakingColor
    {
        get => (Color)this.GetValue(SpeakingColorProperty);
        set => this.SetValue(SpeakingColorProperty, value);
    }

    /// <summary>
    /// Chooses what to read. Null (the default) reads <see cref="PromptView.Response"/> — set this
    /// when the answer only exists inside <see cref="PromptView.ResponseContent"/>.
    /// </summary>
    public Func<PromptView, string?>? TextSelector { get; set; }

    /// <summary>Whether the tool is currently speaking.</summary>
    public bool IsSpeaking => this.isSpeaking;

    // ------- IPromptAwareTool -------

    void IPromptAwareTool.Attach(PromptView view)
    {
        this.prompt = view;
        this.prompt.ResponseChanged += this.OnResponseChanged;
        this.UpdateVisibility();
    }

    void IPromptAwareTool.Detach()
    {
        _ = this.StopAsync();
        if (this.prompt is not null)
            this.prompt.ResponseChanged -= this.OnResponseChanged;

        this.prompt = null;
    }

    // ------- Core logic -------

    void OnResponseChanged(object? sender, EventArgs e)
    {
        this.UpdateVisibility();

        // A new answer invalidates whatever was being read for the old one.
        _ = this.RestartAsync();
    }

    async Task RestartAsync()
    {
        await this.StopAsync();

        if (this.AutoSpeak && !String.IsNullOrWhiteSpace(this.ResolveText()))
            this.Speak();
    }

    void UpdateVisibility()
    {
        if (this.HideWhenEmpty)
            this.IsVisible = !String.IsNullOrWhiteSpace(this.ResolveText());
        else
            this.IsVisible = true;
    }

    string? ResolveText()
    {
        if (this.prompt is null)
            return null;

        return this.TextSelector is { } selector
            ? selector(this.prompt)
            : this.prompt.Response;
    }

    void OnClicked(object? sender, EventArgs e)
    {
        if (this.isSpeaking)
            _ = this.StopAsync();
        else
            this.Speak();
    }

    /// <summary>Read the current answer aloud. Same path as tapping the tool.</summary>
    public void Speak()
    {
        var text = this.ResolveText();
        if (!String.IsNullOrWhiteSpace(text))
            _ = this.SpeakAsync(text!);
    }

    /// <summary>Stop reading, if it is reading.</summary>
    public void Stop() => _ = this.StopAsync();

    /// <summary>Stop reading and wait for the engine to settle.</summary>
    public async Task StopAsync()
    {
        // Both halves are needed. Cancelling releases the await, but the synthesiser is a separate
        // process that is already talking and does not stop because a token was signalled — and the
        // glyph is reset here rather than in the run's finally because a token cancellation is not
        // guaranteed to surface as one.
        this.cts?.Cancel();

        var tts = ResolveService<ITextToSpeechService>();
        if (tts is not null)
        {
            try
            {
                await tts.StopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PromptTextToSpeechTool: {ex.Message}");
            }
        }

        this.isSpeaking = false;
        this.SetSpeakingAppearance(false);
    }

    async Task SpeakAsync(string text)
    {
        var tts = ResolveService<ITextToSpeechService>();
        if (tts is null || !tts.IsSupported)
            return;

        var mine = ++this.generation;
        this.cts?.Cancel();
        var source = new CancellationTokenSource();
        this.cts = source;

        this.isSpeaking = true;
        this.SetSpeakingAppearance(true);

        try
        {
            var options = new TextToSpeechOptions
            {
                SpeechRate = this.SpeechRate,
                Pitch = this.Pitch,
                Volume = this.Volume,
                Culture = this.Culture is not null ? CultureInfo.GetCultureInfo(this.Culture) : null,
                Voice = await this.ResolveVoiceAsync(tts)
            };

            await tts.SpeakAsync(text, options, source.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PromptTextToSpeechTool: {ex.Message}");
        }
        finally
        {
            // Only the run that is still the current one owns the appearance — a second tap starts
            // another and this finally would otherwise reset the glyph out from under it. Keyed on a
            // generation rather than on `cts` because Stop clears that, which is exactly the case
            // where the reset still has to happen.
            if (this.generation == mine)
            {
                this.cts = null;
                this.isSpeaking = false;
                this.SetSpeakingAppearance(false);
            }
            source.Dispose();
        }
    }

    void SetSpeakingAppearance(bool speaking)
    {
        this.Text = speaking ? this.SpeakingText : IdleGlyph;
        this.ToolColor = speaking ? this.SpeakingColor : null;
    }

    async Task<VoiceInfo?> ResolveVoiceAsync(ITextToSpeechService tts)
    {
        if (String.IsNullOrEmpty(this.VoiceName))
            return null;

        try
        {
            var culture = this.Culture is not null ? CultureInfo.GetCultureInfo(this.Culture) : null;
            var voices = await tts.GetVoicesAsync(culture);
            return voices.FirstOrDefault(v => v.Name.Equals(this.VoiceName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    // IPlatformApplication rather than Application.Current.Handler: the alternate app heads (macOS
    // AppKit, GTK) ship their own handler types, so the Application handler is not a MauiContext
    // carrier there and the older lookup comes back null on exactly the desktops this package now
    // targets. The handler path stays as a fallback.
    static T? ResolveService<T>() where T : class
        => IPlatformApplication.Current?.Services?.GetService<T>()
            ?? Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
}
