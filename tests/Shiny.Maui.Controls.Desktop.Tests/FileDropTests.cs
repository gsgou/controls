using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Desktop.FileDrop;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Desktop.Tests;

/// <summary>
/// Everything about file drop that is not native. The four <c>FileDropPlatform</c> types only report
/// raw drags; which of those files an app actually sees, and which event it hears about them on, is
/// decided here — so this is where it can be tested without a window manager.
/// </summary>
public class FileDropTests
{
    static DroppedFile File(string name, long length = 100)
        => new(name, "/tmp/" + name, length, _ => Task.FromResult<Stream>(Stream.Null));

    static (FileDropService Service, List<FileDropEventArgs> Dropped, List<FileDragEventArgs> Left) Build(
        Action<FileDropOptions>? configure = null,
        IFileDropDelegate? handler = null
    )
    {
        var options = new FileDropOptions();
        configure?.Invoke(options);

        var service = new FileDropService(options, handler);
        var dropped = new List<FileDropEventArgs>();
        var left = new List<FileDragEventArgs>();

        service.Dropped += (_, e) => dropped.Add(e);
        service.DragLeave += (_, e) => left.Add(e);
        return (service, dropped, left);
    }

    static void Drop(FileDropService service, params DroppedFile[] files)
        => ((IFileDropHost)service).NotifyDrop(null!, files, Point.Zero);


    [Theory]
    [InlineData("pdf")]
    [InlineData(".pdf")]
    [InlineData(".PDF")]
    public void AnExtensionFilterAcceptsEitherSpelling(string configured)
    {
        var (service, dropped, _) = Build(o => o.AllowedExtensions.Add(configured));
        Drop(service, File("report.pdf"));

        dropped.Single().Files.Single().FileName.ShouldBe("report.pdf");
    }

    [Fact]
    public void FilesOutsideTheFilterAreCountedRatherThanSilentlyLost()
    {
        var (service, dropped, _) = Build(o => o.AllowedExtensions.Add(".png"));
        Drop(service, File("a.png"), File("b.txt"), File("c.txt"));

        var args = dropped.Single();
        args.Files.Count.ShouldBe(1);
        args.RejectedCount.ShouldBe(2);
    }

    [Fact]
    public void OversizeFilesAreRefused()
    {
        var (service, dropped, _) = Build(o => o.MaxFileSize = 1000);
        Drop(service, File("small.txt", 999), File("big.txt", 1001));

        dropped.Single().Files.Single().FileName.ShouldBe("small.txt");
    }

    [Fact]
    public void MaxFilesTakesThemInOrderRatherThanRefusingTheWholeDrop()
    {
        var (service, dropped, _) = Build(o => o.MaxFiles = 2);
        Drop(service, File("a.txt"), File("b.txt"), File("c.txt"));

        var args = dropped.Single();
        args.Files.Select(x => x.FileName).ShouldBe(["a.txt", "b.txt"]);
        args.RejectedCount.ShouldBe(1);
    }

    /// <summary>
    /// No platform sends a "leave" after a drop, so an overlay bound to the drag state would stay up
    /// for good if a wholly-refused drop reported nothing at all.
    /// </summary>
    [Fact]
    public void AWhollyRefusedDropStillEndsTheDrag()
    {
        var (service, dropped, left) = Build(o => o.AllowedExtensions.Add(".png"));
        Drop(service, File("a.txt"), File("b.txt"));

        dropped.ShouldBeEmpty();
        left.Single().RejectedCount.ShouldBe(2);
        left.Single().HasAcceptableFiles.ShouldBeFalse();
    }

    [Fact]
    public void DisablingStopsEverythingWithoutDetaching()
    {
        var (service, dropped, left) = Build();
        service.IsEnabled = false;
        Drop(service, File("a.txt"));

        dropped.ShouldBeEmpty();
        left.ShouldBeEmpty();
    }

    [Fact]
    public void DirectoriesAreRefusedUnlessAskedFor()
    {
        var folder = new DroppedFile("stuff", "/tmp/stuff", -1, _ => Task.FromResult<Stream>(Stream.Null)) { IsDirectory = true };

        var (refusing, refused, _) = Build();
        Drop(refusing, folder);
        refused.ShouldBeEmpty();

        var (accepting, accepted, _) = Build(o => o.AllowDirectories = true);
        Drop(accepting, folder);
        accepted.Single().Files.Single().IsDirectory.ShouldBeTrue();
    }


    /// <summary>
    /// A drag hovering over the window is asked about before the app hears anything, so the platform
    /// can show the copy cursor or the "no drop" one.
    /// </summary>
    [Fact]
    public void TheDragCursorFollowsTheFilter()
    {
        var (service, _, _) = Build(o => o.AllowedExtensions.Add(".png"));
        var host = (IFileDropHost)service;

        host.WouldAccept([File("a.png")]).ShouldBeTrue();
        host.WouldAccept([File("a.txt")]).ShouldBeFalse();

        // Mac Catalyst cannot name the payload until the drop lands. Refusing an unknown one would
        // show "no drop" for every drag there, so it is accepted now and filtered later.
        host.WouldAccept([]).ShouldBeTrue();
    }

    [Fact]
    public void ADisabledServiceRefusesTheDragOutright()
    {
        var (service, _, _) = Build();
        service.IsEnabled = false;

        ((IFileDropHost)service).WouldAccept([File("a.txt")]).ShouldBeFalse();
    }


    [Fact]
    public void TheDelegateRunsBeforeTheEvent()
    {
        var order = new List<string>();
        var handler = new RecordingDelegate(_ => order.Add("delegate"));

        var (service, _, _) = Build(handler: handler);
        service.Dropped += (_, _) => order.Add("event");

        Drop(service, File("a.txt"));
        order.ShouldBe(["delegate", "event"]);
    }

    [Fact]
    public void ADelegateCanConsumeTheDrop()
    {
        var handler = new RecordingDelegate(ctx => ctx.Handled = true);
        var (service, dropped, _) = Build(handler: handler);

        Drop(service, File("a.txt"));
        dropped.ShouldBeEmpty();
    }

    /// <summary>
    /// The delegate runs on whatever thread the native drop arrived on, and native code is not going
    /// to catch anything it throws — so a faulting delegate must not take the drop handler with it.
    /// </summary>
    [Fact]
    public void AThrowingDelegateDoesNotTakeTheDropDown()
    {
        var handler = new RecordingDelegate(_ => throw new InvalidOperationException("boom"));
        var (service, _, _) = Build(handler: handler);

        Should.NotThrow(() => Drop(service, File("a.txt")));
    }


    [Fact]
    public void ContentTypeComesFromTheExtension()
    {
        File("a.PNG").ContentType.ShouldBe("image/png");
        File("a.docx").ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        // Unknown types report the generic one rather than null, so callers never null-check it.
        File("a.wibble").ContentType.ShouldBe("application/octet-stream");
        File("noextension").ContentType.ShouldBe("application/octet-stream");
    }

    [Fact]
    public void FromPathReadsWhatIsOnDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        System.IO.File.WriteAllText(path, "hello");

        try
        {
            var file = DroppedFile.FromPath(path);
            file.FileName.ShouldBe(Path.GetFileName(path));
            file.FullPath.ShouldBe(path);
            file.Length.ShouldBe(5);
            file.IsDirectory.ShouldBeFalse();
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    /// <summary>
    /// A path the sandbox will not open is still worth surfacing — the app may hold an entitlement
    /// this process does not — so a missing file reports a length of -1 rather than throwing.
    /// </summary>
    [Fact]
    public void FromPathDoesNotThrowOnAFileItCannotStat()
    {
        var file = DroppedFile.FromPath("/definitely/not/here.txt");
        file.FileName.ShouldBe("here.txt");
        file.Length.ShouldBe(-1);
    }

    [Fact]
    public async Task ReadAllBytesReadsTheWholeFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");
        await System.IO.File.WriteAllBytesAsync(path, [1, 2, 3, 4]);

        try
        {
            (await DroppedFile.FromPath(path).ReadAllBytesAsync()).ShouldBe([1, 2, 3, 4]);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }


    sealed class RecordingDelegate(Action<FileDropContext> onDrop) : IFileDropDelegate
    {
        public Task OnFilesDropped(FileDropContext context)
        {
            onDrop(context);
            return Task.CompletedTask;
        }
    }
}
