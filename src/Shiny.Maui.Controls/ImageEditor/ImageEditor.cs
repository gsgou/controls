using Microsoft.Maui.Controls.Shapes;
using Shiny.Maui.Controls.ColorPicker;
using Shiny.Maui.Controls.FontPicker;
using Shiny.Maui.Controls.ImageEditor.EditActions;
using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.ImageEditor;

public partial class ImageEditor : ContentView
{
    readonly Grid rootGrid;
    readonly GraphicsView graphicsView;
    readonly ImageEditorDrawable drawable;
    readonly ImageEditorState state;
    View? toolbarView;
    ColorPickerButton? drawColorButton;
    FontPickerButton? fontPickerButton;
    FontSizePickerButton? fontSizePickerButton;
    Label? zoomReadout;
    Border? undoButton;
    Border? redoButton;
    Border? resetButton;

    public ImageEditor()
    {
        state = new ImageEditorState();
        drawable = new ImageEditorDrawable { State = state };

        state.StateChanged += OnStateChanged;

        graphicsView = new GraphicsView
        {
            Drawable = drawable,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        SetupGestures();
        SetupCommands();

        rootGrid = new Grid
        {
            RowDefinitions =
            [
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            ],
            Children = { graphicsView }
        };

        Grid.SetRow(graphicsView, 0);

        BuildDefaultToolbar();

        Content = rootGrid;

        // Invalidate once layout is ready so images set during binding actually render. A resize
        // (rotation, split view) also changes the viewport the pan is clamped against, so the
        // offsets are re-clamped on the next frame — once the drawable knows the new viewport.
        graphicsView.SizeChanged += (_, _) =>
        {
            Invalidate();
            if (zoomScale > 1.001f)
                Dispatcher.Dispatch(() =>
                {
                    ClampOffsets();
                    PushTransformToDrawable();
                });
        };

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(ImageEditor));
    }

    #region Bindable Properties

    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source),
        typeof(ImageSource),
        typeof(ImageEditor),
        null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                _ = ((ImageEditor)b).OnSourceChangedAsync();
            }));

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly BindableProperty CurrentToolModeProperty = BindableProperty.Create(
        nameof(CurrentToolMode),
        typeof(ImageEditorToolMode),
        typeof(ImageEditor),
        ImageEditorToolMode.Move,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).OnToolModeChanged((ImageEditorToolMode)n);
            }));

    public ImageEditorToolMode CurrentToolMode
    {
        get => (ImageEditorToolMode)GetValue(CurrentToolModeProperty);
        set => SetValue(CurrentToolModeProperty, value);
    }

    public static readonly BindableProperty AllowCropProperty = BindableProperty.Create(
        nameof(AllowCrop), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public bool AllowCrop
    {
        get => (bool)GetValue(AllowCropProperty);
        set => SetValue(AllowCropProperty, value);
    }

    public static readonly BindableProperty AllowRotateProperty = BindableProperty.Create(
        nameof(AllowRotate), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public bool AllowRotate
    {
        get => (bool)GetValue(AllowRotateProperty);
        set => SetValue(AllowRotateProperty, value);
    }

    public static readonly BindableProperty AllowDrawProperty = BindableProperty.Create(
        nameof(AllowDraw), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public bool AllowDraw
    {
        get => (bool)GetValue(AllowDrawProperty);
        set => SetValue(AllowDrawProperty, value);
    }

    public static readonly BindableProperty AllowTextAnnotationProperty = BindableProperty.Create(
        nameof(AllowTextAnnotation), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public bool AllowTextAnnotation
    {
        get => (bool)GetValue(AllowTextAnnotationProperty);
        set => SetValue(AllowTextAnnotationProperty, value);
    }

    public static readonly BindableProperty AllowLineProperty = BindableProperty.Create(
        nameof(AllowLine), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public bool AllowLine
    {
        get => (bool)GetValue(AllowLineProperty);
        set => SetValue(AllowLineProperty, value);
    }

    public static readonly BindableProperty AllowArrowProperty = BindableProperty.Create(
        nameof(AllowArrow), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public bool AllowArrow
    {
        get => (bool)GetValue(AllowArrowProperty);
        set => SetValue(AllowArrowProperty, value);
    }

    public static readonly BindableProperty AllowFontSelectionProperty = BindableProperty.Create(
        nameof(AllowFontSelection), typeof(bool), typeof(ImageEditor), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public bool AllowFontSelection
    {
        get => (bool)GetValue(AllowFontSelectionProperty);
        set => SetValue(AllowFontSelectionProperty, value);
    }

    public static readonly BindableProperty AllowFontSizeSelectionProperty = BindableProperty.Create(
        nameof(AllowFontSizeSelection), typeof(bool), typeof(ImageEditor), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public bool AllowFontSizeSelection
    {
        get => (bool)GetValue(AllowFontSizeSelectionProperty);
        set => SetValue(AllowFontSizeSelectionProperty, value);
    }

    public static readonly BindableProperty AllowZoomProperty = BindableProperty.Create(
        nameof(AllowZoom), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                var editor = (ImageEditor)b;
                if (!(bool)n)
                    editor.ZoomToFit();
                editor.BuildDefaultToolbar();
            }));

    public bool AllowZoom
    {
        get => (bool)GetValue(AllowZoomProperty);
        set => SetValue(AllowZoomProperty, value);
    }

    public static readonly BindableProperty CanUndoProperty = BindableProperty.Create(
        nameof(CanUndo), typeof(bool), typeof(ImageEditor), false, BindingMode.OneWayToSource);

    public bool CanUndo
    {
        get => (bool)GetValue(CanUndoProperty);
        private set => SetValue(CanUndoProperty, value);
    }

    public static readonly BindableProperty CanRedoProperty = BindableProperty.Create(
        nameof(CanRedo), typeof(bool), typeof(ImageEditor), false, BindingMode.OneWayToSource);

    public bool CanRedo
    {
        get => (bool)GetValue(CanRedoProperty);
        private set => SetValue(CanRedoProperty, value);
    }

    public static readonly BindableProperty DrawStrokeColorProperty = BindableProperty.Create(
        nameof(DrawStrokeColor), typeof(Color), typeof(ImageEditor), Colors.White,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
            var editor = (ImageEditor)b;
            editor.drawable.ActiveStrokeColor = (Color)n;
            if (editor.drawColorButton != null)
                editor.drawColorButton.SelectedColor = (Color)n;
        }));

    public Color DrawStrokeColor
    {
        get => (Color)GetValue(DrawStrokeColorProperty);
        set => SetValue(DrawStrokeColorProperty, value);
    }

    public static readonly BindableProperty DrawStrokeWidthProperty = BindableProperty.Create(
        nameof(DrawStrokeWidth), typeof(double), typeof(ImageEditor), 3.0,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                var editor = (ImageEditor)b;
                editor.drawable.ActiveStrokeWidth = (float)(double)n;
                editor.BuildDefaultToolbar();
            }));

    public double DrawStrokeWidth
    {
        get => (double)GetValue(DrawStrokeWidthProperty);
        set => SetValue(DrawStrokeWidthProperty, value);
    }

    public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
        nameof(TextFontSize), typeof(double), typeof(ImageEditor), 16.0,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
            var editor = (ImageEditor)b;
            if (editor.activeTextEntry != null)
                editor.activeTextEntry.FontSize = (double)n;
            if (editor.fontSizePickerButton != null)
                editor.fontSizePickerButton.SelectedFontSize = (double)n;
        }));

    public double TextFontSize
    {
        get => (double)GetValue(TextFontSizeProperty);
        set => SetValue(TextFontSizeProperty, value);
    }

    public static readonly BindableProperty AvailableFontSizesProperty = BindableProperty.Create(
        nameof(AvailableFontSizes), typeof(IList<double>), typeof(ImageEditor), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public IList<double>? AvailableFontSizes
    {
        get => (IList<double>?)GetValue(AvailableFontSizesProperty);
        set => SetValue(AvailableFontSizesProperty, value);
    }

    public static readonly BindableProperty AnnotationTextColorProperty = BindableProperty.Create(
        nameof(AnnotationTextColor), typeof(Color), typeof(ImageEditor), Colors.White);

    public Color AnnotationTextColor
    {
        get => (Color)GetValue(AnnotationTextColorProperty);
        set => SetValue(AnnotationTextColorProperty, value);
    }

    public static readonly BindableProperty TextFontFamilyProperty = BindableProperty.Create(
        nameof(TextFontFamily), typeof(string), typeof(ImageEditor), null,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
            var editor = (ImageEditor)b;
            if (editor.activeTextEntry != null)
                editor.activeTextEntry.FontFamily = n as string;
            if (editor.fontPickerButton != null)
                editor.fontPickerButton.SelectedFont = n as string;
        }));

    public string? TextFontFamily
    {
        get => (string?)GetValue(TextFontFamilyProperty);
        set => SetValue(TextFontFamilyProperty, value);
    }

    public static readonly BindableProperty AvailableFontsProperty = BindableProperty.Create(
        nameof(AvailableFonts), typeof(IList<string>), typeof(ImageEditor), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public IList<string>? AvailableFonts
    {
        get => (IList<string>?)GetValue(AvailableFontsProperty);
        set => SetValue(AvailableFontsProperty, value);
    }

    public static readonly BindableProperty ToolbarTemplateProperty = BindableProperty.Create(
        nameof(ToolbarTemplate), typeof(DataTemplate), typeof(ImageEditor), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).ApplyToolbarTemplate();
            }));

    public DataTemplate? ToolbarTemplate
    {
        get => (DataTemplate?)GetValue(ToolbarTemplateProperty);
        set => SetValue(ToolbarTemplateProperty, value);
    }

    public static readonly BindableProperty ShowToolLabelsProperty = BindableProperty.Create(
        nameof(ShowToolLabels), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    /// <summary>
    /// Shows a caption under each tool icon. Turn this off for a compact icon-only bar.
    /// </summary>
    public bool ShowToolLabels
    {
        get => (bool)GetValue(ShowToolLabelsProperty);
        set => SetValue(ShowToolLabelsProperty, value);
    }

    public static readonly BindableProperty ShowStrokeWidthPickerProperty = BindableProperty.Create(
        nameof(ShowStrokeWidthPicker), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    /// <summary>Shows the pen-weight presets alongside the colour swatch for the ink tools.</summary>
    public bool ShowStrokeWidthPicker
    {
        get => (bool)GetValue(ShowStrokeWidthPickerProperty);
        set => SetValue(ShowStrokeWidthPickerProperty, value);
    }

    public static readonly BindableProperty StrokeWidthPresetsProperty = BindableProperty.Create(
        nameof(StrokeWidthPresets), typeof(IList<double>), typeof(ImageEditor), null,
        defaultValueCreator: _ => new List<double> { 2, 4, 8 },
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    /// <summary>Pen weights offered by the stroke-width picker.</summary>
    public IList<double> StrokeWidthPresets
    {
        get => (IList<double>)GetValue(StrokeWidthPresetsProperty);
        set => SetValue(StrokeWidthPresetsProperty, value);
    }

    public static readonly BindableProperty ToolbarBackgroundColorProperty = BindableProperty.Create(
        nameof(ToolbarBackgroundColor), typeof(Color), typeof(ImageEditor), Color.FromRgba(20, 20, 22, 0.86f),
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    /// <summary>
    /// Background of the default toolbar. It defaults to a dark scrim because the bar floats over
    /// arbitrary photos and has to stay legible on all of them.
    /// </summary>
    public Color ToolbarBackgroundColor
    {
        get => (Color)GetValue(ToolbarBackgroundColorProperty);
        set => SetValue(ToolbarBackgroundColorProperty, value);
    }

    public static readonly BindableProperty ToolbarPositionProperty = BindableProperty.Create(
        nameof(ToolbarPosition), typeof(ToolbarPosition), typeof(ImageEditor), ToolbarPosition.Bottom,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).UpdateToolbarPosition();
            }));

    public ToolbarPosition ToolbarPosition
    {
        get => (ToolbarPosition)GetValue(ToolbarPositionProperty);
        set => SetValue(ToolbarPositionProperty, value);
    }

    public static readonly BindableProperty UseFeedbackProperty = BindableProperty.Create(
        nameof(UseFeedback), typeof(bool), typeof(ImageEditor), true);

    public bool UseFeedback
    {
        get => (bool)GetValue(UseFeedbackProperty);
        set => SetValue(UseFeedbackProperty, value);
    }

    public static readonly BindableProperty CropApplyTextProperty = BindableProperty.Create(
        nameof(CropApplyText), typeof(string), typeof(ImageEditor), "Apply",
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public string CropApplyText
    {
        get => (string)GetValue(CropApplyTextProperty);
        set => SetValue(CropApplyTextProperty, value);
    }

    public static readonly BindableProperty CropCancelTextProperty = BindableProperty.Create(
        nameof(CropCancelText), typeof(string), typeof(ImageEditor), "Cancel",
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public string CropCancelText
    {
        get => (string)GetValue(CropCancelTextProperty);
        set => SetValue(CropCancelTextProperty, value);
    }

    public static readonly BindableProperty SaveCommandProperty = BindableProperty.Create(
        nameof(SaveCommand), typeof(System.Windows.Input.ICommand), typeof(ImageEditor), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public System.Windows.Input.ICommand? SaveCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    public static readonly BindableProperty SaveTextProperty = BindableProperty.Create(
        nameof(SaveText), typeof(string), typeof(ImageEditor), "Save",
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    public string SaveText
    {
        get => (string)GetValue(SaveTextProperty);
        set => SetValue(SaveTextProperty, value);
    }

    #endregion

    async Task OnSourceChangedAsync()
    {
        if (Source == null)
        {
            drawable.Image = null;
        }
        else
        {
            try
            {
                var stream = await ResolveImageSourceStreamAsync(Source);
                if (stream != null)
                {
                    await using (stream)
                        drawable.Image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);

                }
                else
                {
                    drawable.Image = null;
                }
            }
            catch
            {
                drawable.Image = null;
            }
        }

        state.Reset();
        ResetViewTransform();
        Invalidate();
    }

    static async Task<Stream?> ResolveImageSourceStreamAsync(ImageSource source)
    {
        switch (source)
        {
            case FileImageSource fileSource:
                // Try regular file path first
                if (File.Exists(fileSource.File))
                    return File.OpenRead(fileSource.File);

                // Try app package file (raw assets/bundles)
                try { return await FileSystem.OpenAppPackageFileAsync(fileSource.File); }
                catch { /* fall through to platform-specific loading */ }

#if IOS || MACCATALYST
                // On iOS/Catalyst, MAUI images are in the app bundle
                var uiImage = UIKit.UIImage.FromBundle(System.IO.Path.GetFileNameWithoutExtension(fileSource.File));
                if (uiImage != null)
                {
                    var pngData = uiImage.AsPNG();
                    if (pngData != null)
                        return pngData.AsStream();
                }
#elif ANDROID
                // On Android, try loading via the MAUI resource system
                var context = Android.App.Application.Context;
                var resName = System.IO.Path.GetFileNameWithoutExtension(fileSource.File)?.ToLowerInvariant();
                if (resName != null && context.Resources != null)
                {
                    var resId = context.Resources.GetIdentifier(resName, "drawable", context.PackageName);
                    if (resId != 0)
                    {
                        var drawable = context.Resources.GetDrawable(resId, context.Theme);
                        if (drawable is Android.Graphics.Drawables.BitmapDrawable bd && bd.Bitmap != null)
                        {
                            var ms = new MemoryStream();
                            await bd.Bitmap.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Png!, 100, ms);
                            ms.Position = 0;
                            return ms;
                        }
                    }
                }
#endif
                return null;

            case StreamImageSource streamSource:
                return await streamSource.Stream(CancellationToken.None);

            case UriImageSource uriSource:
                using (var client = new HttpClient())
                    return new MemoryStream(await client.GetByteArrayAsync(uriSource.Uri));

            default:
                return null;
        }
    }

    void OnToolModeChanged(ImageEditorToolMode mode)
    {
        // Finalize any in-progress operations
        FinalizeCurrentOperation();

        if (UseFeedback)
            FeedbackHelper.Execute(this, "ToolModeChanged", mode.ToString());

        drawable.ToolMode = mode;

        // The crop rect is normalised against the whole image, so entering crop returns to
        // fit-to-view; every other tool keeps whatever zoom the user set up
        if (mode == ImageEditorToolMode.Crop)
        {
            drawable.ActiveCropRect = new RectF(0.1f, 0.1f, 0.8f, 0.8f);
            ZoomToFit();
        }
        else
        {
            drawable.ActiveCropRect = null;
        }

        BuildDefaultToolbar();
        Invalidate();
    }

    void OnStateChanged()
    {
        CanUndo = state.CanUndo;
        CanRedo = state.CanRedo;

        // Toggle in place rather than rebuilding — the toolbar rebuilds on tool changes only,
        // so drawing a stroke doesn't flicker the whole bar
        if (undoButton != null) SetButtonEnabled(undoButton, CanUndo);
        if (redoButton != null) SetButtonEnabled(redoButton, CanRedo);
        if (resetButton != null) SetButtonEnabled(resetButton, CanUndo);

        Invalidate();
    }

    void FinalizeCurrentOperation()
    {
        // Finalize in-progress text entry
        CommitActiveTextEntry();

        // Finalize in-progress draw stroke
        if (drawable.ActiveStrokePoints is { Count: >= 2 })
        {
            var imageRect = drawable.GetImageRect();
            if (imageRect is { Width: > 0, Height: > 0 })
            {
                var normalized = drawable.ActiveStrokePoints
                    .Select(p => new PointF(
                        (p.X - imageRect.X) / imageRect.Width,
                        (p.Y - imageRect.Y) / imageRect.Height))
                    .ToArray();

                state.Push(new DrawStrokeAction
                {
                    Points = normalized,
                    StrokeColor = DrawStrokeColor,
                    StrokeWidth = (float)DrawStrokeWidth,
                    ReferenceWidth = imageRect.Width
                });
            }
            drawable.ActiveStrokePoints = null;
        }

        // Finalize in-progress line / arrow
        if (drawable.ActiveLineStart.HasValue && drawable.ActiveLineEnd.HasValue)
        {
            CommitCurrentLine();
        }
    }

    void Invalidate() => graphicsView.Invalidate();

    #region Toolbar

    void ApplyToolbarTemplate()
    {
        if (ToolbarTemplate != null)
        {
            RemoveToolbar();
            var content = ToolbarTemplate.CreateContent();
            if (content is View view)
            {
                toolbarView = view;
                AddToolbarToGrid();
            }
        }
        else
        {
            BuildDefaultToolbar();
        }
    }

    void BuildDefaultToolbar()
    {
        // Don't build if custom template is set
        if (ToolbarTemplate != null)
            return;

        RemoveToolbar();
        zoomReadout = null;
        drawColorButton = null;
        fontPickerButton = null;
        fontSizePickerButton = null;
        undoButton = redoButton = resetButton = null;

        // Crop is modal — it gets a focused confirm/cancel bar instead of the full tool set
        toolbarView = CurrentToolMode == ImageEditorToolMode.Crop
            ? BuildCropToolbar()
            : BuildStandardToolbar();

        AddToolbarToGrid();
    }

    View BuildStandardToolbar()
    {
        var rows = new VerticalStackLayout { Spacing = 6 };

        rows.Children.Add(BuildToolRow());

        var options = BuildOptionsRow();
        if (options != null)
            rows.Children.Add(options);

        rows.Children.Add(BuildActionRow());

        return WrapInChrome(rows);
    }

    /// <summary>
    /// The tool picker. It lives in a horizontal scroller because the row grows with every
    /// enabled tool and used to run straight off the edge of a phone screen.
    /// </summary>
    View BuildToolRow()
    {
        var tools = new HorizontalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center
        };

        if (AllowZoom)
            tools.Children.Add(CreateToolButton(ImageEditorIcon.Move, "Move", ImageEditorToolMode.Move));

        if (AllowCrop)
            tools.Children.Add(CreateToolButton(ImageEditorIcon.Crop, "Crop", ImageEditorToolMode.Crop));

        if (AllowRotate)
            tools.Children.Add(CreateChromeButton(ImageEditorIcon.Rotate, "Rotate", false, true, () => Rotate(90)));

        if (AllowDraw)
            tools.Children.Add(CreateToolButton(ImageEditorIcon.Draw, "Draw", ImageEditorToolMode.Draw));

        if (AllowLine)
            tools.Children.Add(CreateToolButton(ImageEditorIcon.Line, "Line", ImageEditorToolMode.Line));

        if (AllowArrow)
            tools.Children.Add(CreateToolButton(ImageEditorIcon.Arrow, "Arrow", ImageEditorToolMode.Arrow));

        if (AllowTextAnnotation)
            tools.Children.Add(CreateToolButton(ImageEditorIcon.Text, "Text", ImageEditorToolMode.Text));

        return new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = tools
        };
    }

    /// <summary>Per-tool options — colour, stroke weight, font — shown only when they apply.</summary>
    View? BuildOptionsRow()
    {
        var isInk = CurrentToolMode is ImageEditorToolMode.Draw or ImageEditorToolMode.Line or ImageEditorToolMode.Arrow;
        var isText = CurrentToolMode == ImageEditorToolMode.Text;

        if (!isInk && !isText)
            return null;

        var row = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center
        };

        row.Children.Add(CreateDrawColorButton());

        if (isInk && ShowStrokeWidthPicker)
        {
            foreach (var width in StrokeWidthPresets)
                row.Children.Add(CreateStrokeWidthButton(width));
        }

        if (isText)
        {
            if (AllowFontSelection && AvailableFonts is { Count: > 0 })
                row.Children.Add(CreateFontPickerButton());

            if (AllowFontSizeSelection && AvailableFontSizes is { Count: > 0 })
                row.Children.Add(CreateFontSizePickerButton());
        }

        return new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = row
        };
    }

    /// <summary>History on the left, zoom in the middle, save on the right.</summary>
    View BuildActionRow()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            ],
            ColumnSpacing = 6
        };

        var history = new HorizontalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center
        };
        undoButton = CreateChromeButton(ImageEditorIcon.Undo, null, false, CanUndo, Undo);
        redoButton = CreateChromeButton(ImageEditorIcon.Redo, null, false, CanRedo, Redo);
        resetButton = CreateChromeButton(ImageEditorIcon.Reset, null, false, CanUndo, Reset);
        history.Children.Add(undoButton);
        history.Children.Add(redoButton);
        history.Children.Add(resetButton);
        grid.Add(history, 0);

        if (AllowZoom && ShowZoomControls)
            grid.Add(BuildZoomCluster(), 1);

        if (SaveCommand != null)
        {
            var save = new Button
            {
                Text = SaveText,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColor(ShinyThemeKeys.Color.OnPrimary, Colors.White),
                BackgroundColor = AccentColor,
                CornerRadius = 14,
                HeightRequest = 40,
                Padding = new Thickness(16, 0),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            }.WithFontSize(ShinyThemeKeys.Type.BodyMediumSize);
            save.Clicked += (_, _) => ExecuteSave();
            grid.Add(save, 2);
        }

        return grid;
    }

    View BuildZoomCluster()
    {
        var cluster = new HorizontalStackLayout
        {
            Spacing = 0,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        zoomReadout = new Label
        {
            Text = FormatZoom(zoomScale),
            TextColor = ChromeForeground,
            WidthRequest = 42,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }.WithFontSize(ShinyThemeKeys.Type.LabelSmallSize);

        // Tapping the percentage is the fastest way back to fit-to-view
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => ZoomToFit();
        zoomReadout.GestureRecognizers.Add(tap);

        cluster.Children.Add(CreateChromeButton(ImageEditorIcon.ZoomOut, null, false, true, ZoomOut, 38));
        cluster.Children.Add(zoomReadout);
        cluster.Children.Add(CreateChromeButton(ImageEditorIcon.ZoomIn, null, false, true, ZoomIn, 38));
        cluster.Children.Add(CreateChromeButton(ImageEditorIcon.ZoomFit, null, false, true, ZoomToFit, 38));

        return cluster;
    }

    View BuildCropToolbar()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            ],
            ColumnSpacing = 8
        };

        var cancelBtn = new Button
        {
            Text = CropCancelText,
            TextColor = ChromeForeground,
            BackgroundColor = Color.FromRgba(255, 255, 255, 0.14f),
            CornerRadius = 14,
            HeightRequest = 42,
            MinimumWidthRequest = 64,
            Padding = new Thickness(16, 0),
            VerticalOptions = LayoutOptions.Center
        }.WithFontSize(ShinyThemeKeys.Type.BodyMediumSize);
        cancelBtn.Clicked += (_, _) => CurrentToolMode = ImageEditorToolMode.Move;
        grid.Add(cancelBtn, 0);

        grid.Add(new Label
        {
            Text = "Drag the edges to crop",
            TextColor = ChromeForeground,
            FontSize = 13,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }, 1);

        var applyBtn = new Button
        {
            Text = CropApplyText,
            FontAttributes = FontAttributes.Bold,
            TextColor = ThemeColor(ShinyThemeKeys.Color.OnPrimary, Colors.White),
            BackgroundColor = AccentColor,
            CornerRadius = 14,
            HeightRequest = 42,
            MinimumWidthRequest = 64,
            Padding = new Thickness(16, 0),
            VerticalOptions = LayoutOptions.Center
        }.WithFontSize(ShinyThemeKeys.Type.BodyMediumSize);
        applyBtn.Clicked += (_, _) => ApplyCrop();
        grid.Add(applyBtn, 2);

        return WrapInChrome(grid);
    }

    /// <summary>The shared rounded scrim every default toolbar sits in.</summary>
    Border WrapInChrome(View content) => new()
    {
        Content = content,
        StrokeThickness = 0,
        BackgroundColor = ToolbarBackgroundColor,
        StrokeShape = new RoundRectangle().WithCornerRadius(ShinyThemeKeys.Shape.CornerExtraLargeRadius),
        Padding = new Thickness(8, 8),
        Margin = new Thickness(10, 8)
    };

    View CreateToolButton(ImageEditorIcon icon, string label, ImageEditorToolMode mode)
    {
        var selected = CurrentToolMode == mode;
        return CreateChromeButton(icon, label, selected, true, () =>
        {
            CurrentToolMode = selected ? ImageEditorToolMode.Move : mode;
        });
    }

    Border CreateChromeButton(ImageEditorIcon icon, string? label, bool selected, bool enabled, Action action, double? width = null)
    {
        var tint = selected
            ? ThemeColor(ShinyThemeKeys.Color.OnPrimary, Colors.White)
            : ChromeForeground;

        var showLabel = ShowToolLabels && !string.IsNullOrEmpty(label);

        var content = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        content.Children.Add(new GraphicsView
        {
            Drawable = new ImageEditorIconDrawable { Icon = icon, Color = tint },
            HeightRequest = 22,
            WidthRequest = 22,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Center
        });

        if (showLabel)
        {
            content.Children.Add(new Label
            {
                Text = label,
                FontSize = 9.5,
                TextColor = tint,
                LineBreakMode = LineBreakMode.NoWrap,
                HorizontalTextAlignment = TextAlignment.Center
            });
        }

        var button = new Border
        {
            Content = content,
            StrokeThickness = 0,
            BackgroundColor = selected ? AccentColor : Colors.Transparent,
            StrokeShape = new RoundRectangle().WithCornerRadius(ShinyThemeKeys.Shape.CornerLargeRadius),
            Padding = new Thickness(6, 4),
            MinimumWidthRequest = width ?? (showLabel ? 54 : 44),
            HeightRequest = showLabel ? 48 : 40,
            VerticalOptions = LayoutOptions.Center
        };

        SetButtonEnabled(button, enabled);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => action();
        button.GestureRecognizers.Add(tap);

        return button;
    }

    static void SetButtonEnabled(View button, bool enabled)
    {
        button.IsEnabled = enabled;
        button.Opacity = enabled ? 1 : 0.3;
    }

    View CreateStrokeWidthButton(double width)
    {
        var selected = Math.Abs(DrawStrokeWidth - width) < 0.01;
        var diameter = 6 + width * 1.6;

        var dot = new Border
        {
            BackgroundColor = selected ? ThemeColor(ShinyThemeKeys.Color.OnPrimary, Colors.White) : ChromeForeground,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = (float)(diameter / 2) },
            WidthRequest = diameter,
            HeightRequest = diameter,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true
        };

        var button = new Border
        {
            Content = dot,
            StrokeThickness = 0,
            BackgroundColor = selected ? AccentColor : Colors.Transparent,
            StrokeShape = new RoundRectangle().WithCornerRadius(ShinyThemeKeys.Shape.CornerLargeRadius),
            WidthRequest = 36,
            HeightRequest = 36,
            VerticalOptions = LayoutOptions.Center
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => DrawStrokeWidth = width;
        button.GestureRecognizers.Add(tap);

        return button;
    }

    ColorPickerButton CreateDrawColorButton()
    {
        drawColorButton = new ColorPickerButton
        {
            SelectedColor = DrawStrokeColor,
            CornerRadius = 18,
            HeightRequest = 36,
            WidthRequest = 36,
            VerticalOptions = LayoutOptions.Center
        };
        drawColorButton.ColorChanged += (_, color) => DrawStrokeColor = color;
        return drawColorButton;
    }

    FontPickerButton CreateFontPickerButton()
    {
        fontPickerButton = new FontPickerButton
        {
            AvailableFonts = AvailableFonts,
            SelectedFont = TextFontFamily,
            CornerRadius = 12,
            HeightRequest = 36,
            VerticalOptions = LayoutOptions.Center
        };
        fontPickerButton.FontChanged += (_, font) => TextFontFamily = font;
        return fontPickerButton;
    }

    FontSizePickerButton CreateFontSizePickerButton()
    {
        fontSizePickerButton = new FontSizePickerButton
        {
            AvailableFontSizes = AvailableFontSizes,
            SelectedFontSize = TextFontSize,
            CornerRadius = 12,
            HeightRequest = 36,
            VerticalOptions = LayoutOptions.Center
        };
        fontSizePickerButton.FontSizeChanged += (_, size) => TextFontSize = size;
        return fontSizePickerButton;
    }

    void UpdateZoomReadout()
    {
        if (zoomReadout != null)
            zoomReadout.Text = FormatZoom(zoomScale);
    }

    static string FormatZoom(float scale) => $"{Math.Round(scale * 100)}%";

    Color ChromeForeground => Color.FromRgba(255, 255, 255, 0.88f);

    Color AccentColor => ThemeColor(ShinyThemeKeys.Color.Primary, Color.FromRgba(10, 132, 255, 255));

    void AddToolbarToGrid()
    {
        if (toolbarView == null)
            return;

        if (ToolbarPosition == ToolbarPosition.Top)
        {
            // Swap row definitions so toolbar is on top
            rootGrid.RowDefinitions.Clear();
            rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Grid.SetRow(graphicsView, 1);
            Grid.SetRow(toolbarView, 0);
        }
        else
        {
            rootGrid.RowDefinitions.Clear();
            rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetRow(graphicsView, 0);
            Grid.SetRow(toolbarView, 1);
        }

        rootGrid.Children.Add(toolbarView);
    }

    void RemoveToolbar()
    {
        if (toolbarView != null)
        {
            rootGrid.Children.Remove(toolbarView);
            toolbarView = null;
        }
    }

    void UpdateToolbarPosition()
    {
        if (toolbarView == null)
            return;

        RemoveToolbar();
        if (ToolbarTemplate != null)
            ApplyToolbarTemplate();
        else
            BuildDefaultToolbar();
    }


    /// <summary>
    /// Resolves a theme token to a concrete colour. The editor's chrome is deliberately a fixed dark
    /// scrim (it sits over arbitrary photos and must stay legible), so only the semantic action
    /// buttons follow the theme, and they resolve once rather than binding.
    /// </summary>
    static Color ThemeColor(string key, Color fallback)
        => Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c ? c : fallback;

    #endregion
}

public enum ToolbarPosition
{
    Top,
    Bottom
}
