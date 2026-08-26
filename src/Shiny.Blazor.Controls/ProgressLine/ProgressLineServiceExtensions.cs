using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls;

public static class ProgressLineServiceExtensions
{
    /// <summary>
    /// Registers the progress line service. Also covered by <c>AddShinyControls()</c> — calling both
    /// is safe.
    /// </summary>
    /// <remarks>
    /// <b>Scoped, not singleton.</b> <see cref="ProgressLineService"/> owns the active run list, which
    /// is per-user state. Under WebAssembly scoped and singleton are the same thing, so this only
    /// shows up on Blazor Server — where a singleton would have run one user's loading line across
    /// every connected user's window.
    /// </remarks>
    public static IServiceCollection AddShinyProgressLine(this IServiceCollection services)
    {
        services.TryAddScoped<ProgressLineService>();
        services.TryAddScoped<IProgressLineService>(sp => sp.GetRequiredService<ProgressLineService>());
        return services;
    }
}
