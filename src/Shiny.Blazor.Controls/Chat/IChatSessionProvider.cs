namespace Shiny.Blazor.Controls.Chat;

/// <summary>
/// The integration point a consumer implements to drive a <c>ChatView</c>. The control binds to a
/// provider (plus a session id) and renders; all data, lifecycle, permissions and real-time
/// behavior live behind the provider and the <see cref="IChatSession"/> it returns.
/// </summary>
public interface IChatSessionProvider
{
    Task<IChatSession> CreateSessionAsync(string[] userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an existing session. Throws <see cref="ChatSessionException"/> if the session is
    /// missing or the current user lacks access.
    /// </summary>
    Task<IChatSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
