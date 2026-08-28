namespace Sample.Features.Tabs;

public partial class TabsInboxPage : ContentPage
{
    public TabsInboxPage()
    {
        this.InitializeComponent();
        SampleSourceCode.Attach(this);
        this.BindingContext = new TabsInboxViewModel();
    }
}
