namespace Sample.Features.QuickEntry;

public partial class QuickEntryPage : ContentPage
{
    public QuickEntryPage(QuickEntryViewModel vm)
    {
        this.InitializeComponent();
        SampleSourceCode.Attach(this);
        this.BindingContext = vm;
    }
}
