namespace Sample.Features.Tabs;

public partial class TabsInboxPage : ContentPage
{
    public TabsInboxPage()
    {
        this.InitializeComponent();
        this.BindingContext = new TabsInboxViewModel();
    }
}
