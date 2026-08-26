namespace Shiny.Maui.Controls;

/// <summary>
/// Runs the built-in <see cref="TabSelectionAnimation"/>s. Replaced wholesale by setting
/// <see cref="ShinyTabBar.Animator"/>.
/// </summary>
sealed class DefaultTabAnimator(TabSelectionAnimation animation) : ITabAnimator
{
    public async Task AnimateAsync(TabAnimationContext context)
    {
        // No handler means nothing is on screen - and in a headless host there is no animation
        // manager at all, so awaiting one never completes. Land on the final frame instead.
        if (animation == TabSelectionAnimation.None || context.Duration == 0 || context.Bar.Handler is null)
        {
            Reset(context);
            return;
        }

        var duration = context.Duration;
        var selected = context.IsSelected;

        try
        {
            switch (animation)
            {
                case TabSelectionAnimation.Scale when context.Icon is { } icon:
                    await icon.ScaleToAsync(selected ? 1.15 : 1, duration, Easing.CubicOut).ConfigureAwait(true);
                    break;

                case TabSelectionAnimation.Lift when context.Icon is { } lift:
                    await lift.TranslateToAsync(0, selected ? -3 : 0, duration, Easing.CubicOut).ConfigureAwait(true);
                    break;

                case TabSelectionAnimation.Bounce when context.Icon is { } bounce:
                    if (selected)
                    {
                        // Two legs rather than a spring easing: the overshoot has to be visible at
                        // 200ms, and a spring curve that short reads as a single soft scale.
                        await bounce.ScaleToAsync(1.28, duration / 2, Easing.CubicOut).ConfigureAwait(true);
                        await bounce.ScaleToAsync(1.1, duration / 2, Easing.CubicIn).ConfigureAwait(true);
                    }
                    else
                    {
                        await bounce.ScaleToAsync(1, duration, Easing.CubicOut).ConfigureAwait(true);
                    }
                    break;

                case TabSelectionAnimation.Fade:
                    await context.Label.FadeToAsync(selected ? 1 : 0.65, duration, Easing.CubicOut).ConfigureAwait(true);
                    break;

                case TabSelectionAnimation.Indicator when context.Indicator is { } indicator:
                    if (selected)
                    {
                        indicator.Scale = 0.6;
                        indicator.Opacity = 0;
                        await Task.WhenAll(
                            indicator.ScaleToAsync(1, duration, Easing.CubicOut),
                            indicator.FadeToAsync(1, duration, Easing.CubicOut)
                        ).ConfigureAwait(true);
                    }
                    break;
            }
        }
        catch (Exception)
        {
            // Torn down mid-flight - the page popped, or the cells were rebuilt under us. The reset
            // below still runs so a cell is never left mid-animation if it comes back.
        }

        if (!selected)
            Reset(context);
    }


    /// <summary>Puts every property any of the animations touches back where it started.</summary>
    static void Reset(TabAnimationContext context)
    {
        if (context.Icon is { } icon)
        {
            icon.Scale = 1;
            icon.TranslationY = 0;
        }

        context.Label.Opacity = 1;

        if (context.Indicator is { } indicator)
        {
            indicator.Scale = 1;
            indicator.Opacity = 1;
        }
    }
}
