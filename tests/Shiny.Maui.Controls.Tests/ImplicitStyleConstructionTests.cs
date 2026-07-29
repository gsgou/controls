using System.Collections;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Library-wide guard for the implicit-Style-during-construction crash.
///
/// MAUI applies an implicit <see cref="Style"/> from <c>StyleableElement</c>'s own
/// constructor: <c>MergedStyle</c>'s ctor calls <c>RegisterImplicitStyles()</c>, which
/// resolves the style out of <c>Application.Current.Resources</c> and applies it immediately.
/// That runs <b>before the derived control's constructor body</b>, so any propertyChanged
/// callback that dereferences an instance field throws NullReferenceException, and the app
/// dies while inflating the page rather than showing anything.
///
/// The fix per control is a readiness gate on the callbacks plus a sync pass at the end of
/// the constructor - see <see cref="AutoCompleteEntry"/> and <c>FabMenu</c> for worked
/// examples. Merely reordering the constructor does NOT help, because the style is applied
/// before the constructor body runs at all.
///
/// This test constructs every control twice - bare, then with an implicit style targeting it -
/// and fails if a control outside <see cref="KnownAffected"/> breaks. Fix a control, delete
/// its entry here. The list only ever shrinks.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class ImplicitStyleConstructionTests(ITestOutputHelper output)
{
    /// <summary>
    /// Controls that still crash when an implicit Style targets them. Empty - every control
    /// now routes its child-touching callbacks through <see cref="Infrastructure.StyleGuard"/>.
    /// If something lands here again, it is a regression, not a backlog.
    /// </summary>
    static readonly HashSet<string> KnownAffected = new(StringComparer.Ordinal);

    [Fact]
    public void NoNewControlBreaksUnderAnImplicitStyle()
    {
        var broken = new List<string>();
        var fixedUp = new List<string>();

        // Some controls kick off an animation the moment a styled property lands, from an
        // `async void` method. Its exception is posted to the SynchronizationContext, so a
        // try/catch here cannot see it - and with no context installed it would tear the test
        // host down. Capture those posts instead; a headless host has no IAnimationManager,
        // which is a harness limitation rather than the bug under test.
        var previousContext = SynchronizationContext.Current;
        var capturing = new CapturingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(capturing);

        try
        {
            Run(broken, fixedUp);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }

        foreach (var swallowed in capturing.Captured)
            output.WriteLine($"async-void escape (harness has no animation manager): {swallowed.GetType().Name} - {swallowed.Message}");

        Report(broken, fixedUp);
    }

    void Run(List<string> broken, List<string> fixedUp)
    {
        foreach (var type in Controls())
        {
            var verdict = Probe(type);

            if (verdict.Skip is { } reason)
                output.WriteLine($"skipped ({reason}): {type.Name}");
            else if (verdict.Failure is { } failure)
                broken.Add($"{type.Name} -> {failure}");
            else if (KnownAffected.Contains(type.Name))
                fixedUp.Add(type.Name);
        }
    }

    void Report(List<string> broken, List<string> fixedUp)
    {
        foreach (var b in broken)
            output.WriteLine("BROKEN: " + b);

        var regressions = broken
            .Select(b => b.Split(" -> ")[0])
            .Where(n => !KnownAffected.Contains(n))
            .ToList();

        regressions.ShouldBeEmpty(
            "These controls crash when an implicit Style targets them. Route their " +
            "propertyChanged callbacks through StyleGuard.WhenReady and call " +
            "StyleGuard.MarkReady(this, typeof(TheControl)) at the end of the constructor " +
            "- see AutoCompleteEntry for a worked example."
        );

        if (fixedUp.Count > 0)
            output.WriteLine($"Now fixed - remove from KnownAffected: {string.Join(", ", fixedUp)}");
    }

    /// <summary>
    /// Swallows and records what an <c>async void</c> method posts on failure, so one control's
    /// animation blowing up in a headless host does not take the whole run with it.
    /// </summary>
    sealed class CapturingSynchronizationContext : SynchronizationContext
    {
        public List<Exception> Captured { get; } = new();

        public override void Post(SendOrPostCallback d, object? state) => Invoke(d, state);

        public override void Send(SendOrPostCallback d, object? state) => Invoke(d, state);

        void Invoke(SendOrPostCallback d, object? state)
        {
            try
            {
                d(state);
            }
            catch (Exception ex)
            {
                this.Captured.Add(ex);
            }
        }
    }

    /// <summary>Outcome of probing one control. Exactly one of the two is ever set.</summary>
    readonly record struct Verdict(string? Skip, string? Failure)
    {
        public static Verdict Ok() => new(null, null);
        public static Verdict Skipped(string reason) => new(reason, null);
        public static Verdict Broken(string failure) => new(null, failure);
    }

    static Verdict Probe(Type type)
    {
        // Baseline: if it cannot even be constructed bare in a unit test, it is out of scope
        // here (needs a handler or platform) rather than evidence of this bug.
        new Application();
        try
        {
            Activator.CreateInstance(type);
        }
        catch
        {
            return Verdict.Skipped("cannot construct bare in a test host");
        }

        var style = BuildStyle(type);
        if (style.Setters.Count == 0)
            return Verdict.Skipped("no settable properties");

        var app = new Application();
        app.Resources.Add(style);

        try
        {
            Activator.CreateInstance(type);
            return Verdict.Ok();
        }
        catch (Exception ex)
        {
            var root = Unwrap(ex);

            // Rather than pattern-match exception messages, prove it differentially: set the
            // very same values on a normally-constructed instance with no Style anywhere. If
            // that fails the same way, the control needs a dispatcher / animation manager /
            // handler that a headless test host does not provide - not this bug.
            var direct = FailureFromDirectAssignment(type, style);

            return direct == root.GetType()
                ? Verdict.Skipped($"{root.GetType().Name} also occurs with no Style involved")
                : Verdict.Broken($"{root.GetType().Name} @ {FirstShinyFrame(root)}");
        }
    }

    /// <summary>
    /// Constructs the control with no style at all, then assigns the style's values directly.
    /// Returns the exception type that surfaced, or null if it all worked.
    /// </summary>
    static Type? FailureFromDirectAssignment(Type type, Style style)
    {
        new Application();
        try
        {
            var control = (BindableObject)Activator.CreateInstance(type)!;
            foreach (var setter in style.Setters)
                control.SetValue(setter.Property, setter.Value);

            return null;
        }
        catch (Exception ex)
        {
            return Unwrap(ex).GetType();
        }
    }

    static Exception Unwrap(Exception ex) =>
        ex is TargetInvocationException { InnerException: not null } t ? t.InnerException! : ex;

    static IEnumerable<Type> Controls() => typeof(AutoCompleteEntry).Assembly
        .GetTypes()
        .Where(t => t is { IsPublic: true, IsAbstract: false, IsGenericTypeDefinition: false })
        .Where(t => typeof(VisualElement).IsAssignableFrom(t))
        .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
        .OrderBy(t => t.Name);

    static string FirstShinyFrame(Exception ex) =>
        (ex.StackTrace ?? "")
            .Split('\n')
            .FirstOrDefault(l => l.Contains("Shiny.Maui.Controls."))
            ?.Trim() ?? "(no frame)";

    /// <summary>An implicit style setting every Shiny-declared property to a non-default value.</summary>
    static Style BuildStyle(Type controlType)
    {
        var style = new Style(controlType);

        var properties = controlType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.FieldType == typeof(BindableProperty))
            .Select(f => f.GetValue(null) as BindableProperty)
            .OfType<BindableProperty>()
            // Only the library's own properties - MAUI's are not what is under test.
            .Where(bp => bp.DeclaringType?.Assembly == controlType.Assembly);

        foreach (var bp in properties)
        {
            if (NonDefaultValue(bp) is { } value)
                style.Setters.Add(new Setter { Property = bp, Value = value });
        }
        return style;
    }

    /// <summary>A value guaranteed to differ from the default, so propertyChanged actually fires.</summary>
    static object? NonDefaultValue(BindableProperty bp)
    {
        var t = bp.ReturnType;
        var d = bp.DefaultValue;

        if (t == typeof(Color)) return Equals(d, Colors.Red) ? Colors.Blue : Colors.Red;
        if (t == typeof(string)) return Equals(d, "probe") ? "probe2" : "probe";
        if (t == typeof(bool)) return !(d is bool b && b);
        if (t == typeof(double)) return (d is double dv ? dv : 0d) + 7d;
        if (t == typeof(float)) return (d is float fv ? fv : 0f) + 7f;
        if (t == typeof(int)) return (d is int iv ? iv : 0) + 7;
        if (t == typeof(uint)) return (d is uint uv ? uv : 0u) + 7u;
        if (t == typeof(Thickness)) return new Thickness(7);
        if (t == typeof(CornerRadius)) return new CornerRadius(7);
        if (t == typeof(TimeSpan)) return TimeSpan.FromSeconds(7);
        if (t == typeof(Brush) || t == typeof(SolidColorBrush)) return new SolidColorBrush(Colors.Red);

        if (t.IsEnum)
            return Enum.GetValues(t).Cast<object>().FirstOrDefault(v => !Equals(v, d));

        // Reference/graph types are not what triggers this class of bug; skipping them keeps
        // unrelated construction failures out of the results.
        return null;
    }
}
