namespace Shiny.Maui.Controls.ParallaxCollectionView;

public class ParallaxScrollEventArgs : EventArgs
{
    public ParallaxScrollEventArgs(double verticalOffset, double headerTranslation, double headerVisibleHeight)
    {
        VerticalOffset = verticalOffset;
        HeaderTranslation = headerTranslation;
        HeaderVisibleHeight = headerVisibleHeight;
    }

    public double VerticalOffset { get; }
    public double HeaderTranslation { get; }
    public double HeaderVisibleHeight { get; }
}
