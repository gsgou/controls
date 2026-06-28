using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls;
using Shiny.Maui.Controls.Chat;
using Sample.Features.Chat;

namespace Sample.Features.KitchenSink;

[ShellMap<KitchenSinkPage>(registerRoute: false)]
public partial class KitchenSinkViewModel : ObservableObject
{
    public ObservableCollection<DetentValue> SheetDetents { get; } = [DetentValue.Half, DetentValue.Full];

    public KitchenSinkViewModel()
    {
        this.ChatProvider = new KitchenSinkChatProvider();
    }

    public IChatSessionProvider ChatProvider { get; }

    [ObservableProperty] bool isChatOpen;
    [ObservableProperty] string chatTitle = string.Empty;
    [ObservableProperty] string? activeSessionId;

    [RelayCommand]
    void OpenChat(string participantId)
    {
        var def = KitchenSinkChatProvider.Contacts.FirstOrDefault(c => c.Id == participantId);
        if (def is null)
            return;

        ChatTitle = def.DisplayName;
        ActiveSessionId = def.Id;
        IsChatOpen = true;
    }
}


/// <summary>
/// An in-memory provider seeded with one conversation per demo contact. The Kitchen Sink binds the
/// embedded <c>ChatView</c> to this provider and swaps the bound <c>SessionId</c> as contacts are picked.
/// </summary>
sealed class KitchenSinkChatProvider : IChatSessionProvider
{
    public record Contact(string Id, string DisplayName, string BubbleColorHex, string? Avatar, (string Content, int MinutesAgo)[] Seed);

    public static readonly Contact[] Contacts =
    [
        new("alice", "Alice Johnson", "#E3F2FD", "dotnet_bot.png",
        [
            ("Hey! Just got back from the hike.", -60),
            ("The views were incredible.", -59),
            ("https://picsum.photos/seed/mountain/400/300", -58),
            ("You should come next time!", -55)
        ]),
        new("bob", "Bob Smith", "#FFF3E0", null,
        [
            ("Did you see the game last night?", -120),
            ("What a finish!", -119),
            ("https://picsum.photos/seed/stadium/400/300", -90),
            ("We should watch the next one together.", -85)
        ]),
        new("carol", "Carol Davis", "#F3E5F5", null,
        [
            ("Meeting moved to 3pm.", -30),
            ("Can you bring the design mockups?", -28),
            ("https://picsum.photos/seed/office/400/300", -20),
            ("Thanks!", -18)
        ])
    ];

    readonly Dictionary<string, InMemoryChatStore> stores = new(StringComparer.Ordinal);

    public KitchenSinkChatProvider()
    {
        foreach (var c in Contacts)
        {
            var users = new[]
            {
                new ChatSessionUserInfo(InMemoryChatStore.MeId, "Me", null, null, DateTimeOffset.Now.AddDays(-1)),
                new ChatSessionUserInfo(
                    c.Id,
                    c.DisplayName,
                    c.Avatar is null ? null : ImageSource.FromFile(c.Avatar),
                    Color.FromArgb(c.BubbleColorHex),
                    DateTimeOffset.Now.AddDays(-1))
            };

            var store = InMemoryChatStore.Create(c.Id, c.DisplayName, users);
            foreach (var (content, minutesAgo) in c.Seed)
            {
                var isImage = content.StartsWith("https://", StringComparison.Ordinal);
                store.SeedMessage(c.Id, isImage ? null : content, isImage ? content : null, minutesAgo);
            }
            store.SeedMessage(InMemoryChatStore.MeId, $"Hey {c.DisplayName}!", null, -10);

            this.stores[c.Id] = store;
        }
    }

    public Task<IChatSession> CreateSessionAsync(string[] userIds, CancellationToken cancellationToken = default)
        => throw new ChatSessionException("Creating sessions is not supported in the Kitchen Sink demo.");

    public Task<IChatSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!this.stores.TryGetValue(sessionId, out var store))
            throw new ChatSessionException($"Chat session '{sessionId}' was not found.");

        return Task.FromResult<IChatSession>(new InMemoryChatSession(store));
    }
}


public class KitchenSinkMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? ImageTemplate { get; set; }

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ChatMessage msg && !string.IsNullOrEmpty(msg.ImageUrl))
            return ImageTemplate;

        return TextTemplate;
    }
}
