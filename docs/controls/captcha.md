# Captcha (Blazor only)

[← All Shiny Controls](../../README.md)

A human check in front of a form. One `<Captcha />` over **four providers**, chosen by name at
registration and swapped without touching the markup: the built-in **local** challenge — no account,
no site key, no third-party script, works offline and inside a `BlazorWebView` — plus Google
reCAPTCHA, hCaptcha and Cloudflare Turnstile.

There is no MAUI equivalent. The hosted providers are browser widgets and the local challenge draws
to an HTML canvas; a MAUI app hosting Blazor can use the local one as-is.

<!-- TODO: capture screenshots for captcha -->

Registration is **optional** — with nothing registered, `<Captcha />` renders the local challenge:

```csharp
builder.Services.AddShinyControls(cfg => cfg
    .ConfigureCaptcha(c => c
        .UseTurnstile("0x4AAA...")                                    // public site key only
        .UseLocal(o => o.Mode = LocalCaptchaMode.Math, name: "math")  // a second, named challenge
        .SetDefaultProvider("turnstile")
    )
);
```

```razor
@using Shiny.Blazor.Controls.Captchas

<Captcha @ref="captcha" Validate="VerifyAsync" ValidChanged="v => canSubmit = v" />

<button disabled="@(!canSubmit)" @onclick="SubmitAsync">Sign up</button>
```

**A token is not a verdict.** The component hands you a response token in `State.Response`; it is
your *server* that posts that token, with your **secret** key, to the provider's siteverify endpoint.
Trusting `IsSolved` alone is trusting the client, which is the thing a captcha exists to avoid —
`Validate` is the hook for that round trip. It runs the moment the widget solves, and returning false
(or throwing) keeps the state invalid and, by default, throws the challenge away, because a token
your server rejected is spent either way. The secret key never belongs in the client project.

Gate the submit button on **`ValidChanged`**, not on a value read once: it flips back when a solved
challenge expires. After a failed submit call `ResetAsync()` — a token is single-use at the provider.

`Size="CaptchaSize.Invisible"` scores the session in the background and renders no challenge, so
nothing solves until `ExecuteAsync()` is called from your submit handler; `Flexible` is Turnstile-only
and falls back to `Normal` elsewhere. `Theme`, `Size` and `LanguageCode` are per component or app-wide
defaults, and `BadgePosition` places an invisible provider's badge.

The local challenge takes `LocalCaptchaOptions` — `Mode` (`Text` draws distorted characters, `Math`
asks a small sum), `Length`, `CharacterSet` (look-alikes `0 O 1 I L` already removed), `CaseSensitive`,
`Width`/`Height`, `ExpirySeconds`, `MaxAttempts`, and all of the wording for localisation. **Math mode
renders real text**, so a screen reader can read it — drawn characters cannot be. And be plain about
what it is: the challenge is generated *and checked* in the browser, so it is a bot speed bump rather
than a security boundary. For a public form worth attacking, register a hosted provider and verify the
token on the server.

A `Provider` naming something that is not registered renders a visible "no provider" alert rather than
silently dropping to a weaker check. For a provider this package does not ship, subclass
`RemoteCaptchaProvider` with a `RemoteCaptchaDescriptor` — one shared JS driver handles script
loading, widget lifetime, callbacks, reset and execute — or implement `ICaptchaProvider` for anything
that is not a script-and-global widget.
