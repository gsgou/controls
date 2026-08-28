using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls.FileDrop;

public static class FileDropServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IFileDropService"/> — files dragged from the desktop onto anywhere in
    /// the browser window. Also covered by <c>AddShinyControls()</c>; calling both is safe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scoped, not singleton.</b> The service owns a JS module reference, a
    /// <see cref="DotNetObjectReference{T}"/> and the files from the last drop, all of which belong
    /// to one browser. A singleton would have handed one user's dropped files to every connected
    /// user under Blazor Server, and WebAssembly could never have reproduced it.
    /// </para>
    /// <para>
    /// Place a single <c>&lt;FileDropHost /&gt;</c> in the root layout, or call
    /// <see cref="IFileDropService.StartAsync"/> yourself after the first render.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddShinyFileDrop(
        this IServiceCollection services,
        Action<FileDropOptions>? configure = null
    )
    {
        services.TryAddScoped(_ =>
        {
            var options = new FileDropOptions();
            configure?.Invoke(options);
            return options;
        });

        services.TryAddScoped<IFileDropService>(sp => new FileDropService(
            sp.GetRequiredService<IJSRuntime>(),
            sp.GetRequiredService<FileDropOptions>(),
            sp.GetService<IFileDropDelegate>(),
            sp.GetService<ILogger<FileDropService>>()
        ));
        return services;
    }

    /// <summary>
    /// The same, plus an app-wide <see cref="IFileDropDelegate"/> that handles drops whatever page
    /// is showing.
    /// </summary>
    public static IServiceCollection AddShinyFileDrop<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDelegate>(
        this IServiceCollection services,
        Action<FileDropOptions>? configure = null
    )
        where TDelegate : class, IFileDropDelegate
    {
        services.TryAddScoped<IFileDropDelegate, TDelegate>();
        return services.AddShinyFileDrop(configure);
    }
}
