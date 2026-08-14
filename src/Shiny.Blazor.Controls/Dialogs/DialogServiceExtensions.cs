using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls.Dialogs;

public static class DialogServiceExtensions
{
    /// <summary>
    /// Registers the dialog service. Also covered by <c>AddShinyControls()</c> — calling both is safe.
    /// </summary>
    /// <remarks>
    /// <b>Scoped, not singleton.</b> <see cref="DialogService"/> owns the active-dialog list and the
    /// queue, which is per-user state; a singleton would have put one user's dialog on every
    /// connected user's screen under Blazor Server. Under WebAssembly the two lifetimes are
    /// identical. <see cref="DialogOptions"/> is scoped for the same reason — the options object is
    /// reachable from DI, so it has to be safe to mutate at runtime.
    /// </remarks>
    public static IServiceCollection AddShinyDialogs(this IServiceCollection services, Action<DialogOptions>? configure = null)
    {
        services.TryAddScoped(_ =>
        {
            var options = new DialogOptions();
            configure?.Invoke(options);
            return options;
        });
        services.TryAddScoped<DialogService>();
        services.TryAddScoped<IDialogService>(sp => sp.GetRequiredService<DialogService>());
        return services;
    }
}
