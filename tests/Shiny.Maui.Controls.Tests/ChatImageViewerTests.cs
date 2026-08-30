using System.Reflection;
using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Chat;
using Shiny.Maui.Controls.Infrastructure;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// <see cref="ChatView"/> hosts an <see cref="ImageViewer"/> to show a tapped photo, and an
/// ImageViewer is two things: a thumbnail and a lightbox. The chat wants only the second, and issue
/// #11 is what happens when it gets both - the photo painted over the whole conversation, with no
/// way to dismiss it, alongside the lightbox that works.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class ChatImageViewerTests
{
    public ChatImageViewerTests()
    {
        TestDispatcherProvider.Install();

        // Application.Current is process-wide; a fresh one per test keeps implicit styles from
        // leaking across the collection.
        _ = new Application();
    }


    static ImageViewer ViewerOf(ChatView chat) => (ImageViewer)typeof(ChatView)
        .GetField("imageViewer", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(chat)!;


    static ChatMessage ImageMessage() => new(
        "1", null, "them", null, "https://example.com/photo.png",
        MessageStatus.Sent, null, DateTimeOffset.Now, null, [], []);


    [Fact]
    public void TheHostedViewerNeverPaintsItsOwnThumbnail()
    {
        var chat = new ChatView();
        var viewer = ViewerOf(chat);

        viewer.IsVisible.ShouldBeFalse("it is in the tree only to find the page, not to draw");

        chat.OnImageTapped(ImageMessage(), ImageSource.FromUri(new Uri("https://example.com/photo.png")));

        // Source still reaches the thumbnail - that is ImageViewer's own wiring - but nothing of it
        // is on screen, which is the whole of issue #11.
        viewer.IsVisible.ShouldBeFalse();
    }


    [Fact]
    public void APlainContentPageGetsAnOverlayLayer()
    {
        // The shape of the repo's own samples/Sample/Features/Chat/ChatPage.xaml, and the one that
        // used to have nowhere to put the lightbox at all.
        var chat = new ChatView();
        var page = new ContentPage { Content = chat };

        var host = typeof(ImageViewer)
            .GetMethod("FindOverlayParent", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(ViewerOf(chat), null);

        host.ShouldBeOfType<PageOverlay.ImageViewerLayer>();
        page.Content.ShouldBeOfType<PageOverlay.ShinyOverlayRoot>("the page's content was wrapped");
    }


    [Fact]
    public void TheOverlayCoversThePageRatherThanCellZeroOfItsRootGrid()
    {
        // A root Grid with more than one cell was the case that put a full-screen overlay into the
        // top-left cell - the "left" half of the window in the issue's screenshot.
        var chat = new ChatView();
        var root = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }
        };
        root.Add(new Label(), 0, 0);
        root.Add(chat, 1, 1);
        _ = new ContentPage { Content = root };

        var host = typeof(ImageViewer)
            .GetMethod("FindOverlayParent", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(ViewerOf(chat), null);

        host.ShouldBeOfType<PageOverlay.ImageViewerLayer>();
        host.ShouldNotBe(root);
    }


    [Fact]
    public void AnExplicitOverlayHostStillWins()
    {
        var viewer = new ImageViewer();
        var host = new FloatingPanel.OverlayHost();
        host.Children.Add(viewer);
        _ = new ContentPage { Content = host };

        typeof(ImageViewer)
            .GetMethod("FindOverlayParent", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(viewer, null)
            .ShouldBe(host);
    }
}
