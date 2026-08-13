namespace Shiny.ThemeGen;

/// <summary>The complete, ordered Shiny token contract shared by MAUI and Blazor.</summary>
static class Tokens
{
    // ---- Color roles (PascalCase). Values are produced per-scheme by SchemeBuilder. ----
    public static readonly string[] ColorRoles =
    [
        "Primary", "OnPrimary", "PrimaryContainer", "OnPrimaryContainer",
        "Secondary", "OnSecondary", "SecondaryContainer", "OnSecondaryContainer",
        "Tertiary", "OnTertiary", "TertiaryContainer", "OnTertiaryContainer",
        "Error", "OnError", "ErrorContainer", "OnErrorContainer",
        "Background", "OnBackground",
        "Surface", "OnSurface", "SurfaceVariant", "OnSurfaceVariant",
        "SurfaceContainerLowest", "SurfaceContainerLow", "SurfaceContainer", "SurfaceContainerHigh", "SurfaceContainerHighest",
        "SurfaceTint",
        "Outline", "OutlineVariant",
        "Shadow", "Scrim",
        "InverseSurface", "InverseOnSurface", "InversePrimary",
        "Success", "OnSuccess", "SuccessContainer", "OnSuccessContainer",
        "Info", "OnInfo", "InfoContainer", "OnInfoContainer",
        "Warning", "OnWarning", "WarningContainer", "OnWarningContainer",
        "Caution", "OnCaution", "CautionContainer", "OnCautionContainer",
        "Critical", "OnCritical", "CriticalContainer", "OnCriticalContainer",
    ];

    // ---- Density: control metrics (px) before the theme's density scale is applied. ----
    public const double ControlHeight = 44;
    public const double ControlHeightSmall = 32;
    public const double RowHeight = 48;
    public const double TouchTarget = 44;

    // ---- Shape (corner radii, px). May be overridden per-theme via the "shape" json block. ----
    public static readonly (string Name, double Value)[] Shape =
    [
        ("CornerNone", 0),
        ("CornerExtraSmall", 4),
        ("CornerSmall", 8),
        ("CornerMedium", 12),
        ("CornerLarge", 16),
        ("CornerExtraLarge", 28),
        ("CornerFull", 9999),
    ];

    // ---- State layer opacities ----
    public static readonly (string Name, double Value)[] State =
    [
        ("HoverOpacity", 0.08),
        ("FocusOpacity", 0.10),
        ("PressedOpacity", 0.10),
        ("DraggedOpacity", 0.16),
    ];

    // ---- Spacing scale (px) ----
    public static readonly (string Name, double Value)[] Spacing =
    [
        ("Space0", 0),
        ("Space1", 4),
        ("Space2", 8),
        ("Space3", 12),
        ("Space4", 16),
        ("Space5", 24),
        ("Space6", 32),
        ("Space7", 48),
        ("Space8", 64),
    ];

    // ---- Type scale (Material 3). Size/LineHeight/Tracking in px, Weight numeric. ----
    public static readonly (string Role, double Size, double LineHeight, int Weight, double Tracking)[] Type =
    [
        ("DisplayLarge", 57, 64, 400, -0.25),
        ("DisplayMedium", 45, 52, 400, 0),
        ("DisplaySmall", 36, 44, 400, 0),
        ("HeadlineLarge", 32, 40, 400, 0),
        ("HeadlineMedium", 28, 36, 400, 0),
        ("HeadlineSmall", 24, 32, 400, 0),
        ("TitleLarge", 22, 28, 400, 0),
        ("TitleMedium", 16, 24, 500, 0.15),
        ("TitleSmall", 14, 20, 500, 0.1),
        ("BodyLarge", 16, 24, 400, 0.5),
        ("BodyMedium", 14, 20, 400, 0.25),
        ("BodySmall", 12, 16, 400, 0.4),
        ("LabelLarge", 14, 20, 500, 0.1),
        ("LabelMedium", 12, 16, 500, 0.5),
        ("LabelSmall", 11, 16, 500, 0.5),
    ];

    // ---- Elevation (Material 3 tonal elevation), as the layers each level is built from.
    // Kept structured rather than as literal box-shadow strings so a theme's elevation style and
    // intensity can rebuild them. Index = level; level 0 is deliberately empty ("none").
    public static readonly (double OffsetY, double Blur, double Spread, double Alpha)[][] ElevationLayers =
    [
        [],
        [(1, 2, 0, 0.30), (1, 3, 1, 0.15)],
        [(1, 2, 0, 0.30), (2, 6, 2, 0.15)],
        [(4, 8, 3, 0.15), (1, 3, 0, 0.30)],
        [(6, 10, 4, 0.15), (2, 3, 0, 0.30)],
        [(8, 12, 6, 0.15), (4, 4, 0, 0.30)],
    ];

    // ---- Elevation as MAUI Shadow. Index = level; level 0 is "no shadow". ----
    public static readonly (double OffsetX, double OffsetY, double Radius, double Opacity)[] MauiShadowLevels =
    [
        (0, 0, 0, 0),
        (0, 1, 3, 0.20),
        (0, 2, 6, 0.20),
        (0, 4, 8, 0.22),
        (0, 6, 12, 0.24),
        (0, 8, 16, 0.26),
    ];

    public static readonly string[] ElevationNames = ["Level0", "Level1", "Level2", "Level3", "Level4", "Level5"];

    public static readonly string[] BorderNames = ["Thin", "Medium", "Thick"];

    public static readonly string[] DensityNames = ["Scale", "ControlHeight", "ControlHeightSmall", "RowHeight", "TouchTarget"];

    // ---- Font family slots. Empty means "the platform default". ----
    public static readonly string[] FontFamilyNames = ["FontFamily", "FontFamilyDisplay", "FontFamilyMono"];

    /// <summary>camelCase/PascalCase -> kebab-case (OnPrimaryContainer -> on-primary-container).</summary>
    public static string Kebab(string pascal)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < pascal.Length; i++)
        {
            var c = pascal[i];
            if (char.IsUpper(c) && i > 0)
                sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
