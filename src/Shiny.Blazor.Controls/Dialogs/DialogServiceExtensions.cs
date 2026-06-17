using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Blazor.Controls.Dialogs;

public static class DialogServiceExtensions
{
    public static IServiceCollection AddShinyDialogs(this IServiceCollection services, Action<DialogOptions>? configure = null)
    {
        var options = new DialogOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<DialogService>();
        services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<DialogService>());
        return services;
    }
}
