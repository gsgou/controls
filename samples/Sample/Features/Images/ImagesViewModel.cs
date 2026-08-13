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
