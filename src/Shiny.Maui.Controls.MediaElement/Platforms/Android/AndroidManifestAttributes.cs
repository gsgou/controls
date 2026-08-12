using Android.App;

// Contributed to the consuming app's merged manifest so background playback works without every app
// having to copy these in by hand. FOREGROUND_SERVICE_MEDIA_PLAYBACK became mandatory in API 34 for any
// service declaring the mediaPlayback type — omitting it is a hard crash on launch, not a degraded
// feature.
[assembly: UsesPermission(Android.Manifest.Permission.ForegroundService)]
[assembly: UsesPermission("android.permission.FOREGROUND_SERVICE_MEDIA_PLAYBACK")]
[assembly: UsesPermission(Android.Manifest.Permission.WakeLock)]

// Streaming needs the network; a purely local-file app is unaffected by the extra declaration.
[assembly: UsesPermission(Android.Manifest.Permission.Internet)]

// The media notification. POST_NOTIFICATIONS is a *runtime* grant on API 33+, so the app still has to
// ask for it — declaring it here only makes the request possible.
[assembly: UsesPermission("android.permission.POST_NOTIFICATIONS")]
