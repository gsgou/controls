using CommunityToolkit.Mvvm.ComponentModel;
using Shiny;
using Shiny.Maui.Controls.Chat;
using Shiny.Maui.Controls.SpeechAddins.Chat;

namespace Sample.Features.Chat;

[ShellMap<ChatPage>(registerRoute: false)]
public partial class ChatViewModel : ObservableObject
{
    public ChatViewModel(InMemoryChatSessionProvider provider)
    {
        this.Provider = provider;

        // Example consumer-supplied input-bar actions (surfaced via the overflow button).
        this.InputActions =
        [
            new SpeechToTextTool { AutoSend = false, SilenceTimeout = TimeSpan.FromSeconds(3) },
            new ChatInputAction
            {
                Text = "Insert Greeting",
                Handler = view =>
                {
                    var existing = view.EntryText?.Trim();
                    view.EntryText = string.IsNullOrEmpty(existing) ? "Hello \U0001F44B" : $"{existing} Hello \U0001F44B";
                    return Task.CompletedTask;
                }
            }
        ];

        // Example consumer-supplied bubble actions (appended to the built-in action set).
        this.CustomBubbleActions =
        [
            new TextToSpeechBubbleTool(),
            new ChatBubbleAction
            {
                Text = "Translate",
                Handler = async msg =>
                    await Shell.Current.DisplayAlert("Translate", $"Translating: {msg.Body}", "OK")
            }
        ];
    }

    public IChatSessionProvider Provider { get; }
    public string SessionId => InMemoryChatSessionProvider.DemoSessionId;
    public IList<ChatInputAction> InputActions { get; }
    public IList<ChatBubbleAction> CustomBubbleActions { get; }
}
