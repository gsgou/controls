using Microsoft.Extensions.DependencyInjection;
using Shiny.Blazor.Controls.FileDrop;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// The filtering half of browser file drop. The interop half needs a browser and is exercised by the
/// sample; what can be asserted here is which files an app is shown, and the one rule that is
/// genuinely browser-shaped: a drag in progress reveals no name and no size, so it cannot be judged
/// on either.
/// </summary>
public class FileDropTests
{
    static DroppedFile Dropped(string name, long length = 100, string contentType = "")
        => new("k", name, length, contentType, DateTimeOffset.UnixEpoch, (_, _) => Task.FromResult<Stream>(Stream.Null));

    /// <summary>What the browser gives during a drag: a type at best, never a name or a size.</summary>
    static DroppedFile Hovering(string contentType = "application/pdf")
        => new("", "", -1, contentType, DateTimeOffset.UnixEpoch, (_, _) => Task.FromResult<Stream>(Stream.Null));


    [Theory]
    [InlineData("png")]
    [InlineData(".png")]
    [InlineData(".PNG")]
    public void AnExtensionFilterAcceptsEitherSpelling(string configured)
    {
        var options = new FileDropOptions();
        options.AllowedExtensions.Add(configured);

        options.Accepts(Dropped("photo.png")).ShouldBeTrue();
        options.Accepts(Dropped("notes.txt")).ShouldBeFalse();
    }

    [Fact]
    public void OversizeFilesAreRefused()
    {
        var options = new FileDropOptions { MaxFileSize = 1000 };

        options.Accepts(Dropped("small.txt", 999)).ShouldBeTrue();
        options.Accepts(Dropped("big.txt", 1001)).ShouldBeFalse();
    }

    /// <summary>
    /// The DataTransfer API hides names and sizes until the drop lands. Judging a hover on either
    /// would show "no drop" for every drag; the real check happens when the files arrive.
    /// </summary>
    [Fact]
    public void AHoverIsAcceptedBecauseNothingIsKnownAboutItYet()
    {
        var options = new FileDropOptions { MaxFileSize = 1 };
        options.AllowedExtensions.Add(".png");

        options.Accepts(Hovering()).ShouldBeTrue();
        options.Accepts(Dropped("refused.pdf")).ShouldBeFalse();
    }

    [Fact]
    public void MetadataIsKnownOnlyOnceTheDropHasLanded()
    {
        Hovering().IsMetadataKnown.ShouldBeFalse();
        Dropped("a.txt").IsMetadataKnown.ShouldBeTrue();
    }

    [Fact]
    public void TheExtensionIsLowerCasedSoComparisonsAreStable()
        => Dropped("Photo.PNG").Extension.ShouldBe(".png");


    /// <summary>
    /// The service holds a JS module reference, a DotNetObjectReference and the last drop's files.
    /// A singleton would have handed one user's dropped files to every connected user on Blazor
    /// Server, and no WebAssembly test could reproduce it.
    /// </summary>
    [Theory]
    [InlineData(typeof(IFileDropService))]
    [InlineData(typeof(FileDropOptions))]
    public void PerUserStateIsScoped(Type serviceType)
    {
        var services = new ServiceCollection();
        services.AddShinyFileDrop();

        services.Single(x => x.ServiceType == serviceType)
            .Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void TheUmbrellaAndTheIndividualCallCompose()
    {
        var services = new ServiceCollection();
        services.AddShinyFileDrop();
        services.AddShinyControls();

        services.Count(x => x.ServiceType == typeof(IFileDropService)).ShouldBe(1);
    }

    [Fact]
    public void ADelegateRegistrationIsPickedUp()
    {
        var services = new ServiceCollection();
        services.AddShinyFileDrop<NoOpDelegate>();

        var registration = services.Single(x => x.ServiceType == typeof(IFileDropDelegate));
        registration.ImplementationType.ShouldBe(typeof(NoOpDelegate));
        registration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }


    sealed class NoOpDelegate : IFileDropDelegate
    {
        public Task OnFilesDropped(FileDropContext context) => Task.CompletedTask;
    }
}
