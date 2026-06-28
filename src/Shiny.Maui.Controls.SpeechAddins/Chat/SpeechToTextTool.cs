using System.Globalization;
using Shiny.Maui.Controls.Chat;
using Shiny.Speech;

namespace Shiny.Maui.Controls.SpeechAddins.Chat;

/// <summary>
/// A <see cref="ChatInputAction"/> that listens for speech via <see cref="ISpeechToTextService"/>
/// and backfills the chat entry. Optionally auto-sends when recognition completes. Surfaced from the
/// input bar's overflow actions.
/// </summary>
public class SpeechToTextTool : ChatInputAction
{
    public SpeechToTextTool()
    {
        this.Text = "Voice Input";
    }

    public static readonly BindableProperty AutoSendProperty = BindableProperty.Create(
        nameof(AutoSend), typeof(bool), typeof(SpeechToTextTool), false);

    /// <summary>When true, automatically submits the entry text after recognition completes.</summary>
    public bool AutoSend
    {
        get => (bool)GetValue(AutoSendProperty);
        set => SetValue(AutoSendProperty, value);
    }

    public static readonly BindableProperty SilenceTimeoutProperty = BindableProperty.Create(
        nameof(SilenceTimeout), typeof(TimeSpan), typeof(SpeechToTextTool), TimeSpan.FromSeconds(2));

    /// <summary>Duration of silence before recognition is considered complete.</summary>
    public TimeSpan SilenceTimeout
    {
        get => (TimeSpan)GetValue(SilenceTimeoutProperty);
        set => SetValue(SilenceTimeoutProperty, value);
    }

    public static readonly BindableProperty CultureProperty = BindableProperty.Create(
        nameof(Culture), typeof(string), typeof(SpeechToTextTool));

    /// <summary>Culture code (e.g. "en-US") for recognition. Null uses the system default.</summary>
    public string? Culture
    {
        get => (string?)GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    public static readonly BindableProperty PreferOnDeviceProperty = BindableProperty.Create(
        nameof(PreferOnDevice), typeof(bool), typeof(SpeechToTextTool), false);

    /// <summary>When true, prefers on-device recognition over cloud-based.</summary>
    public bool PreferOnDevice
    {
        get => (bool)GetValue(PreferOnDeviceProperty);
        set => SetValue(PreferOnDeviceProperty, value);
    }

    public static readonly BindableProperty ListeningTextProperty = BindableProperty.Create(
        nameof(ListeningText), typeof(string), typeof(SpeechToTextTool), "Listening…");

    /// <summary>Text used to represent the listening state (reserved for richer renderers).</summary>
    public string ListeningText
    {
        get => (string)GetValue(ListeningTextProperty);
        set => SetValue(ListeningTextProperty, value);
    }

    public override async Task InvokeAsync(ChatView chatView)
    {
        var stt = ResolveService<ISpeechToTextService>();
        if (stt is null || !stt.IsSupported)
            return;

        var access = await stt.RequestAccess();
        if (access != AccessState.Available)
            return;

        using var cts = new CancellationTokenSource();
        try
        {
            var options = new SpeechRecognitionOptions
            {
                SilenceTimeout = this.SilenceTimeout,
                PreferOnDevice = this.PreferOnDevice,
                Culture = this.Culture is not null ? CultureInfo.GetCultureInfo(this.Culture) : null
            };

            var result = await stt.ListenUntilSilence(options, cts.Token);
            if (!string.IsNullOrEmpty(result))
            {
                var existing = chatView.EntryText?.Trim();
                chatView.EntryText = string.IsNullOrEmpty(existing) ? result : $"{existing} {result}";

                if (this.AutoSend)
                    chatView.SubmitEntry();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SpeechToTextTool: {ex.Message}");
        }
    }

    static T? ResolveService<T>() where T : class
        => Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
}
