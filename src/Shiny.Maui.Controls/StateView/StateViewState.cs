namespace Shiny.Maui.Controls;

/// <summary>
/// One named branch of a <see cref="StateView"/>. Give it a <see cref="Name"/> and either inline
/// <see cref="Content"/> or a <see cref="ContentTemplate"/>; the state view shows whichever state's
/// name matches its <c>CurrentState</c>.
/// </summary>
/// <remarks>
/// Inline content is built when the XAML is inflated, so every state pays its cost up front.
/// <see cref="ContentTemplate"/> is built the first time the state is shown and then cached (see
/// <see cref="StateView.CacheContent"/>), which is what you want when a branch is expensive or
/// rarely reached.
/// </remarks>
[ContentProperty(nameof(Content))]
public class StateViewState : BindableObject
{
    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(StateViewState), null,
        propertyChanged: (b, o, n) => ((StateViewState)b).RaiseChanged());

    public static readonly BindableProperty ContentProperty = BindableProperty.Create(
        nameof(Content), typeof(View), typeof(StateViewState), null,
        propertyChanged: (b, o, n) => ((StateViewState)b).RaiseChanged());

    public static readonly BindableProperty ContentTemplateProperty = BindableProperty.Create(
        nameof(ContentTemplate), typeof(DataTemplate), typeof(StateViewState), null,
        propertyChanged: (b, o, n) => ((StateViewState)b).OnContentTemplateChanged());

    /// <summary>The value <c>CurrentState</c> is matched against (ordinal, case-insensitive).</summary>
    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    /// <summary>Content built eagerly with the rest of the markup.</summary>
    public View? Content
    {
        get => (View?)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Content built the first time this state is shown. Wins over <see cref="Content"/>.</summary>
    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    /// <summary>The view realized from <see cref="ContentTemplate"/>, once it has been.</summary>
    internal View? TemplatedContent { get; private set; }

    /// <summary>
    /// The view to host, realizing <see cref="ContentTemplate"/> on first use.
    /// </summary>
    internal View? ResolveContent()
    {
        if (this.ContentTemplate == null)
            return this.Content;

        if (this.TemplatedContent == null)
        {
            var template = this.ContentTemplate;
            if (template is DataTemplateSelector selector)
                template = selector.SelectTemplate(this.BindingContext, null);

            this.TemplatedContent = template.CreateContent() as View;
        }
        return this.TemplatedContent;
    }

    /// <summary>Drop the realized template so the next show rebuilds it.</summary>
    internal void ReleaseTemplatedContent() => this.TemplatedContent = null;

    void OnContentTemplateChanged()
    {
        this.TemplatedContent = null;
        this.RaiseChanged();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // Inline content is parented into the visual tree and inherits from there; templated
        // content may not be hosted yet, so it is seeded explicitly.
        if (this.TemplatedContent != null && this.TemplatedContent.Parent == null)
            SetInheritedBindingContext(this.TemplatedContent, this.BindingContext);
    }

    /// <summary>Raised when something the owning <see cref="StateView"/> renders from has changed.</summary>
    internal event EventHandler? Changed;

    private protected void RaiseChanged() => this.Changed?.Invoke(this, EventArgs.Empty);
}
