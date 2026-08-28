namespace Sample.Features.Expanders;

public partial class ExpanderPage : ContentPage
{
    public ExpanderPage()
    {
        InitializeComponent();
        SampleSourceCode.Attach(this);
    }
}
