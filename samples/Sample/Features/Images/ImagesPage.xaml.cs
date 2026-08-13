using Shiny.Maui.Controls.Images;

namespace Sample.Features.Images;

public partial class ImagesPage : ContentPage
{
    public ImagesPage()
    {
        InitializeComponent();
    }


    void OnImageLoaded(object? sender, ImageLoadedEventArgs e)
    {
        // Origin is the interesting part here: the first visit reports Network, and coming back to
        // the page reports Memory or Disk - which is the whole point of the cache being verifiable.
        if (this.BindingContext is ImagesViewModel vm)
            vm.OnImageLoaded(e);
    }
}
