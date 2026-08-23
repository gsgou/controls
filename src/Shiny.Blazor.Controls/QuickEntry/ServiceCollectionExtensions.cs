using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls.QuickEntry;

public static class QuickEntryServiceCollectionExtensions
{
    /// <summary>
    /// Registers the quick entry popup and its screen-edge glow. Also covered by
    /// <c>AddShinyControls()</c> — calling both is safe. Place a single
    /// <c>&lt;QuickEntryHost /&gt;</c> in the root layout either way.
    /// </summary>
    /// <remarks>
    /// <b>Scoped, not singleton.</b> The service owns the popup's open state and the options object
    /// is documented as live, both of which are per-user state — a singleton would show one user's
    /// popup to every connected user under Blazor Server. Under WebAssembly the two lifetimes are
    /// identical. <paramref name="configure"/> therefore runs once per scope against that scope's own
    /// options instance rather than once against a shared one.
    /// </remarks>
    public static IServiceCollection AddShinyQuickEntry(
        this IServiceCollection services,
        Action<QuickEntryOptions>? configure = null
    )
    {
        services.TryAddScoped(_ =>
        {
            var options = new QuickEntryOptions();
            configure?.Invoke(options);
            return options;
        });
        services.TryAddScoped<QuickEntryService>();
        services.TryAddScoped<IQuickEntryService>(sp => sp.GetRequiredService<QuickEntryService>());
        return services;
    }
}
