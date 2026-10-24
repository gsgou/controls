using System.Reflection;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Puts <see cref="ShinyThemeManager"/>'s static state back the way a test found it.
/// </summary>
/// <remarks>
/// <c>UseShinyControls()</c> installs a theme, and the manager is static: the theme it leaves behind is
/// re-applied by the next control any later test constructs (StyleGuard.MarkReady → EnsureApplied),
/// merged into that test's own <see cref="Application"/> on top of whatever dictionary the test set up.
/// <c>ThemeGeometryCoverageTests</c> failed only in a full run because of it. Wrap any test that
/// installs a theme in <c>using var _ = ThemeManagerState.Preserve();</c>.
/// </remarks>
static class ThemeManagerState
{
    static readonly PropertyInfo Current = typeof(ShinyThemeManager).GetProperty(nameof(ShinyThemeManager.CurrentTheme))!;
    static readonly FieldInfo Applied = typeof(ShinyThemeManager).GetField("applied", BindingFlags.NonPublic | BindingFlags.Static)!;


    public static IDisposable Preserve() => new Restore(Current.GetValue(null), Applied.GetValue(null));


    sealed class Restore(object? theme, object? applied) : IDisposable
    {
        public void Dispose()
        {
            Current.SetValue(null, theme);
            Applied.SetValue(null, applied);
        }
    }
}
