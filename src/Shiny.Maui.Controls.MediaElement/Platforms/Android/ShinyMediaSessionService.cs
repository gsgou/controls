using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Media3.Session;

namespace Shiny.Maui.Controls.Media;

/// <summary>
/// The foreground service that keeps audio alive once the app is backgrounded, and puts the media
/// notification (with its play/pause/seek controls) in the shade.
/// </summary>
/// <remarks>
/// <para>
/// Android kills background audio without a foreground service, and since API 26 a foreground service
/// must post a notification. Media3 builds that notification for us from the
/// <see cref="MediaSession"/> — this service just has to exist, be declared in the manifest, and hand
/// the session over from <see cref="OnGetSession"/>.
/// </para>
/// <para>
/// The player itself stays in the app process rather than moving into the service. That keeps a single
/// ExoPlayer instance driving both the on-screen surface and the notification, which is what lets
/// background playback be a runtime toggle instead of an architecture the whole control has to adopt
/// up front.
/// </para>
/// </remarks>
[Service(
    Exported = true,
    ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
[IntentFilter(["androidx.media3.session.MediaSessionService"])]
public class ShinyMediaSessionService : MediaSessionService
{
    // Set by the backend before the service is started. Static because Android constructs the service,
    // so there is no other way to hand it the session the app already created.
    internal static MediaSession? ActiveSession { get; set; }

    public override MediaSession? OnGetSession(MediaSession.ControllerInfo? controllerInfo)
        => ActiveSession;

    public override void OnTaskRemoved(Intent? rootIntent)
    {
        // Swiping the app away should not leave an orphaned notification driving a player whose UI is gone.
        var session = ActiveSession;
        if (session?.Player is { PlayWhenReady: false } or null)
            this.StopSelf();

        base.OnTaskRemoved(rootIntent);
    }
}
