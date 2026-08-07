using AVFoundation;
using Foundation;

namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// Owns the app's shared <see cref="AVAudioSession"/> for the capture session, in place of AVFoundation's own
/// automatic configuration.
/// </summary>
/// <remarks>
/// <para>
/// <b>An <c>AVCaptureSession</c> takes the app's audio session over by default, and it takes it exclusively.</b>
/// With <c>AVCaptureSession.AutomaticallyConfiguresApplicationAudioSession</c> left at its default of true,
/// AVFoundation reconfigures and activates the shared <see cref="AVAudioSession"/> whenever the capture session
/// runs — and the configuration it picks never carries
/// <see cref="AVAudioSessionCategoryOptions.MixWithOthers"/>. Two things follow, and both are silent:
/// whatever else was playing (music over CarPlay or Bluetooth, a podcast, a navigation app) is interrupted the
/// moment recording starts, and the capture session in turn is interrupted — with
/// <c>AVCaptureSessionInterruptionReasonAudioDeviceInUseByAnotherClient</c>, which stops <i>video</i> capture as
/// well — the moment anything else claims audio. For a continuous recorder (a dash cam being the obvious case)
/// that means pressing play in another app ends the recording.
/// </para>
/// <para>
/// The only supported way out is to turn the automatic configuration off and configure the session here, which
/// is what this type does: <see cref="AVAudioSessionCategory.PlayAndRecord"/> with
/// <see cref="AVAudioSessionCategoryOptions.MixWithOthers"/>, so the microphone is captured without evicting
/// anyone. It is applied only when a recording actually asks for audio — with
/// <see cref="VideoRecordingOptions.IncludeAudio"/> off, nothing here runs and (with the automatic
/// configuration disabled) nothing touches the audio session at all, which is why a video-only recording no
/// longer stops the music either.
/// </para>
/// <para>
/// <b><see cref="AVAudioSessionCategoryOptions.AllowBluetooth"/> is deliberately not set.</b> That option opts
/// into the hands-free profile (HFP), which is an <i>input</i> route — asking for it while a car or a headset is
/// playing music over A2DP switches the whole link into call mode, dropping the music being mixed alongside us
/// to mono telephony quality. <see cref="AVAudioSessionCategoryOptions.AllowBluetoothA2DP"/> is output-only and
/// carries no such cost.
/// </para>
/// </remarks>
sealed class AppleCaptureAudioSession
{
    NSObject? interruptionToken;
    bool activated;

    /// <summary>Reports a configuration failure. Invoked on whatever thread the call came in on.</summary>
    public Action<string>? OnError { get; set; }

    /// <summary>
    /// Puts the shared audio session into a record-capable category and activates it.
    /// </summary>
    /// <param name="mixWithOthers">
    /// Leave other apps' audio playing (<see cref="CameraView.MixWithOtherAudio"/>).
    /// </param>
    public void Activate(bool mixWithOthers)
    {
        var session = AVAudioSession.SharedInstance();

        var options = AVAudioSessionCategoryOptions.AllowBluetoothA2DP | AVAudioSessionCategoryOptions.DefaultToSpeaker;
        if (mixWithOthers)
            options |= AVAudioSessionCategoryOptions.MixWithOthers;

        session.SetCategory(AVAudioSessionCategory.PlayAndRecord, options, out var categoryError);
        if (categoryError != null)
        {
            this.OnError?.Invoke("Could not configure the audio session for recording: " + categoryError.LocalizedDescription);
            return;
        }

        // Best effort: the mode only tunes microphone selection and processing (VideoRecording picks the mic
        // beside the camera in use and applies wind-noise reduction), so a device or route that refuses it
        // still records perfectly well through whatever mode is already set.
        session.SetMode(AVAudioSessionMode.VideoRecording.GetConstant()!, out _);

        session.SetActive(true, out var activeError);
        if (activeError != null)
        {
            this.OnError?.Invoke("Could not activate the audio session for recording: " + activeError.LocalizedDescription);
            return;
        }

        this.activated = true;
        this.Observe();
    }


    /// <summary>
    /// Hands the audio session back, if this instance is what activated it.
    /// </summary>
    /// <remarks>
    /// <see cref="AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation"/> is what lets an app that was
    /// interrupted (or ducked) resume by itself. Nothing is deactivated that this instance did not activate:
    /// the shared session belongs to the whole process, and another component's recording must not be ended by
    /// the camera being torn down.
    /// </remarks>
    public void Deactivate()
    {
        this.interruptionToken?.Dispose();
        this.interruptionToken = null;

        if (!this.activated)
            return;

        this.activated = false;
        AVAudioSession.SharedInstance().SetActive(false, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out _);
    }


    /// <summary>
    /// Reactivates the session after an interruption that says it may be resumed.
    /// </summary>
    /// <remarks>
    /// Mixing keeps other apps' playback from interrupting us, but nothing keeps out the interruptions that
    /// pre-empt every app — an incoming call, Siri. Those leave the session deactivated, and iOS never
    /// reactivates it for us: without this, audio capture stays dead for the rest of the recording while video
    /// carries on, so the file ends up silent from the call onwards with nothing having reported a failure.
    /// The capture session's own restart is handled separately, off
    /// <c>AVCaptureSessionInterruptionEnded</c>.
    /// </remarks>
    void Observe()
        // The sender parameter is named rather than discarded: `_` here would shadow the out-discard below.
        => this.interruptionToken ??= AVAudioSession.Notifications.ObserveInterruption((sender, e) =>
        {
            var resumable = e.InterruptionType == AVAudioSessionInterruptionType.Ended
                && (e.Option & AVAudioSessionInterruptionOptions.ShouldResume) != 0;

            if (resumable)
                AVAudioSession.SharedInstance().SetActive(true, out _);
        });
}
