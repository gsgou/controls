using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls.Splash;

public static class SplashScreenExtensions
{
    /// <summary>
    /// Registers <see cref="ISplashScreen"/> for driving the pre-boot splash from managed code.
    /// The splash markup and script still have to be referenced from index.html - see the docs.
    /// Also covered by <c>AddShinyControls()</c> — calling both is safe.
    /// </summary>
    public static IServiceCollection AddShinySplashScreen(this IServiceCollection services)
    {
        services.TryAddScoped<ISplashScreen, SplashScreenService>();
        return services;
    }
}
