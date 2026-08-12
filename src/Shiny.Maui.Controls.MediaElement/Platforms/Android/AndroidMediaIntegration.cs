namespace Shiny.Maui.Controls.Media;

/// <summary>
/// The one thing the Android backend can't discover for itself: whether the activity is in
/// Picture-in-Picture.
/// </summary>
/// <remarks>
/// <para>
/// Android reports PiP transitions through <c>Activity.OnPictureInPictureModeChanged</c>, an override on
/// the <b>app's</b> activity — a library has no way to observe it. Forward it and
/// <c>MediaElement.IsPictureInPictureActive</c> and its event stay accurate; skip it and PiP still works,
/// the control just won't know when the user collapses the window.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// // MainActivity.cs
/// public override void OnPictureInPictureModeChanged(bool isInPictureInPictureMode, Configuration? config)
/// {
///     base.OnPictureInPictureModeChanged(isInPictureInPictureMode, config);
///     AndroidMediaIntegration.NotifyPictureInPictureModeChanged(isInPictureInPictureMode);
/// }
/// </code>
/// </example>
public static class AndroidMediaIntegration
{
    /// <summary>Raised for every active backend when <see cref="NotifyPictureInPictureModeChanged"/> is called.</summary>
    internal static event EventHandler<bool>? PictureInPictureModeChanged;

    /// <summary>Tell the media backends that the activity entered or left Picture-in-Picture.</summary>
    public static void NotifyPictureInPictureModeChanged(bool isInPictureInPictureMode)
        => PictureInPictureModeChanged?.Invoke(null, isInPictureInPictureMode);
}
