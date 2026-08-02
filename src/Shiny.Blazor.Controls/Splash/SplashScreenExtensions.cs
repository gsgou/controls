using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Blazor.Controls.Splash;

public static class SplashScreenExtensions
{
    /// <summary>
    /// Registers <see cref="ISplashScreen"/> for driving the pre-boot splash from managed code.
    /// The splash markup and script still have to be referenced from index.html - see the docs.
    /// </summary>
    public static IServiceCollection AddShinySplashScreen(this IServiceCollection services)
    {
        services.AddScoped<ISplashScreen, SplashScreenService>();
        return services;
    }
}
