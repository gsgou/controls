namespace Shiny.Controls.Keyframe;

/// <summary>
/// Maps linear progress onto eased progress. Input is normally in [0,1]; implementations
/// should tolerate values slightly outside that range (springs and overshoot curves return
/// values outside [0,1] by design).
/// </summary>
/// <param name="t">Linear progress, normally 0..1.</param>
/// <returns>Eased progress. May exceed [0,1] for overshooting curves.</returns>
public delegate double EasingFunction(double t);
