using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platforms.Linux.Gtk4.Platform;
#if DEBUG
using Microsoft.Maui.DevFlow.Agent.Gtk;
#endif

namespace Sample.Linux;

public class Program : GtkMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

#if DEBUG
    // GTK needs the agent started once the MAUI Application object exists (OnStarted fires
    // after activation); the builder registration alone doesn't spin up the HTTP listener.
    protected override void OnStarted()
    {
        base.OnStarted();
        (this.Application as Application)?.StartDevFlowAgent();
    }
#endif

    public static void Main(string[] args) => new Program().Run(args);
}
