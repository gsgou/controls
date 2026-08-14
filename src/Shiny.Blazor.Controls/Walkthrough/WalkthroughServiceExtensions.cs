using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls;

public static class WalkthroughServiceExtensions
{
    /// <summary>
    /// Registers where "has this user seen the tour" is remembered, backing
    /// <c>Walkthrough.RememberRunKey</c>. Defaults to <c>localStorage</c>.
    /// </summary>
    /// <remarks>
    /// Optional: a walkthrough with no <c>RememberRunKey</c> needs no store, and one that cannot find a
    /// store simply runs every time rather than failing — which is the safe direction for onboarding
    /// to break in.
    /// </remarks>
    public static IServiceCollection AddShinyWalkthrough(this IServiceCollection services)
    {
        services.TryAddScoped<IWalkthroughStore, LocalStorageWalkthroughStore>();
        return services;
    }


    /// <summary>
    /// Keeps the flag with the rest of your user state — a server profile, a synced settings store —
    /// instead of in one browser.
    /// </summary>
    public static IServiceCollection AddShinyWalkthrough<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services)
        where T : class, IWalkthroughStore
    {
        services.AddScoped<IWalkthroughStore, T>();
        return services;
    }
}
