# ChatView Provider Integration — Implementation Plan

Status: **Proposed** · Owner: Allan · Last updated: 2026-06-27

## Goal

Move ChatView to a **provider-driven integration interface** (the same model as the
Scheduler `ISchedulerEventProvider`), so the control surface becomes *styles + layout
only*. All data, lifecycle, permissions, and real-time behavior live behind
`IChatSessionProvider` / `IChatSession`. Consumers implement the provider; the control
binds to it and renders.

Alongside this we will:

1. Replace the ad-hoc bubble/entry **tool** model with a **permission-driven** action set
   (`ChatSessionPermissions`). Tools that are pure client concerns (copy, TTS) stay; tools
   that overlap provider actions (react/edit/delete/attach) collapse into permission-gated
   built-in affordances.
2. Add a **markdown composition toolbar** to the input bar, gated by
   `MessageBodyPermissions`.
3. Support **image selection from gallery or camera**, where the platform supports it.

---

## 1. Why this exists (carryover from API review)

The first-draft `IChatViewProvider` had structural problems we are correcting here:

- **Session scoping was inconsistent** — some methods took `sessionId`, some didn't, and
  events carried no session at all.
- **Three permissions had no action method** — `CanEditMessages`, `CanDeleteMessages`,
  `CanChangeSessionName`.
- **No attachment send path** despite shipping camera/gallery tools today.
- **No way to identify the current user**, so the control couldn't align bubbles or
  suppress self read-receipts.
- **Read receipts were a single bool** — can't model group chats.
- **Index/count paging** is unstable under live inserts (Scheduler deliberately uses a
  stable range; chat needs a stable cursor).
- **No optimistic-send correlation, no send/connection status.**

---

## 2. Revised public API

> Two namespaces, mirrored: `Shiny.Maui.Controls.Chat` and `Shiny.Blazor.Controls.Chat`.
> MAUI uses `ImageSource`/`Color`; Blazor uses `string` URLs / CSS colors. Records below
> show the MAUI shape; Blazor swaps the platform types as the Scheduler models already do.

### 2.1 Provider + session handle

The provider is a thin factory/lookup. A **session-scoped handle** owns everything that
needs a session — including the live events — so nothing threads `sessionId` and the
control can subscribe on attach / dispose on detach (no leaked handlers).

```csharp
public interface IChatSessionProvider
{
    Task<IChatSession> CreateSessionAsync(string[] userIds, CancellationToken ct = default);

    // throws ChatSessionException if the session is missing or the current user has no access
    Task<IChatSession> GetSessionAsync(string sessionId, CancellationToken ct = default);
}

public interface IChatSession : IAsyncDisposable
{
    ChatSessionInfo Info { get; }              // ALWAYS current — refreshed before SessionUpdated fires
    string CurrentUserId { get; }              // who "me" is — drives alignment + ownership checks

    // ---- paging (cursor-based, stable under live inserts; pages older OR newer) ----
    Task<MessagePage> GetMessagesAsync(
        string? cursorMessageId,               // null + Older = newest page (initial load)
        MessagePageDirection direction,
        int count,
        CancellationToken ct = default
    );

    // ---- outgoing ----
    Task<ChatMessage> SendMessageAsync(OutgoingMessage message, CancellationToken ct = default);
    Task<ChatMessage> ResendMessageAsync(string clientMessageId, CancellationToken ct = default);
    Task EditMessageAsync(string messageId, string body, CancellationToken ct = default);
    Task DeleteMessageAsync(string messageId, CancellationToken ct = default);

    // add == true toggles the emoji on; add == false removes it. Users may hold multiple distinct reactions.
    Task ReactToMessageAsync(string messageId, string emoji, bool add, CancellationToken ct = default);

    // batch; control passes only visible, not-mine, unread ids
    Task MarkReadAsync(string[] messageIds, CancellationToken ct = default);

    // ---- session management (gated by Info.Permissions) ----
    Task ToggleTypingAsync(bool isTyping, CancellationToken ct = default);
    Task InviteUserAsync(string userId, CancellationToken ct = default);
    Task LeaveAsync(CancellationToken ct = default);
    Task RenameAsync(string sessionName, CancellationToken ct = default);

    // ---- live (may fire off the UI thread; control marshals) ----
    event EventHandler<ChatMessage> MessageReceived;        // includes echoes of own sends (multi-device)
    event EventHandler<MessageChanged> MessageUpdated;      // carries WHAT changed
    event EventHandler<string> MessageDeleted;              // messageId
    event EventHandler<UserTypingEvent> UserTyping;
    event EventHandler<ChatSessionUserInfo> UserJoined;
    event EventHandler<ChatSessionUserInfo> UserLeft;
    event EventHandler<ChatSessionInfo> SessionUpdated;     // name / users / permitted emojis / permissions
    event EventHandler<ChatConnectionState> ConnectionStateChanged;
}
```

### 2.2 Paging result

```csharp
public record MessagePage(
    IReadOnlyList<ChatMessage> Messages,   // always chronological asc, regardless of direction
    bool HasMore                           // more available in the requested direction
);

public enum MessagePageDirection
{
    Older,   // load history above the cursor (scroll up); the default LoadMore path
    Newer    // load messages below the cursor — needed for jump-to-first-unread then scroll down
}
```

### 2.3 Outgoing message (send payload)

A small object instead of a bare string, so attachments (and a future `ReplyToMessageId`)
don't break the signature.

```csharp
public record OutgoingMessage(
    string? Body,                          // markdown; null when attachment-only
    OutgoingAttachment? Attachment = null,
    string ClientMessageId = ""            // control-generated; correlates optimistic bubble w/ echo
    // string? ReplyToMessageId            // FUTURE — reserved
);

public record OutgoingAttachment(
    ChatAttachmentKind Kind,               // Image (others reserved)
    Stream Content,                        // PROVIDER OWNS + DISPOSES after upload; control supplies it
    string FileName,
    string ContentType
);

public enum ChatAttachmentKind { Image /*, Video, Audio, File — reserved */ }
```

### 2.4 Message + change models

```csharp
public record ChatMessage(
    string MessageId,
    string? ClientMessageId,               // matches OutgoingMessage.ClientMessageId for echo reconciliation
    string SenderId,
    string? Body,                          // markdown
    string? ImageUrl,
    MessageStatus Status,                  // Sending/Sent/Delivered/Read/Failed/Rejected
    string? StatusReason,                  // human-readable reason when Failed/Rejected (shown on the bubble)
    DateTimeOffset Timestamp,
    DateTimeOffset? EditedTimestamp,
    IReadOnlyList<Reaction> Reactions,
    IReadOnlyList<ReadReceipt> ReadReceipts,   // per-user; control collapses to bool for 1:1
    string? Identifier = null,                  // template-selector discriminator (was ChatMessage.Identifier)
    IReadOnlyDictionary<string, string>? Metadata = null  // custom-payload bag for templates
);

public enum MessageStatus
{
    Sending,
    Sent,
    Delivered,
    Read,
    Failed,    // transient (network/server) — control offers retry via ResendMessageAsync
    Rejected   // provider refused the message (too big, too many images, not permitted) — NOT retryable
}

public record Reaction(string UserId, string Emoji, DateTimeOffset Timestamp);
public record ReadReceipt(string UserId, DateTimeOffset Timestamp);

// what changed — kills the fragile "EditedTimestamp == null means reaction" heuristic
public record MessageChanged(ChatMessage Message, MessageChangeKind Change);
public enum MessageChangeKind { Edited, ReactionChanged, ReadReceiptChanged, StatusChanged }
```

### 2.5 Session + user info

```csharp
public record ChatSessionInfo(
    string SessionId,
    string SessionName,
    ChatSessionUserInfo[] Users,
    string[] PermittedEmojis,
    MessageBodyPermissions BodyPermissions,   // drives the markdown toolbar
    ChatSessionPermissions Permissions,       // drives every action affordance
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastReadDate,             // folded in from CurrentUserChatSessionInfo
    int UnreadMessageCount
);

public record ChatSessionUserInfo(
    string UserId,
    string DisplayName,
    ImageSource? Avatar,                       // Blazor: string? AvatarUrl
    Color? BubbleColor,                        // Blazor: string? — preserves per-participant tint
    DateTimeOffset JoinedDate
);

public record UserTypingEvent(string UserId, bool IsTyping, DateTimeOffset Timestamp);

public enum ChatConnectionState { Connected, Reconnecting, Offline }
```

### 2.7 Exceptions — provider rejects, control reacts

Validation that depends on server/business rules (size caps, attachment counts, content
policy, access) lives in the **provider**, never the control. The control attempts the
action optimistically and renders whatever the provider throws — it does **not** pre-check
limits.

```csharp
// GetSessionAsync throws this when the session is missing or the current user lacks access.
public class ChatSessionException : Exception
{
    public ChatSessionException(string message) : base(message) { }
}

// SendMessageAsync / ResendMessageAsync / EditMessageAsync throw this when the provider
// REFUSES the content (message too large, too many/large images, unsupported, not permitted).
// The control flips the optimistic bubble to MessageStatus.Rejected, sets StatusReason to
// Message, and shows it inline (no retry affordance — the user must change the content).
public class ChatSendRejectedException : Exception
{
    public ChatSendRejectedException(string reason, SendRejectionKind kind) : base(reason)
        => this.Kind = kind;

    public SendRejectionKind Kind { get; }
}

public enum SendRejectionKind
{
    MessageTooLarge,
    TooManyAttachments,
    AttachmentTooLarge,
    UnsupportedContent,
    NotPermitted,
    Other
}
```

**Why this shape:** keeping limits in the provider means the control never hard-codes "max
N images" or byte caps it can't know. A *transient* failure (network/server hiccup) →
`MessageStatus.Failed` + retry. A *rejection* (the message will never be accepted as-is) →
`ChatSendRejectedException` → `MessageStatus.Rejected` + reason, no retry. The control's only
job is to surface `StatusReason`.

### 2.6 Permissions (largely unchanged)

```csharp
[Flags]
public enum ChatSessionPermissions
{
    None = 0,
    CanSendMessages     = 1,
    CanEditMessages     = 2,   // own messages only
    CanDeleteMessages   = 4,   // own messages only
    CanReactToMessages  = 8,
    CanInviteUsers      = 16,
    CanLeaveSession     = 32,
    CanChangeSessionName= 64,
    CanSendImages       = 128, // gates the gallery/camera attach affordance (independent of text send)

    Default = CanSendMessages | CanSendImages | CanReactToMessages | CanInviteUsers | CanLeaveSession,
    All = CanSendMessages | CanEditMessages | CanDeleteMessages | CanReactToMessages
        | CanInviteUsers | CanLeaveSession | CanChangeSessionName | CanSendImages
}

[Flags]
public enum MessageBodyPermissions
{
    None = 0,
    Links = 1, Bold = 2, Italics = 4, Underline = 8,
    Strikethrough = 16, Codeblocks = 32,
    All = Links | Bold | Italics | Underline | Strikethrough | Codeblocks
}
```

### Permission → action mapping (every flag now has a home)

| Permission | Surfaced as |
|---|---|
| `CanSendMessages` | input bar enabled; `SendMessageAsync` |
| `CanEditMessages` | "Edit" action on **own** bubbles; `EditMessageAsync` |
| `CanDeleteMessages` | "Delete" action on **own** bubbles; `DeleteMessageAsync` |
| `CanReactToMessages` | reaction picker (filtered to `PermittedEmojis`); `ReactToMessageAsync` |
| `CanInviteUsers` | invite affordance; `InviteUserAsync` |
| `CanLeaveSession` | leave affordance; `LeaveAsync` |
| `CanChangeSessionName`| rename affordance; `RenameAsync` |
| `CanSendImages` | gallery/camera attach affordance (see §5) |

---

## 3. Tool model → permission-driven actions

**Today:** `ChatEntryTool` / `ChatBubbleTool` FAB hierarchies with built-ins
(`PhotoGalleryEntryTool`, `TakePhotoEntryTool`, `SpeechToTextTool`, `CopyBubbleTool`,
`AcknowledgementBubbleTool`, `AcknowledgementSelectorBubbleTool`, `TextToSpeechBubbleTool`).

**Going forward:** the control renders a **built-in action set derived from
`ChatSessionPermissions` + ownership (`CurrentUserId`)**, not from a consumer-supplied tool
list.

- **Collapse into permission actions (remove as public tools):**
  `AcknowledgementBubbleTool`, `AcknowledgementSelectorBubbleTool` → reaction picker driven
  by `CanReactToMessages` + `PermittedEmojis`. `PhotoGalleryEntryTool`, `TakePhotoEntryTool`
  → built-in attachment picker (see §5) driven by `CanSendMessages`.
- **Keep as client-only tools (no provider interaction):** `CopyBubbleTool`,
  `TextToSpeechBubbleTool`, `SpeechToTextTool`. These never touched the provider and remain
  opt-in extras (TTS/STT stay in `Shiny.Maui.Controls.SpeechAddins`).
- **Extensibility:** retain a *single* lightweight hook for genuinely custom bubble actions
  (e.g. `CustomBubbleActions` taking the message), so consumers can still add app-specific
  verbs without the old multi-class FAB tree.

`BubbleToolItems`, `MyBubbleToolItems`, `ToolItems`, and the `Acknowledgement*BubbleTool` /
`*EntryTool` types are removed/relocated. No migration notes — ChatView is still v1 beta,
so we change the surface freely.

---

## 4. Markdown composition toolbar (input bar)

The input bar gains a formatting toolbar gated by `ChatSessionInfo.BodyPermissions`.

- **Toolbar buttons appear only for granted flags:** Bold, Italics, Underline,
  Strikethrough, Code block, Link. (`MessageBodyPermissions.None` → no toolbar, plain text.)
- **Behavior:** each button wraps the current selection with the markdown delimiter
  (`**`, `*`/`_`, `~~`, `` ` ``/```` ``` ````), or inserts a placeholder when there's no
  selection. Link prompts for URL + display text → `[text](url)`.
- **Outgoing `Body` is markdown**; rendering already exists (MAUI link detection / Blazor
  linkify) — extend the bubble renderer to honor the same subset for display.
- **No add-on dependency (resolved, §10):** core Chat does **not** reference the
  `*.Controls.Markdown` package. Implement a self-contained minimal toolbar + inline subset
  renderer in core; only extract a shared primitive *into* core if overlap proves it out.
- **Reference only:** the repo already ships a `MarkdownEditor` (with toolbar) and `MarkdownView`
  in `Shiny.*.Controls.Markdown` — crib their toolbar UX and inline-render approach, but
  reimplement the needed subset in core rather than taking the dependency.
- **MAUI:** new internal `ChatMarkdownToolbar` above `ChatInputBar`.
  **Blazor:** toolbar row in `ChatView.razor` operating on the textarea selection via JS interop.

---

## 5. Image selection — gallery or camera

Built-in attachment affordance (replaces the photo/camera entry tools), shown when
**`CanSendImages`** is set (independent of `CanSendMessages`, so a session can be text-only
or image-capable as the provider decides).

**Image + caption (decision):** for v1 a message is **either text or an image, not both** —
matches today's behavior (`ImageUrl` present ⇒ no text bubble) and keeps the renderer and the
tap→ImageViewer path simple. `MessageBodyPermissions` stays strictly about *text formatting*;
it does not gain an image flag (image sending is the `CanSendImages` action above). Captions
can be added later without breaking the model (`Body` is already nullable alongside `ImageUrl`).

- **Picker UX:** an action sheet offering **Gallery** and **Camera** — *Camera shown only
  when the platform/device supports capture.*
- **Capability probe:** use `MediaPicker.Default.IsCaptureSupported` (MAUI Essentials) to
  decide whether to show Camera. On platforms without capture (most desktop, web), show
  Gallery only.
  - MAUI: `MediaPicker.PickPhotoAsync()` / `CapturePhotoAsync()` → `Stream` → `OutgoingAttachment`.
  - Blazor: `<InputFile accept="image/*">`; `capture` attribute hint for mobile browsers;
    no native camera on desktop → Gallery/file only.
- **Flow:** picker → `OutgoingAttachment(Image, stream, name, contentType)` →
  `SendMessageAsync` → optimistic bubble (`MessageStatus.Sending`, local preview) →
  provider uploads → echo reconciles via `ClientMessageId`, `ImageUrl` populated.
- **Reuse note:** `Shiny.Maui.Controls.Camera` exists; for the v1 attachment picker the
  Essentials `MediaPicker` is sufficient and keeps the core control dependency-free. A
  richer in-app CameraView capture path can be a later opt-in.

### 5.1 Tapping an image opens ImageViewer

When a user taps a bubble image (`ChatMessage.ImageUrl`), the control opens the existing
**ImageViewer** component (pinch/pan/double-tap zoom) — this is a built-in, client-side
behavior, **not** a provider call.

- **MAUI:** image-bubble tap → present `ImageViewer` (modal/overlay) loading `ImageUrl`.
  Reuse the existing component from `Shiny.Maui.Controls`; no new dependency.
- **Blazor:** image tap → open the Blazor `ImageViewer` with the same URL.
- This replaces the generic "image tappable → `MessageTappedCommand`" path for image
  bubbles. Non-image bubble taps remain a notification only (no provider round-trip).
- **Optional override:** expose a control flag (e.g. `OpenImagesInViewer`, default `true`)
  so a consumer using a custom `MessageTemplate` can opt out and handle the tap itself.
- For **optimistic** image sends (`MessageStatus.Sending`, local preview before upload),
  the viewer opens on the local preview; once the echo populates the remote `ImageUrl`,
  subsequent taps use it.

---

## 6. Control changes (styles + layout only)

### MAUI (`src/Shiny.Maui.Controls/Chat`)
- **New:** `Provider` (`IChatSessionProvider`) and `SessionId` bindable properties — control
  resolves/creates the `IChatSession`, subscribes to events, disposes on `Unloaded`.
- **Remove data-owning bindables** now sourced from the session: `Messages`,
  `Participants`, `TypingParticipants`, plus the manual `LoadMoreCommand`/`SendCommand`/
  `AttachImageCommand`/`MessageTappedCommand` wiring (control calls the session directly).
- **Keep all styling bindables:** bubble colors/corner radius/fonts, input bar colors,
  `MessageTemplate`/`MessageTemplateSelector`, `UseFeedback`, `AdjustForKeyboard`, scroll
  behavior.
- **Keep + adapt:** `ScrollToEnd`, `ScrollToMessage`, pending/optimistic rendering (now keyed
  off `MessageStatus` instead of `DateSent == null`), new-message + typing pills.
- **CancellationToken / loader pattern:** mirror Scheduler — per-load `CancellationTokenSource`,
  catch `TaskCanceledException`, loader overlay during initial fetch.
- **Threading:** marshal all `IChatSession` events to the UI thread.
- **Connection banner:** render `ConnectionStateChanged` (offline/reconnecting) and disable
  the input bar while not `Connected`. **No offline queue** — sends are only attempted while
  `Connected`. A send that still fails transiently → `Failed` + retry; a provider rejection →
  `Rejected` + reason (see §2.7).

### Blazor (`src/Shiny.Blazor.Controls/Chat`)
- Mirror the above with `IChatSessionProvider`/`IChatSession`; `IAsyncDisposable` component
  disposes the session; events marshalled via `InvokeAsync(StateHasChanged)`.

---

## 7. Control-side responsibilities (documented contract)

- **Dedup / merge:** `MessageId` is the canonical key. The same message may arrive via both
  `MessageReceived` and a later `GetMessagesAsync` page (boundary race) — the control merges
  on `MessageId` and never renders duplicates. Echoes of own sends reconcile against the
  optimistic bubble by `ClientMessageId`, then by `MessageId` thereafter.
- **Read receipts:** send only ids that are visible, not mine, and currently unread
  (`MarkReadAsync`); debounce on scroll. Ignore inbound `ReadReceiptChanged` for
  `CurrentUserId` so receipts don't loop back into `MarkReadAsync`.
- **Typing:** debounce `ToggleTypingAsync(true)`, auto-send `false` on send / inactivity
  timeout; run an expiry timer on inbound `UserTyping` to clear stale indicators.
- **Optimistic send:** generate `ClientMessageId`, render `Sending` bubble immediately,
  reconcile/replace on echo. On `ChatSendRejectedException` → `Rejected` + `StatusReason`
  (no retry); on any other failure → `Failed` + retry via `ResendMessageAsync`. The control
  does **not** pre-validate size/count — it attempts and surfaces the provider's verdict.
- **Reactions:** filter the picker to `Info.PermittedEmojis`. If `PermittedEmojis` is
  **null** and `CanReactToMessages` is set, fall back to the built-in default emoji set; an
  **empty** array means no reactions. Provider re-validates regardless.
- **Ownership:** compare `SenderId` to `CurrentUserId` for alignment, edit/delete
  eligibility, and self-receipt suppression.
- **Errors:** `GetSessionAsync`/initial load throwing `ChatSessionException` → render an
  error state (extends the Scheduler loader pattern, which only had a spinner).

---

## 8. Work breakdown

1. **Core models + interfaces** (MAUI + Blazor): §2 records/enums, `IChatSessionProvider`,
   `IChatSession`. Keep AOT/trim-clean (no anonymous types over Blazor JS interop).
2. **MAUI control rewire**: provider resolution, session lifecycle/dispose, event
   marshaling, cursor paging + loader, optimistic send, status rendering, connection banner.
3. **Blazor control rewire**: same, component `IAsyncDisposable`.
4. **Permission-driven actions**: bubble action set from permissions+ownership; remove old
   tool classes; keep Copy/TTS/STT as client extras + one custom-action hook.
5. **Markdown toolbar**: gated by `BodyPermissions`; share Markdown package toolbar/renderer
   (MAUI + Blazor); extend bubble rendering for the markdown subset.
6. **Image attachment**: gallery/camera action sheet with `IsCaptureSupported` probe (MAUI),
   `InputFile` (Blazor); wire `OutgoingAttachment` → `SendMessageAsync`.
7. **Sample app**: implement a demo `InMemoryChatSessionProvider` (simulated typing,
   receipts, reactions, edits, reconnect) under `samples/Sample/Features/Chat/`; update the
   chat pages + `MauiProgram.cs` registration.
8. **Tests** (`tests/`): cursor paging stability under live insert, optimistic
   reconcile/failure, permission gating, read-receipt filtering, typing debounce/expiry.

## 9. Docs & sync (per CLAUDE.md, do with the feature)

- **README.md** — rewrite the ChatView section for the provider model + new packages/behavior.
- **Local skill** `SKILLS/shiny-controls/chatview.md` — regenerate so generated code uses
  `IChatSessionProvider`/`IChatSession`, the markdown toolbar, and permission-driven actions.
- **Docs repo** (`~/Desktop/dev/documentation`): release-notes entry; menu nodes under the
  `Controls` topic for the provider model / markdown toolbar / attachments. No migration
  guide — ChatView is v1 beta.
- **Screenshots:** leave a `TODO: capture screenshots for chatview (provider, toolbar,
  attachment picker)` — do **not** capture as part of this work.

## 10. Resolved defaults

- **Markdown: no dependency on the `*.Controls.Markdown` add-on package.** Core Chat must
  not depend on an add-on (it would invert the package dependency direction). Implement a
  **minimal inline markdown subset** (bold/italic/underline/strikethrough/code/links) — a
  small toolbar + lightweight inline renderer — self-contained in the core control. The full
  `MarkdownView`/`MarkdownEditor` is heavier than chat bubbles need; only the subset above is
  in scope. If the inline renderer turns out to overlap meaningfully, extract a shared
  primitive *into core* and have the Markdown package consume it — never the reverse.
- **Keep the single `CustomBubbleActions` hook.** Real apps always need app-specific verbs
  (forward, report, pin); template-only would force consumers to rebuild the entire bubble.
  One lightweight hook (custom actions receiving the `ChatMessage`) stays alongside
  `MessageTemplate`.
- **v1 attachments = Essentials `MediaPicker` only.** Gallery + camera (camera gated by
  `MediaPicker.IsCaptureSupported`). In-app `CameraView` capture is deferred (§11).
- **Page-size is a control property.** Expose `PageSize` (default **30**), mirroring the
  Scheduler's `DaysPerPage` convention, so consumers can tune fetch granularity per layout.

## 11. Deferred / reserved

- `ReplyToMessageId` (record + `OutgoingMessage` slots reserved).
- Non-image attachments (`ChatAttachmentKind` reserved: Video/Audio/File).
- In-app `CameraView`-based capture path.
- Message search / jump-to-message.
