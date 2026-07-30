using System.Reflection;
using System.Reflection.Emit;
using Label = Microsoft.Maui.Controls.Label;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Infrastructure;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Library-wide guard for the <i>other</i> half of the StyleGuard contract.
///
/// <para>
/// <see cref="ImplicitStyleConstructionTests"/> proves a control does not <b>crash</b> when an
/// implicit Style is applied mid-construction. It cannot see the quieter failure: a control
/// whose constructor never calls <see cref="StyleGuard.MarkReady"/> for the level that queued
/// the callback. Nothing throws - the work simply sits in the queue forever and the styled or
/// XAML-set value never applies. That is how <c>ShinyContentPage</c> shipped rendering blank,
/// and how <c>CarouselGallery</c> / <c>StaggeredGrid</c> / <c>VirtualizedGrid</c> shipped
/// ignoring <c>ItemsSource</c> entirely.
/// </para>
///
/// <para>
/// The invariant is simple and total: once a constructor has returned, nothing may still be
/// queued. This test asserts that for every control, constructed bare and under an implicit
/// style that touches every one of its properties.
/// </para>
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class GuardedCallbacksLeftQueuedTests(ITestOutputHelper output)
{
    /// <summary>
    /// The realistic XAML path, and the one that caught both shipped bugs: construct the control,
    /// then assign its properties the way <c>InitializeComponent</c> does. A level that never
    /// marked itself ready parks every one of those assignments forever.
    /// </summary>
    [Fact]
    public void NoControlStrandsCallbacksWhenPropertiesAreSetAfterConstruction()
    {
        var stranded = new List<string>();

        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SwallowingSynchronizationContext());

        try
        {
            foreach (var type in Controls())
            {
                new Application();

                BindableObject control;
                try
                {
                    control = (BindableObject)Activator.CreateInstance(type)!;
                }
                catch
                {
                    output.WriteLine($"skipped (cannot construct bare in a test host): {type.Name}");
                    continue;
                }

                foreach (var (property, value) in ProbeValues(type))
                {
                    try
                    {
                        control.SetValue(property, value);
                    }
                    catch
                    {
                        // Setting a property may need a handler this host does not have. The
                        // callback still queued or ran, which is all we are measuring.
                    }
                }

                if (StyleGuard.HasPending(control))
                    stranded.Add($"{type.Name} (properties set after construction)");
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Report(stranded);
    }

    /// <summary>
    /// The same invariant for a <b>subclass</b> of every control - an app deriving a XAML page or
    /// a custom control from one of ours. Before levels were scoped, every base constructor's
    /// MarkReady no-op'd for a subclass instance and the whole queue was stranded.
    /// </summary>
    [Fact]
    public void NoSubclassedControlStrandsCallbacks()
    {
        var stranded = new List<string>();
        var module = AssemblyBuilder
            .DefineDynamicAssembly(new AssemblyName("GuardSubclassProbe"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("Main");

        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SwallowingSynchronizationContext());

        try
        {
            foreach (var type in Controls())
            {
                var subclass = Subclass(module, type);
                if (subclass is null)
                {
                    output.WriteLine($"skipped (cannot subclass): {type.Name}");
                    continue;
                }

                new Application();

                BindableObject control;
                try
                {
                    control = (BindableObject)Activator.CreateInstance(subclass)!;
                }
                catch
                {
                    output.WriteLine($"skipped (cannot construct in a test host): {type.Name}");
                    continue;
                }

                foreach (var (property, value) in ProbeValues(type))
                {
                    try
                    {
                        control.SetValue(property, value);
                    }
                    catch
                    {
                        // Needs a handler this host does not have; the callback still queued or ran.
                    }
                }

                if (StyleGuard.HasPending(control))
                    stranded.Add($"{type.Name} (subclassed)");
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Report(stranded);
    }

    /// <summary>Emits <c>class Sub_Foo : Foo { public Sub_Foo() : base() { } }</c>.</summary>
    static Type? Subclass(ModuleBuilder module, Type baseType)
    {
        if (baseType.IsSealed)
            return null;

        var baseCtor = baseType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null, Type.EmptyTypes, modifiers: null);

        if (baseCtor is null || baseCtor.IsPrivate)
            return null;

        var tb = module.DefineType($"Sub_{baseType.Name}", TypeAttributes.Public | TypeAttributes.Class, baseType);
        var il = tb
            .DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes)
            .GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, baseCtor);
        il.Emit(OpCodes.Ret);

        return tb.CreateType();
    }

    [Fact]
    public void NoControlLeavesGuardedCallbacksQueuedUnderAnImplicitStyle()
    {
        var stranded = new List<string>();

        // Some controls start an animation from an async void method the moment a styled value
        // lands; a headless host has no animation manager, so capture those posts rather than
        // letting them tear the run down. Same harness concession as ImplicitStyleConstructionTests.
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SwallowingSynchronizationContext());

        try
        {
            foreach (var type in Controls())
            {
                var style = BuildStyle(type);
                if (style.Setters.Count == 0)
                    continue;

                var app = new Application();
                app.Resources.Add(style);

                object styled;
                try
                {
                    styled = Activator.CreateInstance(type)!;
                }
                catch
                {
                    // A hard failure here is ImplicitStyleConstructionTests' business, not ours.
                    continue;
                }

                if (StyleGuard.HasPending(styled))
                    stranded.Add($"{type.Name} (implicit style)");
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Report(stranded);
    }

    void Report(List<string> stranded)
    {
        foreach (var s in stranded)
            output.WriteLine("STRANDED: " + s);

        stranded.ShouldBeEmpty(
            "These controls finish construction with guarded callbacks still queued, so the " +
            "values that triggered them silently never applied. The constructor must call " +
            "StyleGuard.MarkReady(this, typeof(TheDeclaringType)) for every level that guards " +
            "properties - see CollectionControlBase for a base-level example."
        );
    }

    sealed class SwallowingSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => Invoke(d, state);
        public override void Send(SendOrPostCallback d, object? state) => Invoke(d, state);

        static void Invoke(SendOrPostCallback d, object? state)
        {
            try
            {
                d(state);
            }
            catch
            {
                // Harness limitation, not the bug under test.
            }
        }
    }

    static IEnumerable<Type> Controls() => typeof(AutoCompleteEntry).Assembly
        .GetTypes()
        .Where(t => t is { IsPublic: true, IsAbstract: false, IsGenericTypeDefinition: false })
        .Where(t => typeof(VisualElement).IsAssignableFrom(t))
        .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
        .OrderBy(t => t.Name);

    static Style BuildStyle(Type controlType)
    {
        var style = new Style(controlType);

        foreach (var (property, value) in ProbeValues(controlType))
            style.Setters.Add(new Setter { Property = property, Value = value });

        return style;
    }

    /// <summary>Every Shiny-declared property on the control, paired with a non-default value.</summary>
    static IEnumerable<(BindableProperty Property, object Value)> ProbeValues(Type controlType) => controlType
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(f => f.FieldType == typeof(BindableProperty))
        .Select(f => f.GetValue(null) as BindableProperty)
        .OfType<BindableProperty>()
        // Only the library's own properties - MAUI's are not what is under test.
        .Where(bp => bp.DeclaringType?.Assembly == controlType.Assembly)
        .Select(bp => (Property: bp, Value: NonDefaultValue(bp)!))
        .Where(x => x.Value is not null);

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

        // Reference types matter here in a way they do not for the crash test: the bugs this
        // catches were on IEnumerable and View properties, which a scalar-only probe never sets.
        if (t == typeof(DataTemplate)) return new DataTemplate(() => new Label());
        if (t == typeof(ICommand)) return new Command(() => { });
        if (t == typeof(ImageSource)) return ImageSource.FromFile("probe.png");
        if (t == typeof(View)) return new Label();
        if (t.IsAssignableFrom(typeof(List<object>))) return new List<object> { "a", "b" };

        return null;
    }
}
