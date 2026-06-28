using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls.Chat;

namespace Sample.Features.Chat;

public partial class ChatTemplatePage : ContentPage
{
    public ChatTemplatePage()
    {
        InitializeComponent();
    }
}

/// <summary>
/// Selects a bubble template based on the message's <c>kind</c> metadata. The control passes each
/// <see cref="ChatMessage"/> through this selector when rendering custom bubble content.
/// </summary>
public class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? ActionTemplate { get; set; }
    public DataTemplate? CardTemplate { get; set; }

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
    {
        var kind = (item as ChatMessage)?.Metadata?.GetValueOrDefault("kind");
        return kind switch
        {
            "action" => this.ActionTemplate,
            "card" => this.CardTemplate,
            _ => this.TextTemplate
        };
    }
}

[ShellMap<ChatTemplatePage>(registerRoute: false)]
public partial class ChatTemplateViewModel : ObservableObject
{
    public ChatTemplateViewModel()
    {
        this.Provider = new TemplateChatSessionProvider();
    }

    public IChatSessionProvider Provider { get; }
    public string SessionId => TemplateChatSessionProvider.SessionId;

    [RelayCommand]
    async Task Action(ChatMessage msg)
        => await Shell.Current.DisplayAlert("Action", $"You accepted: \"{msg.Body}\"", "OK");

    [RelayCommand]
    async Task Dismiss(ChatMessage msg)
        => await Shell.Current.DisplayAlert("Dismissed", "Dismissed.", "OK");
}


/// <summary>
/// A tiny read-only provider that seeds a conversation containing text, action and card messages so
/// the template selector has something to render. Sends are echoed back as plain text bubbles.
/// </summary>
sealed class TemplateChatSessionProvider : IChatSessionProvider
{
    public const string SessionId = "templates";

    readonly TemplateStore store = new();

    public Task<IChatSession> CreateSessionAsync(string[] userIds, CancellationToken cancellationToken = default)
        => Task.FromResult<IChatSession>(new TemplateChatSession(this.store));

    public Task<IChatSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId != SessionId)
            throw new ChatSessionException($"Chat session '{sessionId}' was not found.");

        return Task.FromResult<IChatSession>(new TemplateChatSession(this.store));
    }
}


sealed class TemplateStore
{
    public const string MeId = "me";
    public const string BotId = "bot";

    readonly object sync = new();
    int counter;

    public object Sync => this.sync;
    public List<ChatMessage> Messages { get; } = new();

    public ChatSessionUserInfo[] Users { get; } =
    [
        new(MeId, "Me", null, null, DateTimeOffset.Now.AddDays(-1)),
        new(BotId, "Assistant", null, Color.FromArgb("#F0F0F0"), DateTimeOffset.Now.AddDays(-1))
    ];

    public TemplateStore() => this.Seed();

    public string NextId() => $"t{Interlocked.Increment(ref this.counter)}";

    void Seed()
    {
        var now = DateTimeOffset.Now;

        void Add(string? body, string? imageUrl, int minutesAgo, params (string, string)[] meta)
        {
            this.Messages.Add(new ChatMessage(
                MessageId: this.NextId(),
                ClientMessageId: null,
                SenderId: BotId,
                Body: body,
                ImageUrl: imageUrl,
                Status: MessageStatus.Read,
                StatusReason: null,
                Timestamp: now.AddMinutes(minutesAgo),
                EditedTimestamp: null,
                Reactions: Array.Empty<Reaction>(),
                ReadReceipts: Array.Empty<ReadReceipt>(),
                Metadata: meta.Length == 0 ? null : meta.ToDictionary(x => x.Item1, x => x.Item2)
            ));
        }

        Add("Hey! Here are some different message types with custom templates.", null, -10);
        Add("Would you like to schedule a meeting for tomorrow at 2pm?", null, -8, ("kind", "action"), ("actionText", "Schedule"));
        Add("A beautiful resort in the mountains with stunning views.", "https://picsum.photos/300/150", -4,
            ("kind", "card"), ("cardTitle", "Mountain Resort Package"));
        Add("Would you like me to book this package?", null, -3, ("kind", "action"), ("actionText", "Book Now"));
    }
}


sealed class TemplateChatSession : IChatSession
{
    readonly TemplateStore store;

    public TemplateChatSession(TemplateStore store) => this.store = store;

    public ChatSessionInfo Info => new(
        TemplateChatSessionProvider.SessionId,
        "Template Demo",
        this.store.Users,
        PermittedEmojis: null,
        BodyPermissions: MessageBodyPermissions.All,
        Permissions: ChatSessionPermissions.All,
        CreatedAt: DateTimeOffset.Now.AddDays(-1),
        LastReadDate: DateTimeOffset.Now,
        UnreadMessageCount: 0
    );

    public string CurrentUserId => TemplateStore.MeId;

    public event EventHandler<ChatMessage>? MessageReceived;
    public event EventHandler<MessageChanged>? MessageUpdated;
    public event EventHandler<string>? MessageDeleted;
    public event EventHandler<UserTypingEvent>? UserTyping;
    public event EventHandler<ChatSessionUserInfo>? UserJoined;
    public event EventHandler<ChatSessionUserInfo>? UserLeft;
    public event EventHandler<ChatSessionInfo>? SessionUpdated;
    public event EventHandler<ChatConnectionState>? ConnectionStateChanged;

    public Task<MessagePage> GetMessagesAsync(string? cursorMessageId, MessagePageDirection direction, int count, CancellationToken cancellationToken = default)
    {
        lock (this.store.Sync)
            return Task.FromResult(new MessagePage(this.store.Messages.ToArray(), false));
    }

    public Task<ChatMessage> SendMessageAsync(OutgoingMessage message, CancellationToken cancellationToken = default)
    {
        message.Attachment?.Content.Dispose();
        ChatMessage stored;
        lock (this.store.Sync)
        {
            stored = new ChatMessage(
                this.store.NextId(),
                string.IsNullOrEmpty(message.ClientMessageId) ? null : message.ClientMessageId,
                this.CurrentUserId,
                message.Body,
                null,
                MessageStatus.Sent,
                null,
                DateTimeOffset.Now,
                null,
                Array.Empty<Reaction>(),
                Array.Empty<ReadReceipt>()
            );
            this.store.Messages.Add(stored);
        }
        return Task.FromResult(stored);
    }

    public Task<ChatMessage> ResendMessageAsync(string clientMessageId, CancellationToken cancellationToken = default)
        => throw new ChatSessionException("Resend is not supported in the template demo.");

    public Task EditMessageAsync(string messageId, string body, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ReactToMessageAsync(string messageId, string emoji, bool add, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task MarkReadAsync(string[] messageIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ToggleTypingAsync(bool isTyping, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task InviteUserAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LeaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RenameAsync(string sessionName, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
