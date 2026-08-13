using Microsoft.Extensions.DependencyInjection;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Images;

/// <summary>
/// An image that always shows something: placeholder artwork, a determinate or indeterminate
/// loading ring, the image itself, or error artwork.
/// </summary>
/// <remarks>
/// Remote URIs go through <see cref="IImageService"/>, which caches to memory and disk, caps how
/// many downloads run at once, and collapses concurrent requests for the same URI into one. Replace
/// <see cref="IImageDownloader"/> to control how the bytes are fetched (auth headers, your own
/// <c>HttpClient</c>) without giving up any of that.
/// </remarks>
/// <example>
/// <code>
/// &lt;shiny:ShinyImage Uri="{Binding AvatarUrl}"
///                   PlaceholderImage="avatar_placeholder.png"
///                   ErrorImage="broken_image.png"
///                   Aspect="AspectFill"
///                   HeightRequest="120" /&gt;
/// </code>
/// </example>
public partial class ShinyImage : ContentView, IDisposable
{
    readonly Grid root;
    readonly Image targetImage;
    readonly Image placeholderImage;
    readonly Image errorImage;
    readonly ContentView loadingHost;
    readonly ContentView errorHost;
    readonly LoadingRing ring;

    CancellationTokenSource? cts;
    IImageService? resolvedService;
    bool isDisposed;

    /// <summary>Creates the control.</summary>
    public ShinyImage()
    {
        this.targetImage = new Image
        {
            Aspect = Aspect.AspectFit,
            Opacity = 0,
            IsVisible = false
        };

        this.placeholderImage = new Image
        {
            Aspect = Aspect.AspectFit,
            IsVisible = false,
            InputTransparent = true
        };

        this.errorImage = new Image
        {
            Aspect = Aspect.AspectFit,
            InputTransparent = true
        };

        this.ring = new LoadingRing();

        this.loadingHost = new ContentView
        {
            Content = this.ring,
            IsVisible = false,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        this.errorHost = new ContentView
        {
            Content = this.errorImage,
            IsVisible = false,
            InputTransparent = true
        };

        this.root = new Grid();
        this.root.Children.Add(this.placeholderImage);
        this.root.Children.Add(this.targetImage);
        this.root.Children.Add(this.errorHost);
        this.root.Children.Add(this.loadingHost);
        this.Content = this.root;

        // A recycled cell raises Unloaded then Loaded again. Cancelling on the way out stops work
        // for a row nobody is looking at; restarting on the way back in is what keeps a scrolled-away
        // and scrolled-back cell from showing a permanently empty frame. Cache hits make the retry
        // nearly free.
        this.Loaded += this.OnControlLoaded;
        this.Unloaded += this.OnControlUnloaded;

        StyleGuard.MarkReady(this, typeof(ShinyImage));
    }


    /// <summary>
    /// The service this control loads through. Assign to override the resolved one for this instance
    /// - handy in tests and for a screen that needs its own cache policy.
    /// </summary>
    public IImageService ImageService
    {
        get => this.resolvedService ??= ResolveService(this);
        set => this.resolvedService = value;
    }


    /// <summary>
    /// Re-fetches the image, skipping both cache tiers. The cache is refreshed with what comes back.
    /// </summary>
    public Task ReloadAsync() => this.LoadAsync(true);


    void OnControlLoaded(object? sender, EventArgs e)
    {
        if (this.State is ImageLoadState.None or ImageLoadState.Failed || this.targetImage.Source is null)
            this.BeginLoad();
    }


    void OnControlUnloaded(object? sender, EventArgs e) => this.CancelPending();


    void BeginLoad() => _ = this.LoadAsync(false);


    async Task LoadAsync(bool bypassCache)
    {
        this.CancelPending();

        if (this.isDisposed)
            return;

        var source = this.Source;
        if (source is not null)
        {
            // An explicit ImageSource is already resolved as far as we are concerned - there is
            // nothing to download, queue or cache.
            this.SetProgress(new ImageLoadProgress(ImageLoadState.Loaded));
            await this.ShowImageAsync(source).ConfigureAwait(true);
            this.RaiseLoaded(null, ImageOrigin.Memory, null);
            return;
        }

        var uri = this.Uri;
        if (String.IsNullOrWhiteSpace(uri))
        {
            this.targetImage.Source = null;
            this.targetImage.Opacity = 0;
            this.SetValue(LoadErrorPropertyKey, null);
            this.SetProgress(ImageLoadProgress.None);
            return;
        }

        // Local files and bundled resources never touch the service. Sending them through the
        // download path would mean a queue slot and a cache copy for bytes already on the device.
        if (!IsRemote(uri))
        {
            this.SetProgress(new ImageLoadProgress(ImageLoadState.Loaded));
            await this.ShowImageAsync(ImageSource.FromFile(uri)).ConfigureAwait(true);
            this.RaiseLoaded(uri, ImageOrigin.Disk, null);
            return;
        }

        var tokenSource = new CancellationTokenSource();
        this.cts = tokenSource;
        var token = tokenSource.Token;

        this.SetProgress(ImageLoadProgress.Queued);

        // Progress<T> captures the sync context at construction. Building it here - on the UI thread,
        // where every load starts - is what makes the ring update without a manual dispatch.
        var progress = new Progress<ImageLoadProgress>(this.SetProgress);

        var request = new ImageRequest(uri)
        {
            BypassCache = bypassCache,
            CacheEnabled = this.CacheEnabled,
            CacheDuration = this.CacheDuration
        };

        ImageResult result;
        try
        {
            result = await this.ImageService.GetAsync(request, progress, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            result = ImageResult.Fail(ex);
        }

        if (token.IsCancellationRequested || this.isDisposed)
            return;

        // The Uri changed while this load was in flight (a recycled cell, or a fast rebind). Showing
        // these bytes now would put the previous row's picture in the new row.
        if (!String.Equals(this.Uri, uri, StringComparison.Ordinal))
            return;

        if (!result.Success)
        {
            this.ShowError(uri, result.Error ?? new InvalidOperationException("Image load failed"));
            return;
        }

        var imageSource = BuildSource(result);
        if (imageSource is null)
        {
            this.ShowError(uri, new InvalidOperationException("Image loaded but produced no readable content"));
            return;
        }

        this.SetProgress(new ImageLoadProgress(ImageLoadState.Loaded, result.ContentLength ?? 0, result.ContentLength));
        await this.ShowImageAsync(imageSource).ConfigureAwait(true);
        this.RaiseLoaded(uri, result.Origin, result.ContentLength);
    }


    /// <summary>
    /// Turns a service result into something MAUI can render.
    /// </summary>
    /// <remarks>
    /// Bytes are preferred over the file path when both are available - a stream over an in-memory
    /// array skips a file read per cell in a scrolling list. Crucially the stream is built from a
    /// <b>factory</b>, not a single stream instance: MAUI reads a stream source once and may re-read
    /// it after a handler is recycled, and a one-shot stream renders blank the second time.
    /// </remarks>
    static ImageSource? BuildSource(ImageResult result)
    {
        if (result.Bytes is { Length: > 0 } bytes)
            return ImageSource.FromStream(() => new MemoryStream(bytes));

        if (!String.IsNullOrWhiteSpace(result.FilePath) && File.Exists(result.FilePath))
            return ImageSource.FromFile(result.FilePath);

        return null;
    }


    async Task ShowImageAsync(ImageSource source)
    {
        this.targetImage.Source = source;
        this.targetImage.IsVisible = true;
        this.ApplyState();

        if (this.FadeInDuration == 0)
        {
            this.targetImage.Opacity = 1;
            return;
        }

        this.targetImage.Opacity = 0;
        await this.targetImage.FadeToAsync(1, this.FadeInDuration).ConfigureAwait(true);
    }


    void ShowError(string? uri, Exception error)
    {
        this.targetImage.Source = null;
        this.targetImage.Opacity = 0;
        this.SetValue(LoadErrorPropertyKey, error);
        this.SetProgress(new ImageLoadProgress(ImageLoadState.Failed));

        this.ImageFailed?.Invoke(this, new ImageFailedEventArgs(uri, error));

        var command = this.ImageFailedCommand;
        if (command?.CanExecute(error) == true)
            command.Execute(error);
    }


    void RaiseLoaded(string? uri, ImageOrigin origin, long? contentLength)
    {
        var args = new ImageLoadedEventArgs(uri, origin, contentLength);
        this.ImageLoaded?.Invoke(this, args);

        var command = this.ImageLoadedCommand;
        if (command?.CanExecute(args) == true)
            command.Execute(args);
    }


    void SetProgress(ImageLoadProgress progress)
    {
        this.SetValue(ProgressPropertyKey, progress);
        this.SetValue(StatePropertyKey, progress.State);
        this.SetValue(IsLoadingPropertyKey, progress.State is ImageLoadState.Queued or ImageLoadState.Downloading);

        this.ring.Percent = progress.Percent;

        // A custom template binds against the progress record, and the record is replaced rather
        // than mutated, so the host's binding context has to be re-pointed each time.
        if (this.LoadingTemplate is not null)
            this.loadingHost.BindingContext = progress;

        if (this.ErrorTemplate is not null)
            this.errorHost.BindingContext = progress;

        this.ApplyState();
    }


    void ApplyState()
    {
        var state = this.State;
        var isLoading = state is ImageLoadState.Queued or ImageLoadState.Downloading;
        var isFailed = state == ImageLoadState.Failed;
        var isLoaded = state == ImageLoadState.Loaded;

        this.loadingHost.IsVisible = isLoading;
        this.errorHost.IsVisible = isFailed;
        this.targetImage.IsVisible = isLoaded;

        // Set on the ring itself, not just its host. Hiding a parent does not raise IsVisible on the
        // child, and the ring uses its own IsVisible to decide whether to hold the shared frame
        // clock - a loaded image reports a null Percent, which would otherwise read as "spinning
        // forever" and keep a 60fps timer awake on every page holding one.
        this.ring.IsVisible = isLoading;

        // The placeholder stays behind the ring rather than being swapped out for it, so a blurred
        // thumbnail with a progress ring over it is just two properties.
        this.placeholderImage.IsVisible = !isLoaded && !isFailed && this.PlaceholderImage is not null;
    }


    void RebuildLoadingContent()
    {
        var template = this.LoadingTemplate;
        if (template is null)
        {
            this.loadingHost.BindingContext = null;
            this.loadingHost.Content = this.ring;
            return;
        }

        this.loadingHost.Content = template.CreateContent() as View;
        this.loadingHost.BindingContext = this.Progress;
    }


    void RebuildErrorContent()
    {
        var template = this.ErrorTemplate;
        if (template is null)
        {
            this.errorHost.BindingContext = null;
            this.errorHost.Content = this.errorImage;
            return;
        }

        this.errorHost.Content = template.CreateContent() as View;
        this.errorHost.BindingContext = this.Progress;
    }


    void CancelPending()
    {
        var pending = Interlocked.Exchange(ref this.cts, null);
        if (pending is null)
            return;

        try
        {
            pending.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // already torn down
        }
        pending.Dispose();
    }


    /// <summary>True for anything the image service should download.</summary>
    internal static bool IsRemote(string uri)
        => System.Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
           && (parsed.Scheme == System.Uri.UriSchemeHttp || parsed.Scheme == System.Uri.UriSchemeHttps);


    /// <summary>
    /// Finds the registered service, falling back to a self-built one.
    /// </summary>
    /// <remarks>
    /// The fallback is deliberate. A control constructed outside a running app - a headless test
    /// host, a design-time preview - has no MauiContext, and throwing there would make the control
    /// impossible to unit test and would crash the previewer over a service that has a perfectly
    /// good default.
    /// </remarks>
    static IImageService ResolveService(ShinyImage image)
    {
        var services = image.Handler?.MauiContext?.Services
                       ?? Application.Current?.Handler?.MauiContext?.Services;

        return services?.GetService<IImageService>() ?? DefaultService.Value;
    }

    static readonly Lazy<IImageService> DefaultService = new(() => new ImageService());


    /// <summary>Cancels any in-flight load and releases the control's resources.</summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }


    /// <summary>Cancels any in-flight load and releases the control's resources.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (this.isDisposed || !disposing)
            return;

        this.isDisposed = true;
        this.Loaded -= this.OnControlLoaded;
        this.Unloaded -= this.OnControlUnloaded;
        this.CancelPending();
    }
}
