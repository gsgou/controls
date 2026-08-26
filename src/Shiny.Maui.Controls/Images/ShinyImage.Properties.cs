using System.Windows.Input;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Images;

public partial class ShinyImage
{
    // Source

    /// <summary>
    /// The image to load.
    /// </summary>
    /// <remarks>
    /// <para>An absolute <c>http</c>/<c>https</c> URI goes through <see cref="IImageService"/>.
    /// <c>resource://MyApp.Assets.logo.svg</c> reads an embedded resource - add <c>MyLib/</c> before
    /// the name to say which assembly. A <c>data:</c> URI is decoded inline. Anything else is a file
    /// path or a bundled asset, read from disk or from the app package. Only the first involves any
    /// download machinery.</para>
    /// <para>SVG is detected from the payload rather than the extension, so it works on an endpoint
    /// that serves vectors from a URL that does not say so.</para>
    /// </remarks>
    public static readonly BindableProperty UriProperty = BindableProperty.Create(
        nameof(Uri), typeof(string), typeof(ShinyImage), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ShinyImage), () => ((ShinyImage)b).BeginLoad())
    );

    /// <inheritdoc cref="UriProperty" />
    public string? Uri
    {
        get => (string?)this.GetValue(UriProperty);
        set => this.SetValue(UriProperty, value);
    }


    /// <summary>
    /// An explicit <see cref="ImageSource"/>, which takes precedence over <see cref="Uri"/> and
    /// bypasses the service entirely. Use it for embedded resources, streams you already have, or
    /// font images - the loading and error states still work, they just resolve immediately.
    /// </summary>
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source), typeof(ImageSource), typeof(ShinyImage), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ShinyImage), () => ((ShinyImage)b).BeginLoad())
    );

    /// <inheritdoc cref="SourceProperty" />
    public ImageSource? Source
    {
        get => (ImageSource?)this.GetValue(SourceProperty);
        set => this.SetValue(SourceProperty, value);
    }


    // Placeholder / error artwork

    /// <summary>
    /// Artwork shown before and during the load. It stays behind the loading ring, so a blurred
    /// thumbnail plus a progress ring is one binding away.
    /// </summary>
    public static readonly BindableProperty PlaceholderImageProperty = BindableProperty.Create(
        nameof(PlaceholderImage), typeof(ImageSource), typeof(ShinyImage), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ShinyImage), () =>
        {
            var img = (ShinyImage)b;
            img.placeholderImage.Source = (ImageSource?)n;
            img.ApplyState();
        })
    );

    /// <inheritdoc cref="PlaceholderImageProperty" />
    public ImageSource? PlaceholderImage
    {
        get => (ImageSource?)this.GetValue(PlaceholderImageProperty);
        set => this.SetValue(PlaceholderImageProperty, value);
    }


    /// <summary>Artwork shown when the load fails. Ignored when <see cref="ErrorTemplate"/> is set.</summary>
    public static readonly BindableProperty ErrorImageProperty = BindableProperty.Create(
        nameof(ErrorImage), typeof(ImageSource), typeof(ShinyImage), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ShinyImage), () =>
        {
            var img = (ShinyImage)b;
            img.errorImage.Source = (ImageSource?)n;
            img.ApplyState();
        })
    );

    /// <inheritdoc cref="ErrorImageProperty" />
    public ImageSource? ErrorImage
    {
        get => (ImageSource?)this.GetValue(ErrorImageProperty);
        set => this.SetValue(ErrorImageProperty, value);
    }


    // Full overrides

    /// <summary>
    /// Replaces the built-in loading ring entirely. The template's binding context is the live
    /// <see cref="ImageLoadProgress"/>, so <c>State</c>, <c>Percent</c>, <c>PercentDisplay</c>,
    /// <c>BytesRead</c> and <c>IsIndeterminate</c> are all bindable from inside it.
    /// </summary>
    public static readonly BindableProperty LoadingTemplateProperty = BindableProperty.Create(
        nameof(LoadingTemplate), typeof(DataTemplate), typeof(ShinyImage), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ShinyImage), () => ((ShinyImage)b).RebuildLoadingContent())
    );

    /// <inheritdoc cref="LoadingTemplateProperty" />
    public DataTemplate? LoadingTemplate
    {
        get => (DataTemplate?)this.GetValue(LoadingTemplateProperty);
        set => this.SetValue(LoadingTemplateProperty, value);
    }


    /// <summary>
    /// Replaces the built-in error artwork. The binding context is the failing
    /// <see cref="ImageLoadProgress"/>; the exception is on <see cref="LoadError"/>.
    /// </summary>
    public static readonly BindableProperty ErrorTemplateProperty = BindableProperty.Create(
        nameof(ErrorTemplate), typeof(DataTemplate), typeof(ShinyImage), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ShinyImage), () => ((ShinyImage)b).RebuildErrorContent())
    );

    /// <inheritdoc cref="ErrorTemplateProperty" />
    public DataTemplate? ErrorTemplate
    {
        get => (DataTemplate?)this.GetValue(ErrorTemplateProperty);
        set => this.SetValue(ErrorTemplateProperty, value);
    }


    // Appearance

    /// <summary>How the image is scaled into the control. Applies to the placeholder too.</summary>
    /// <remarks>
    /// For SVG this wins over the file's own <c>preserveAspectRatio</c> scaling, so a vector and a
    /// raster in the same layout behave identically. The file's alignment - which corner the
    /// leftover space goes to - is still honoured.
    /// </remarks>
    public static readonly BindableProperty AspectProperty = BindableProperty.Create(
        nameof(Aspect), typeof(Aspect), typeof(ShinyImage), Aspect.AspectFit,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ShinyImage), () =>
        {
            var img = (ShinyImage)b;
            img.targetImage.Aspect = (Aspect)n;
            img.placeholderImage.Aspect = (Aspect)n;
            img.ApplyVectorAppearance();
        })
    );

    /// <inheritdoc cref="AspectProperty" />
    public Aspect Aspect
    {
        get => (Aspect)this.GetValue(AspectProperty);
        set => this.SetValue(AspectProperty, value);
    }


    /// <summary>
    /// Milliseconds the image fades in over once loaded. Zero shows it instantly.
    /// </summary>
    /// <remarks>
    /// A short fade is not decoration here: a cache hit resolves within a frame, and popping straight
    /// from placeholder to image reads as a flicker when scrolling a list.
    /// </remarks>
    public static readonly BindableProperty FadeInDurationProperty = BindableProperty.Create(
        nameof(FadeInDuration), typeof(uint), typeof(ShinyImage), (uint)150
    );

    /// <inheritdoc cref="FadeInDurationProperty" />
    public uint FadeInDuration
    {
        get => (uint)this.GetValue(FadeInDurationProperty);
        set => this.SetValue(FadeInDurationProperty, value);
    }


    /// <summary>Whether the percentage is drawn inside the ring. Never shown when indeterminate.</summary>
    public static readonly BindableProperty ShowProgressTextProperty = BindableProperty.Create(
        nameof(ShowProgressText), typeof(bool), typeof(ShinyImage), true,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ShinyImage), () => ((ShinyImage)b).ring.ShowPercentText = (bool)n)
    );

    /// <inheritdoc cref="ShowProgressTextProperty" />
    public bool ShowProgressText
    {
        get => (bool)this.GetValue(ShowProgressTextProperty);
        set => this.SetValue(ShowProgressTextProperty, value);
    }


    /// <summary>Diameter of the loading ring.</summary>
    public static readonly BindableProperty RingSizeProperty = BindableProperty.Create(
        nameof(RingSize), typeof(double), typeof(ShinyImage), 48.0,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ShinyImage), () =>
        {
            var img = (ShinyImage)b;
            img.ring.WidthRequest = (double)n;
            img.ring.HeightRequest = (double)n;
        })
    );

    /// <inheritdoc cref="RingSizeProperty" />
    public double RingSize
    {
        get => (double)this.GetValue(RingSizeProperty);
        set => this.SetValue(RingSizeProperty, value);
    }


    /// <summary>Colour of the ring's progress arc. Null uses the theme Primary token.</summary>
    public static readonly BindableProperty RingColorProperty = BindableProperty.Create(
        nameof(RingColor), typeof(Color), typeof(ShinyImage), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ShinyImage), () => ((ShinyImage)b).ring.RingColor = (Color?)n)
    );

    /// <inheritdoc cref="RingColorProperty" />
    public Color? RingColor
    {
        get => (Color?)this.GetValue(RingColorProperty);
        set => this.SetValue(RingColorProperty, value);
    }


    /// <summary>Colour of the ring's unfilled track. Null uses the theme SurfaceContainerHighest token.</summary>
    public static readonly BindableProperty RingTrackColorProperty = BindableProperty.Create(
        nameof(RingTrackColor), typeof(Color), typeof(ShinyImage), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ShinyImage), () => ((ShinyImage)b).ring.TrackColor = (Color?)n)
    );

    /// <inheritdoc cref="RingTrackColorProperty" />
    public Color? RingTrackColor
    {
        get => (Color?)this.GetValue(RingTrackColorProperty);
        set => this.SetValue(RingTrackColorProperty, value);
    }


    /// <summary>Colour of the percentage label. Null uses the theme OnSurface token.</summary>
    public static readonly BindableProperty ProgressTextColorProperty = BindableProperty.Create(
        nameof(ProgressTextColor), typeof(Color), typeof(ShinyImage), null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ShinyImage), () => ((ShinyImage)b).ring.TextColor = (Color?)n)
    );

    /// <inheritdoc cref="ProgressTextColorProperty" />
    public Color? ProgressTextColor
    {
        get => (Color?)this.GetValue(ProgressTextColorProperty);
        set => this.SetValue(ProgressTextColorProperty, value);
    }


    /// <summary>
    /// What <c>currentColor</c> resolves to in an SVG - the tint a single-colour icon takes on.
    /// Null draws it black. Ignored by raster images, and by artwork that names its own colours.
    /// </summary>
    /// <remarks>
    /// This is why an icon set ships one file per glyph rather than one per glyph per colour: the
    /// artwork says <c>fill="currentColor"</c> and every placement decides for itself. The tint is
    /// applied at draw time, so the same parsed document is shared by placements that disagree
    /// about the colour.
    /// </remarks>
    public static readonly BindableProperty SvgTintColorProperty = BindableProperty.Create(
        nameof(SvgTintColor), typeof(Color), typeof(ShinyImage), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ShinyImage), () => ((ShinyImage)b).ApplyVectorAppearance())
    );

    /// <inheritdoc cref="SvgTintColorProperty" />
    public Color? SvgTintColor
    {
        get => (Color?)this.GetValue(SvgTintColorProperty);
        set => this.SetValue(SvgTintColorProperty, value);
    }


    // Caching

    /// <summary>Whether this image participates in the memory and disk caches.</summary>
    public static readonly BindableProperty CacheEnabledProperty = BindableProperty.Create(
        nameof(CacheEnabled), typeof(bool), typeof(ShinyImage), true
    );

    /// <inheritdoc cref="CacheEnabledProperty" />
    public bool CacheEnabled
    {
        get => (bool)this.GetValue(CacheEnabledProperty);
        set => this.SetValue(CacheEnabledProperty, value);
    }


    /// <summary>
    /// Overrides <see cref="ImageOptions.DiskCacheDuration"/> for this image. A server-supplied
    /// <c>Cache-Control</c> or <c>Expires</c> still wins over both.
    /// </summary>
    public static readonly BindableProperty CacheDurationProperty = BindableProperty.Create(
        nameof(CacheDuration), typeof(TimeSpan?), typeof(ShinyImage), null
    );

    /// <inheritdoc cref="CacheDurationProperty" />
    public TimeSpan? CacheDuration
    {
        get => (TimeSpan?)this.GetValue(CacheDurationProperty);
        set => this.SetValue(CacheDurationProperty, value);
    }


    // Read-only state

    static readonly BindablePropertyKey StatePropertyKey = BindableProperty.CreateReadOnly(
        nameof(State), typeof(ImageLoadState), typeof(ShinyImage), ImageLoadState.None
    );

    /// <summary>Where the current load is.</summary>
    public static readonly BindableProperty StateProperty = StatePropertyKey.BindableProperty;

    /// <inheritdoc cref="StateProperty" />
    public ImageLoadState State => (ImageLoadState)this.GetValue(StateProperty);


    static readonly BindablePropertyKey ProgressPropertyKey = BindableProperty.CreateReadOnly(
        nameof(Progress), typeof(ImageLoadProgress), typeof(ShinyImage), null,
        defaultValueCreator: _ => ImageLoadProgress.None
    );

    /// <summary>The live progress snapshot - also the binding context of <see cref="LoadingTemplate"/>.</summary>
    public static readonly BindableProperty ProgressProperty = ProgressPropertyKey.BindableProperty;

    /// <inheritdoc cref="ProgressProperty" />
    public ImageLoadProgress Progress => (ImageLoadProgress)this.GetValue(ProgressProperty);


    static readonly BindablePropertyKey IsLoadingPropertyKey = BindableProperty.CreateReadOnly(
        nameof(IsLoading), typeof(bool), typeof(ShinyImage), false
    );

    /// <summary>True while queued or downloading. Handy for a trigger on a surrounding container.</summary>
    public static readonly BindableProperty IsLoadingProperty = IsLoadingPropertyKey.BindableProperty;

    /// <inheritdoc cref="IsLoadingProperty" />
    public bool IsLoading => (bool)this.GetValue(IsLoadingProperty);


    static readonly BindablePropertyKey LoadErrorPropertyKey = BindableProperty.CreateReadOnly(
        nameof(LoadError), typeof(Exception), typeof(ShinyImage), null
    );

    /// <summary>Why the last load failed, or null.</summary>
    public static readonly BindableProperty LoadErrorProperty = LoadErrorPropertyKey.BindableProperty;

    /// <inheritdoc cref="LoadErrorProperty" />
    public Exception? LoadError => (Exception?)this.GetValue(LoadErrorProperty);


    // Commands and events
    //
    // Named ImageLoaded/ImageFailed rather than the more obvious Loaded/Failed: VisualElement
    // already declares a Loaded event, and shadowing it would silently break anything relying on
    // the base member.

    /// <summary>Invoked once the image is on screen.</summary>
    public static readonly BindableProperty ImageLoadedCommandProperty = BindableProperty.Create(
        nameof(ImageLoadedCommand), typeof(ICommand), typeof(ShinyImage)
    );

    /// <inheritdoc cref="ImageLoadedCommandProperty" />
    public ICommand? ImageLoadedCommand
    {
        get => (ICommand?)this.GetValue(ImageLoadedCommandProperty);
        set => this.SetValue(ImageLoadedCommandProperty, value);
    }


    /// <summary>Invoked with the exception when a load fails.</summary>
    public static readonly BindableProperty ImageFailedCommandProperty = BindableProperty.Create(
        nameof(ImageFailedCommand), typeof(ICommand), typeof(ShinyImage)
    );

    /// <inheritdoc cref="ImageFailedCommandProperty" />
    public ICommand? ImageFailedCommand
    {
        get => (ICommand?)this.GetValue(ImageFailedCommandProperty);
        set => this.SetValue(ImageFailedCommandProperty, value);
    }


    /// <summary>Raised once the image is on screen.</summary>
    public event EventHandler<ImageLoadedEventArgs>? ImageLoaded;

    /// <summary>Raised when a load fails.</summary>
    public event EventHandler<ImageFailedEventArgs>? ImageFailed;
}


/// <summary>Details of a successful load.</summary>
/// <param name="Uri">What was loaded. Null when an explicit <c>Source</c> was used.</param>
/// <param name="Origin">Which tier answered - useful for verifying caching actually works.</param>
/// <param name="ContentLength">Size in bytes, when known.</param>
public record ImageLoadedEventArgs(string? Uri, ImageOrigin Origin, long? ContentLength);


/// <summary>Details of a failed load.</summary>
/// <param name="Uri">What was being loaded.</param>
/// <param name="Error">Why it failed.</param>
public record ImageFailedEventArgs(string? Uri, Exception Error);
