using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Maui.Controls.SpeechAddins;

/// <summary>
/// Finds the Shiny.Speech engine a tool needs in the app's container.
/// </summary>
/// <remarks>
/// Every tool here no-ops when its engine is missing - a speech button should not throw in an app that
/// simply did not register speech. But a no-op with no trace reads as a broken button, so the first
/// miss for each engine says which registration is missing, through the app's logger at Warning (or
/// Trace, which a Release build keeps, when there is no logger).
/// </remarks>
static class SpeechServices
{
    static readonly HashSet<Type> warned = new();


    public static T? Resolve<T>() where T : class
    {
        // IPlatformApplication first: the alternate app heads (macOS AppKit, GTK) ship their own handler
        // types, so Application.Current.Handler carries no MauiContext there. Three of the four tools
        // used only that path, so on macOS they found no engine even with one registered.
        var services = IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
        var service = services?.GetService<T>();
        if (service is not null)
            return service;

        lock (warned)
        {
            if (!warned.Add(typeof(T)))
                return null;
        }

        var message =
            $"No {typeof(T).Name} is registered, so this speech tool does nothing. Register Shiny.Speech with " +
            "builder.Services.AddSpeechServices() (or AddSpeechToText() / AddTextToSpeech()).";
        var logger = services?.GetService<ILoggerFactory>()?.CreateLogger("Shiny.SpeechAddins");
        if (logger is not null)
            logger.LogWarning(message);
        else
            System.Diagnostics.Trace.TraceWarning("[Shiny.SpeechAddins] " + message);

        return null;
    }
}
