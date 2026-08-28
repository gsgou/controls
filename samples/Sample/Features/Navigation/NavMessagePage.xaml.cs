namespace Sample.Features.Navigation;

public partial class NavMessagePage : ContentPage
{
    public NavMessagePage()
    {
        this.InitializeComponent();
        SampleSourceCode.Attach(this);
    }

    void OnStar(object? sender, EventArgs e) => this.Status.Text = "Left item: starred";

    void OnShare(object? sender, EventArgs e) => this.Status.Text = "Right item: share";

    void OnDelete(object? sender, EventArgs e) => this.Status.Text = "Right item: delete";

    void OnBack(object? sender, EventArgs e) => _ = this.Navigation.PopAsync();
}
