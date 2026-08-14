using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls.OnScreenKeyboard;

public static class OnScreenKeyboardServiceCollectionExtensions
{
    /// <summary>
    /// Registers the on-screen keyboard. Also covered by <c>AddShinyControls()</c> — calling both is
    /// safe. Place a single <c>&lt;OnScreenKeyboardHost /&gt;</c> in the root layout either way.
    /// </summary>
    /// <remarks>
    /// <b>Scoped, not singleton.</b> The service owns <c>IsVisible</c> and the options object is
    /// documented as live — an app is expected to mutate it at runtime. Both are per-user state, so a
    /// singleton would have raised one user's keyboard on every connected user's screen under Blazor
    /// Server. Under WebAssembly the two lifetimes are identical. Note that <paramref name="configure"/>
    /// therefore runs once per scope against that scope's own options instance, rather than once
    /// against a shared one.
    /// </remarks>
    public static IServiceCollection AddShinyOnScreenKeyboard(
        this IServiceCollection services,
        Action<OnScreenKeyboardOptions>? configure = null
    )
    {
        services.TryAddScoped(_ =>
        {
            var options = new OnScreenKeyboardOptions();
            configure?.Invoke(options);
            return options;
        });
        services.TryAddScoped<OnScreenKeyboardService>();
        services.TryAddScoped<IOnScreenKeyboardService>(sp => sp.GetRequiredService<OnScreenKeyboardService>());
        return services;
    }
}
