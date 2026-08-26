using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls.Images;

namespace Sample.Features.Images;

[ShellMap<ImagesPage>(registerRoute: false)]
public partial class ImagesViewModel(IImageService imageService) : ObservableObject
{
    // picsum.photos serves real photos at an exact size and sends a Content-Length, which is what
    // makes the determinate ring worth demonstrating at all. Seeding each URL keeps it stable
    // between runs, so a second visit is a genuine cache hit rather than a different picture.
    public string LargeImageUri => "https://picsum.photos/seed/shiny-large/1600/1200";

    public string BrokenImageUri => "https://shinylib.net/this-image-does-not-exist-404.png";

    // Wikimedia serves the SVG logo itself as image/svg+xml over a stable URL, which makes it a fair
    // demonstration of the remote path: a real download, a real cache entry, and a vector at the end.
    public string RemoteSvgUri => "https://upload.wikimedia.org/wikipedia/commons/4/4f/SVG_Logo.svg";

    // Percent-encoded rather than base64 so the markup stays readable in the source that carries it.
    public string InlineSvgUri { get; } =
        "data:image/svg+xml," + Uri.EscapeDataString(
            """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
              <circle cx="32" cy="32" r="30" fill="#10B981" />
              <path d="M18 33 L28 43 L47 22" fill="none" stroke="#ffffff" stroke-width="6"
                    stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            """
        );

    public ObservableCollection<string> Photos { get; } = new(
        Enumerable.Range(1, 30).Select(i => $"https://picsum.photos/seed/shiny-grid-{i}/400/400")
    );

    [ObservableProperty] string cacheSize = "tap refresh";
    [ObservableProperty] string lastLoad = "-";
    [ObservableProperty] string reloadKey = "https://picsum.photos/seed/shiny-large/1600/1200";


    [RelayCommand]
    async Task RefreshCacheSize()
    {
        var bytes = await imageService.GetCacheSizeAsync();
        CacheSize = bytes < 1024 * 1024
            ? $"{bytes / 1024.0:0.0} KB"
            : $"{bytes / (1024.0 * 1024):0.00} MB";
    }


    [RelayCommand]
    async Task ClearCache()
    {
        await imageService.ClearCacheAsync();
        await RefreshCacheSize();

        // Nothing re-requests an image just because the cache went away, so the page nudges the
        // control itself - which is exactly what an app with a "free up space" button has to do.
        ReloadKey = LargeImageUri + "?cleared=" + DateTime.UtcNow.Ticks;
    }


    public void OnImageLoaded(ImageLoadedEventArgs args)
        => LastLoad = $"{args.Origin} · {(args.ContentLength is { } len ? $"{len / 1024.0:0} KB" : "size unknown")}";
}
