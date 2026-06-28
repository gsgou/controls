using System.Globalization;
using Shiny.Maui.Controls.Chat;
using Shiny.Speech;

namespace Shiny.Maui.Controls.SpeechAddins.Chat;

/// <summary>
/// A <see cref="ChatBubbleAction"/> that reads a message's body aloud using
/// <see cref="ITextToSpeechService"/>. Appended to the built-in bubble action set.
/// </summary>
public class TextToSpeechBubbleTool : ChatBubbleAction
{
    CancellationTokenSource? cts;

    public TextToSpeechBubbleTool()
    {
        this.Text = "Read Aloud";
    }

    public static readonly BindableProperty SpeechRateProperty = BindableProperty.Create(
        nameof(SpeechRate), typeof(float), typeof(TextToSpeechBubbleTool), 1.0f);

    public float SpeechRate
    {
        get => (float)GetValue(SpeechRateProperty);
        set => SetValue(SpeechRateProperty, value);
    }

    public static readonly BindableProperty PitchProperty = BindableProperty.Create(
        nameof(Pitch), typeof(float), typeof(TextToSpeechBubbleTool), 1.0f);

    public float Pitch
    {
        get => (float)GetValue(PitchProperty);
        set => SetValue(PitchProperty, value);
    }

    public static readonly BindableProperty VolumeProperty = BindableProperty.Create(
        nameof(Volume), typeof(float), typeof(TextToSpeechBubbleTool), 1.0f);

    public float Volume
    {
        get => (float)GetValue(VolumeProperty);
        set => SetValue(VolumeProperty, value);
    }

    public static readonly BindableProperty VoiceNameProperty = BindableProperty.Create(
        nameof(VoiceName), typeof(string), typeof(TextToSpeechBubbleTool));

    /// <summary>The name of the voice to use. Null uses the system default.</summary>
    public string? VoiceName
    {
        get => (string?)GetValue(VoiceNameProperty);
        set => SetValue(VoiceNameProperty, value);
    }

    public static readonly BindableProperty CultureProperty = BindableProperty.Create(
        nameof(Culture), typeof(string), typeof(TextToSpeechBubbleTool));

    /// <summary>Culture code (e.g. "en-US") for voice selection. Null uses the system default.</summary>
    public string? Culture
    {
        get => (string?)GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    public override async Task InvokeAsync(ChatMessage message)
    {
        if (string.IsNullOrEmpty(message.Body))
            return;

        var tts = ResolveService<ITextToSpeechService>();
        if (tts is null || !tts.IsSupported)
            return;

        this.cts?.Cancel();
        this.cts = new CancellationTokenSource();

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

            await tts.SpeakAsync(message.Body, options, this.cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TextToSpeechBubbleTool: {ex.Message}");
        }
    }

    async Task<VoiceInfo?> ResolveVoiceAsync(ITextToSpeechService tts)
    {
        if (string.IsNullOrEmpty(this.VoiceName))
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

    static T? ResolveService<T>() where T : class
        => Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
}
