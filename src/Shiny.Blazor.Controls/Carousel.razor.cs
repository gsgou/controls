using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls;

public partial class Carousel<TItem> : IAsyncDisposable
{
    ElementReference viewportRef;
    ElementReference trackRef;
    IJSObjectReference? module;
    DotNetObjectReference<Carousel<TItem>>? selfRef;
    readonly HashSet<int> loaded = new();
    string? lastOptionSig;
    bool initialized;

    [Inject] IJSRuntime JS { get; set; } = default!;

    // ----- Data / templates -----

    [Parameter] public IReadOnlyList<TItem>? Items { get; set; }
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Rendered in place of an item that has not yet been lazily loaded.</summary>
    [Parameter] public RenderFragment? PlaceholderTemplate { get; set; }

    /// <summary>Rendered when <see cref="Items"/> is null or empty.</summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    // ----- Behaviour -----

    [Parameter] public CarouselTransition Transition { get; set; } = CarouselTransition.Snap;
    [Parameter] public CarouselOrientation Orientation { get; set; } = CarouselOrientation.Horizontal;

    /// <summary>Auto-advance through items on a timer.</summary>
    [Parameter] public bool AutoPlay { get; set; }

    /// <summary>Milliseconds each item is shown before auto-advancing.</summary>
    [Parameter] public int AutoPlayInterval { get; set; } = 4000;

    /// <summary>Pause auto-play while the pointer is over the carousel or it has focus.</summary>
    [Parameter] public bool PauseOnHover { get; set; } = true;

    /// <summary>Wrap around past the first/last item.</summary>
    [Parameter] public bool Loop { get; set; } = true;

    /// <summary>Transition duration (ms) for Slide/Fade/Scale and the snap scale tween.</summary>
    [Parameter] public int TransitionDuration { get; set; } = 400;

    // ----- Lazy loading -----

    /// <summary>Only render item templates within <see cref="LazyLoadBuffer"/> of the current item.</summary>
    [Parameter] public bool LazyLoad { get; set; } = true;

    /// <summary>Number of neighbours on each side of the current item to keep rendered.</summary>
    [Parameter] public int LazyLoadBuffer { get; set; } = 1;

    // ----- Scroll-driven scaling (Snap mode) -----

    /// <summary>Smoothly scale items by their distance from centre as they snap into view.</summary>
    [Parameter] public bool EnableScaling { get; set; }

    [Parameter] public double FocusedItemScale { get; set; } = 1.0;
    [Parameter] public double UnfocusedItemScale { get; set; } = 0.82;

    // ----- Sizing (Snap mode) -----

    [Parameter] public double ItemWidth { get; set; } = 280;
    [Parameter] public double ItemHeight { get; set; } = 360;
    [Parameter] public double ItemSpacing { get; set; } = 16;

    /// <summary>How much of the neighbouring items peek in from the edges (Snap mode).</summary>
    [Parameter] public double PeekAmount { get; set; } = 40;

    // ----- Chrome -----

    [Parameter] public bool ShowIndicators { get; set; } = true;
    [Parameter] public bool ShowArrows { get; set; } = true;
    [Parameter] public bool ShowAutoPlayProgress { get; set; }
    [Parameter] public string? AriaLabel { get; set; }

    // ----- Binding / events -----

    [Parameter] public int CurrentPosition { get; set; }
    [Parameter] public EventCallback<int> CurrentPositionChanged { get; set; }
    [Parameter] public EventCallback<TItem> ItemSelected { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    // ----- Derived -----

    int Count => Items?.Count ?? 0;
    bool Horizontal => Orientation == CarouselOrientation.Horizontal;
    string PrevGlyph => Horizontal ? "‹" : "‹";
    string NextGlyph => Horizontal ? "›" : "›";

    string ModeClass => Transition switch
    {
        CarouselTransition.Slide => "mode-slide",
        CarouselTransition.Fade => "mode-fade",
        CarouselTransition.Scale => "mode-scale",
        _ => "mode-snap"
    };

    string OrientClass => Horizontal ? "orient-h" : "orient-v";

    string RootStyle =>
        $"--carousel-item-width:{ItemWidth}px;--carousel-item-height:{ItemHeight}px;" +
        $"--carousel-spacing:{ItemSpacing}px;--carousel-peek:{PeekAmount}px;" +
        $"--carousel-duration:{TransitionDuration}ms;" +
        $"--carousel-focused-scale:{Inv(FocusedItemScale)};--carousel-unfocused-scale:{Inv(UnfocusedItemScale)};";

    string TrackStyle
    {
        get
        {
            if (Transition != CarouselTransition.Slide)
                return "";
            var offset = -CurrentPosition * 100;
            return Horizontal
                ? $"transform:translate3d({offset}%,0,0);"
                : $"transform:translate3d(0,{offset}%,0);";
        }
    }

    string ItemStyle => Transition == CarouselTransition.Snap
        ? "width:var(--carousel-item-width);height:var(--carousel-item-height);"
        : "";

    static string Inv(double d) => d.ToString(System.Globalization.CultureInfo.InvariantCulture);

    bool IsLoaded(int index) => !LazyLoad || loaded.Contains(index);

    // ----- Lifecycle -----

    protected override void OnParametersSet()
    {
        if (CurrentPosition < 0)
            CurrentPosition = 0;
        else if (Count > 0 && CurrentPosition >= Count)
            CurrentPosition = Count - 1;

        EnsureLoaded(CurrentPosition);
    }

    void EnsureLoaded(int center)
    {
        if (!LazyLoad || Count == 0)
            return;

        for (var d = -LazyLoadBuffer; d <= LazyLoadBuffer; d++)
        {
            var i = center + d;
            if (Loop)
                i = ((i % Count) + Count) % Count;
            if (i >= 0 && i < Count)
                loaded.Add(i);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Count == 0)
            return;

        if (firstRender || module is null)
        {
            module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Shiny.Blazor.Controls/carousel.js");
            selfRef ??= DotNetObjectReference.Create(this);
        }

        var sig = OptionSignature();
        if (!initialized)
        {
            await module.InvokeVoidAsync("init", viewportRef, trackRef, selfRef, BuildOptions());
            initialized = true;
            lastOptionSig = sig;
        }
        else if (sig != lastOptionSig)
        {
            await module.InvokeVoidAsync("update", viewportRef, BuildOptions());
            lastOptionSig = sig;
        }
    }

    string OptionSignature() =>
        $"{Transition}|{Orientation}|{AutoPlay}|{AutoPlayInterval}|{PauseOnHover}|{Loop}|" +
        $"{EnableScaling}|{FocusedItemScale}|{UnfocusedItemScale}|{Count}";

    CarouselJsOptions BuildOptions() => new()
    {
        Mode = ModeClass.Replace("mode-", ""),
        Orientation = Horizontal ? "h" : "v",
        AutoPlay = AutoPlay,
        Interval = AutoPlayInterval,
        PauseOnHover = PauseOnHover,
        Loop = Loop,
        EnableScaling = EnableScaling && Transition == CarouselTransition.Snap,
        FocusedScale = FocusedItemScale,
        UnfocusedScale = UnfocusedItemScale,
        Count = Count,
        StartIndex = CurrentPosition
    };

    // ----- Navigation -----

    public Task NextAsync() => AdvanceAsync(1);
    public Task PreviousAsync() => AdvanceAsync(-1);

    async Task AdvanceAsync(int dir)
    {
        if (Count == 0)
            return;

        var next = CurrentPosition + dir;
        if (Loop)
            next = ((next % Count) + Count) % Count;
        else
            next = Math.Clamp(next, 0, Count - 1);

        await GoToAsync(next);
    }

    /// <summary>Programmatically move to <paramref name="index"/>.</summary>
    public async Task GoToAsync(int index)
    {
        if (Count == 0)
            return;

        index = Loop
            ? ((index % Count) + Count) % Count
            : Math.Clamp(index, 0, Count - 1);

        var changed = index != CurrentPosition;
        CurrentPosition = index;
        EnsureLoaded(index);

        if (changed && CurrentPositionChanged.HasDelegate)
            await CurrentPositionChanged.InvokeAsync(index);

        StateHasChanged();

        if (module is not null)
            await module.InvokeVoidAsync("goTo", viewportRef, index);
    }

    async Task OnItemClicked(int index)
    {
        if (Items is null || index >= Items.Count)
            return;

        await GoToAsync(index);
        if (ItemSelected.HasDelegate)
            await ItemSelected.InvokeAsync(Items[index]);
    }

    Task OnKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e) => e.Key switch
    {
        "ArrowRight" or "ArrowDown" => NextAsync(),
        "ArrowLeft" or "ArrowUp" => PreviousAsync(),
        "Home" => GoToAsync(0),
        "End" => GoToAsync(Count - 1),
        _ => Task.CompletedTask
    };

    // ----- JS callbacks -----

    /// <summary>Auto-play timer ticked in JS.</summary>
    [JSInvokable]
    public Task OnAutoTick() => AdvanceAsync(1);

    /// <summary>Snap-mode scroll settled on a new centred item.</summary>
    [JSInvokable]
    public async Task OnActiveIndexChanged(int index)
    {
        if (index == CurrentPosition || index < 0 || index >= Count)
            return;

        CurrentPosition = index;
        EnsureLoaded(index);

        if (CurrentPositionChanged.HasDelegate)
            await CurrentPositionChanged.InvokeAsync(index);

        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("dispose", viewportRef);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
        }
        selfRef?.Dispose();
    }

    sealed class CarouselJsOptions
    {
        public string Mode { get; set; } = "snap";
        public string Orientation { get; set; } = "h";
        public bool AutoPlay { get; set; }
        public int Interval { get; set; }
        public bool PauseOnHover { get; set; }
        public bool Loop { get; set; }
        public bool EnableScaling { get; set; }
        public double FocusedScale { get; set; }
        public double UnfocusedScale { get; set; }
        public int Count { get; set; }
        public int StartIndex { get; set; }
    }
}
