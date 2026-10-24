using Microsoft.Extensions.DependencyInjection;
using Shiny.Blazor.Controls.Images;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// <c>AddShinyImages()</c> routes downloads through the app's <see cref="HttpClient"/>. Blazor Server and
/// the Web App template register none, and the first image used to fail with a bare "No service for type
/// HttpClient" that pointed nowhere near the registration that needed it.
/// </summary>
public class ImageRegistrationTests
{
    [Fact]
    public void WithoutAnHttpClientTheErrorNamesTheFix()
    {
        using var provider = new ServiceCollection().AddShinyImages().BuildServiceProvider();

        var ex = Should.Throw<InvalidOperationException>(() => provider.CreateScope().ServiceProvider.GetService<IImageDownloader>());
        ex.Message.ShouldContain("HttpClient");
        ex.Message.ShouldContain("AddShinyImages<T>()");
    }


    [Fact]
    public void WithAnHttpClientItResolves()
    {
        using var provider = new ServiceCollection()
            .AddScoped(_ => new HttpClient())
            .AddShinyImages()
            .BuildServiceProvider();

        provider.CreateScope().ServiceProvider.GetService<IImageDownloader>().ShouldNotBeNull();
    }
}
