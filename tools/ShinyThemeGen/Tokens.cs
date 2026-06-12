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

    // ---- Elevation: CSS box-shadow per level (Material 3 tonal elevation, neutral shadow) ----
    public static readonly (string Name, string BoxShadow)[] Elevation =
    [
        ("Level0", "none"),
        ("Level1", "0 1px 2px rgba(0,0,0,0.3), 0 1px 3px 1px rgba(0,0,0,0.15)"),
        ("Level2", "0 1px 2px rgba(0,0,0,0.3), 0 2px 6px 2px rgba(0,0,0,0.15)"),
        ("Level3", "0 4px 8px 3px rgba(0,0,0,0.15), 0 1px 3px rgba(0,0,0,0.3)"),
        ("Level4", "0 6px 10px 4px rgba(0,0,0,0.15), 0 2px 3px rgba(0,0,0,0.3)"),
        ("Level5", "0 8px 12px 6px rgba(0,0,0,0.15), 0 4px 4px rgba(0,0,0,0.3)"),
    ];

    // ---- Elevation as MAUI Shadow (levels 1..3 are the practically useful ones) ----
    public static readonly (string Name, double OffsetX, double OffsetY, double Radius, double Opacity)[] MauiShadow =
    [
        ("Level1", 0, 1, 3, 0.20),
        ("Level2", 0, 2, 6, 0.20),
        ("Level3", 0, 4, 8, 0.22),
    ];

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
