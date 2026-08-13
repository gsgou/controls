using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls.Images;

/// <summary>
/// Registration for <see cref="ShinyImage"/>'s optional download hook.
/// </summary>
/// <remarks>
/// <b>None of this is required.</b> <see cref="ShinyImage"/> works with nothing registered - it
/// streams remote images through <c>fetch</c> for a real progress percentage and falls back to a
/// plain <c>&lt;img&gt;</c> when a cross-origin server blocks that. Register a downloader only when
/// the bytes have to come through C#, which in practice means authenticated images.
/// </remarks>
public static class ImageServiceExtensions
{
    /// <summary>
    /// Routes <see cref="ShinyImage"/> downloads through the registered <see cref="HttpClient"/>,
    /// so anything configured on that client - a base address, an auth handler - applies to images.
    /// </summary>
    public static IServiceCollection AddShinyImages(this IServiceCollection services)
    {
        services.TryAddScoped<IImageDownloader>(sp => new HttpImageDownloader(sp.GetRequiredService<HttpClient>()));
        return services;
    }


    /// <summary>
    /// Routes <see cref="ShinyImage"/> downloads through your own <see cref="IImageDownloader"/>.
    /// </summary>
    public static IServiceCollection AddShinyImages<T>(this IServiceCollection services)
        where T : class, IImageDownloader
    {
        services.AddScoped<IImageDownloader, T>();
        return services;
    }
}
