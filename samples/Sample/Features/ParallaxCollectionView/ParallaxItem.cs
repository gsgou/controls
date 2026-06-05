namespace Sample.Features.ParallaxCollectionView;

public class ParallaxItem
{
    public ParallaxItem(string title, string subtitle, string emoji, string color)
    {
        Title = title;
        Subtitle = subtitle;
        Emoji = emoji;
        Color = Microsoft.Maui.Graphics.Color.Parse(color);
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string Emoji { get; }
    public Color Color { get; }
}
