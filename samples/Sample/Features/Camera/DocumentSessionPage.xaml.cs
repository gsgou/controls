using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Features.Camera;

public partial class DocumentSessionPage : ContentPage
{
    readonly DocumentSessionStore session;

    /// <summary>The shared app-session list the camera's document analyzers feed.</summary>
    public ObservableCollection<CapturedDocument> Documents => this.session.Documents;

    public DocumentSessionPage()
    {
        this.session = IPlatformApplication.Current!.Services.GetRequiredService<DocumentSessionStore>();
        InitializeComponent();
        SampleSourceCode.Attach(this);
        this.BindingContext = this;
    }

    void OnClearClicked(object? sender, EventArgs e) => this.session.Clear();
}
