namespace Shiny.Maui.Controls.Camera;

public partial class CameraView
{
    PinchGestureRecognizer? pinchZoomGesture;

    // The zoom the pinch started from. Every platform reports PinchGestureUpdatedEventArgs.Scale as the ratio
    // since the *previous* update (UIPinchGestureRecognizer, ScaleGestureDetector.ScaleFactor,
    // ManipulationDelta.Scale), never as the ratio since the gesture began — so the deltas have to be
    // accumulated, and each one is scaled by where the gesture started to keep a given finger travel worth the
    // same proportional zoom whether you began at 1x or at 6x.
    double pinchStartZoom = 1d;


    static void OnPinchToZoomEnabledChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CameraView)bindable).ApplyPinchToZoom((bool)newValue);


    void ApplyPinchToZoom(bool enabled)
    {
        if (enabled)
        {
            if (this.pinchZoomGesture != null)
                return;

            this.pinchZoomGesture = new PinchGestureRecognizer();
            this.pinchZoomGesture.PinchUpdated += this.OnPinchZoomUpdated;
            this.GestureRecognizers.Add(this.pinchZoomGesture);
        }
        else if (this.pinchZoomGesture != null)
        {
            this.pinchZoomGesture.PinchUpdated -= this.OnPinchZoomUpdated;
            this.GestureRecognizers.Remove(this.pinchZoomGesture);
            this.pinchZoomGesture = null;
        }
    }


    void OnPinchZoomUpdated(object? sender, PinchGestureUpdatedEventArgs args)
    {
        switch (args.Status)
        {
            case GestureStatus.Started:
                // anchor on the live value, not on whatever the last gesture left behind — Zoom may have been
                // driven by a slider or the view model in between
                this.pinchStartZoom = this.Zoom <= 0 ? 1d : this.Zoom;
                break;

            case GestureStatus.Running:
                var scale = args.Scale;
                if (double.IsNaN(scale) || double.IsInfinity(scale))
                    return;

                // Zoom coerces to MinZoom..MaxZoom, so the accumulator can't run away past the ends of the
                // range: pinching back the other way responds on the very next update instead of first having
                // to unwind an out-of-range value.
                this.Zoom += (scale - 1) * this.pinchStartZoom;
                break;
        }
    }
}
