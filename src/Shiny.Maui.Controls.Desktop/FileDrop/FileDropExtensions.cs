using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Shiny.Maui.Controls.Desktop.FileDrop;

namespace Shiny;

public static class FileDropMauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IFileDropService"/> — files dragged from Finder / Explorer / Files onto
    /// the app window, including over a <c>BlazorWebView</c> or any other hosted web content.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseFileDrop(o =>
    /// {
    ///     o.AllowedExtensions.Add(".pdf");
    ///     o.MaxFileSize = 50 * 1024 * 1024;
    /// });
    ///
    /// // then, anywhere with DI:
    /// drop.Dropped += (_, e) => this.Import(e.Files);
    /// </code>
    /// </example>
    /// <remarks>
    /// Safe to call unconditionally. On a platform without window-level file drop —
    /// iOS, Android, and any host that reaches this package's <c>net10.0</c> asset without being
    /// Linux — the service still resolves, <see cref="IFileDropService.IsSupported"/> is false and
    /// nothing fires, so shared code needs no <c>#if</c>.
    /// </remarks>
    public static MauiAppBuilder UseFileDrop(this MauiAppBuilder builder, Action<FileDropOptions>? configure = null)
    {
        var options = new FileDropOptions();
        configure?.Invoke(options);

        builder.Services.TryAddSingleton(options);
        builder.Services.TryAddSingleton<IFileDropService>(sp => new FileDropService(
            sp.GetRequiredService<FileDropOptions>(),
            sp.GetService<IFileDropDelegate>(),
            sp.GetService<ILogger<FileDropService>>()
        ));
        builder.Services.AddSingleton<IMauiInitializeService, FileDropInitializer>();
        return builder;
    }

    /// <summary>
    /// The same, plus an app-wide <see cref="IFileDropDelegate"/> that handles drops wherever they
    /// land and whatever page is showing.
    /// </summary>
    /// <remarks>
    /// The delegate is a singleton and runs before <see cref="IFileDropService.Dropped"/>, which it
    /// can suppress by setting <see cref="FileDropContext.Handled"/>.
    /// </remarks>
    public static MauiAppBuilder UseFileDrop<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDelegate>(
        this MauiAppBuilder builder,
        Action<FileDropOptions>? configure = null
    )
        where TDelegate : class, IFileDropDelegate
    {
        builder.Services.TryAddSingleton<IFileDropDelegate, TDelegate>();
        return builder.UseFileDrop(configure);
    }
}
