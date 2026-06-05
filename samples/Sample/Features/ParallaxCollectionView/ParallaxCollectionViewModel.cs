using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.ParallaxCollectionView;

[ShellMap<ParallaxCollectionViewPage>(registerRoute: false)]
public partial class ParallaxCollectionViewModel : ObservableObject
{
    [ObservableProperty]
    string statusMessage = "Scroll to see the hero translate at half speed";

    [ObservableProperty]
    double parallaxFactor = 0.5;

    [ObservableProperty]
    bool collapseToSticky;

    [ObservableProperty]
    bool fadeHeader;

    public ObservableCollection<ParallaxItem> Items { get; } = new(Build());

    [RelayCommand]
    void ToggleCollapse() => CollapseToSticky = !CollapseToSticky;

    [RelayCommand]
    void ToggleFade() => FadeHeader = !FadeHeader;

    [RelayCommand]
    void CycleFactor()
    {
        ParallaxFactor = ParallaxFactor switch
        {
            < 0.35 => 0.5,
            < 0.65 => 0.8,
            _ => 0.2
        };
        StatusMessage = $"Parallax factor: {ParallaxFactor:F1}";
    }

    [RelayCommand]
    void OnItemSelected(ParallaxItem? item)
    {
        if (item is null) return;
        StatusMessage = $"Tapped: {item.Title}";
    }

    static IEnumerable<ParallaxItem> Build()
    {
        var data = new[]
        {
            ("Mountain Lake",    "Alpine reflection",         "🏔️", "#2563EB"),
            ("Desert Dunes",     "Golden hour at the dunes",  "🏜️", "#D97706"),
            ("Coral Reef",       "Tropical underwater life",  "🐠", "#0891B2"),
            ("Misty Forest",     "Moss and rain",             "🌲", "#15803D"),
            ("Sunset Cliffs",    "End-of-day pacific coast",  "🌅", "#DC2626"),
            ("Northern Lights",  "Auroras over the fjord",    "✨", "#7C3AED"),
            ("Tokyo Streets",    "Neon, rain, and ramen",     "🏙️", "#DB2777"),
            ("Tuscan Hills",     "Vineyards and stone roads", "🍇", "#92400E"),
            ("Iceland Glacier",  "Blue ice caves",            "🧊", "#0EA5E9"),
            ("Sahara Stars",     "A sky thick with stars",    "🌌", "#1E1B4B"),
            ("Bali Rice Fields", "Terraces at dawn",          "🌾", "#65A30D"),
            ("Patagonia",        "Wind, granite, and glaciers","🥾", "#0F766E"),
            ("Kyoto Temples",    "Cherry blossoms in bloom",  "🌸", "#EC4899"),
            ("Greek Islands",    "White domes, blue sea",     "⛵", "#2563EB"),
            ("Moroccan Souk",    "Spice, tile, and color",    "🧿", "#B45309"),
            ("Norwegian Fjord",  "Cliffs into deep water",    "🌊", "#1D4ED8"),
            ("Scottish Moors",   "Heather and grey sky",      "🌫️", "#4B5563"),
            ("Big Sur Coast",    "Highway 1 lookouts",        "🚐", "#A16207"),
            ("Swiss Alps",       "Snow on the matterhorn",    "⛰️", "#0F172A"),
            ("Costa Rican Rain", "Cloud forest mornings",     "🦜", "#16A34A"),
        };

        foreach (var (title, subtitle, emoji, color) in data)
            yield return new ParallaxItem(title, subtitle, emoji, color);
    }
}
