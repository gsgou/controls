namespace Sample.Features.QuickEntry;

public partial class QuickEntryPage : ContentPage
{
    public QuickEntryPage(QuickEntryViewModel vm)
    {
        this.InitializeComponent();
        this.BindingContext = vm;
    }
}
