using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls.Toast;

public static class ToastServiceExtensions
{
    /// <summary>
    /// Registers the toast service. Also covered by <c>AddShinyControls()</c> — calling both is safe.
    /// </summary>
    /// <remarks>
    /// <b>Scoped, not singleton.</b> <see cref="ToastService"/> owns the live toast list and the
    /// queue, which is per-user state. Under WebAssembly scoped and singleton are the same thing, so
    /// this only shows up on Blazor Server — where a singleton would have shown one user's toast to
    /// every connected user.
    /// </remarks>
    public static IServiceCollection AddShinyToast(this IServiceCollection services)
    {
        services.TryAddScoped<ToastService>();
        services.TryAddScoped<IToastService>(sp => sp.GetRequiredService<ToastService>());
        return services;
    }
}
