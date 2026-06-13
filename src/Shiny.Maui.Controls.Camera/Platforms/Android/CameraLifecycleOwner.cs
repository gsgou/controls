using AndroidX.Lifecycle;

namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// A standalone <see cref="ILifecycleOwner"/> the handler drives directly, so CameraX
/// <c>BindToLifecycle</c> tracks the handler's connect/start/stop rather than the host Activity.
/// </summary>
sealed class CameraLifecycleOwner : Java.Lang.Object, ILifecycleOwner
{
    readonly LifecycleRegistry registry;

    public CameraLifecycleOwner()
    {
        this.registry = new LifecycleRegistry(this);
        this.registry.SetCurrentState(Lifecycle.State.Initialized!);
    }

    public Lifecycle Lifecycle => this.registry;

    public void Start() => this.registry.SetCurrentState(Lifecycle.State.Resumed!);

    public void Stop() => this.registry.SetCurrentState(Lifecycle.State.Created!);

    public void Destroy() => this.registry.SetCurrentState(Lifecycle.State.Destroyed!);
}
